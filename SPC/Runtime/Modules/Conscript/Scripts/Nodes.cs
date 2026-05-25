using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HELIX.Widgets.Diagnostics;
using HELIX.Widgets.Diagnostics.Properties;
using JetBrains.Annotations;
using UnityEngine;

namespace Spookline.SPC.Conscript {
    public class ConscriptHierarchy {
        public ConscriptNode Root { get; }
        public List<ConscriptNode> Nodes { get; }

        public ConscriptHierarchy(ConscriptNode root) {
            Root = root;
            var visited = new List<ConscriptNode>();
            Link(root, visited);
            Nodes = visited;
        }

        private static void Link(ConscriptNode node, List<ConscriptNode> visited) {
            if (visited.Contains(node)) return;
            node.PostInit();
            node.HierarchyIndex = visited.Count;
            visited.Add(node);

            var interrupters = new List<ConscriptNode>();
            foreach (var child in node.children) {
                child.Parent = node;
                if (interrupters.Count > 0) child.Interrupters = interrupters.ToArray();

                Link(child, visited);
                if (child.IsInterrupter) interrupters.Add(child);
            }

            node.Interrupters = interrupters;
        }

    }


    public class ConscriptNode : DiagnosticableTreeBase, IEnumerable<ConscriptNode> {

        // Never null at actual runtime
        internal RuntimeNodeList children;
        private bool _scheduled;

        // Runtime
        public NodeState State { get; private set; } = NodeState.Uninitialized;
        public ConscriptNode Parent { get; internal set; }
        public int HierarchyIndex { get; internal set; }

        // Siblings with higher priority are interrupters
        public IReadOnlyList<ConscriptNode> Interrupters { get; internal set; }

        [CanBeNull]
        public IReadOnlyList<ConscriptNode> Children {
            get => children;

            // Used by the builder dsl
            protected set {
                if (children == null) {
                    children = new RuntimeNodeList(value);
                    return;
                }

                Debug.LogError("Cannot set children after initialization");
            }
        }

        // Used by the builder dsl
        public void Add(ConscriptNode node) {
            children ??= new RuntimeNodeList();
            children.Add(node);
        }

        public virtual bool IsInterrupter => false;

        public virtual bool WillInterruptNode(ConscriptNode node) {
            return false;
        }

        public virtual bool WillInterrupt() {
            if (!State.IsActive()) return false;

            if (Interrupters != null) {
                for (var i = 0; i < Interrupters.Count; i++) {
                    var interrupter = Interrupters[i];
                    if (interrupter.WillInterruptNode(this)) return true;
                }
            }

            return false;
        }


        // Will always be called before any runtime methods execute
        internal void PostInit() {
            children ??= new RuntimeNodeList();
            children.Seal();
            Initialize();
        }

        protected virtual void Initialize() { }

        protected virtual void OnReset() { }

        protected virtual void OnEnd() { }

        protected virtual NodeState OnBegin() {
            return NodeState.Running;
        }

        protected virtual void OnInterrupt() { }

        protected virtual NodeState OnChildComplete(ConscriptNode child, NodeState result) {
            return State;
        }

        protected virtual NodeState OnUpdate() => NodeState.Succeeded;

        protected virtual void OnStateChange(NodeState previous, NodeState next) { }

        private bool ChangeState(NodeState next) {
            var previous = State;
            if (previous.HasTerminated() && previous != NodeState.Uninitialized) return true;
            if (previous == next) return false;
            var willTerminate = next.HasTerminatedOrInitial();
            if (willTerminate) children.Interrupt();

            if (next is NodeState.Interrupted) {
                try { OnInterrupt(); } catch (Exception e) {
                    Debug.LogException(e); //
                }
            }

            try { OnStateChange(previous, next); } catch (Exception e) {
                Debug.LogException(e); //
            }

            State = next;

            if (willTerminate) {
                try { OnEnd(); } catch (Exception e) {
                    Debug.LogException(e); //
                }

                Parent?.NotifyChildComplete(this, next);
                return true;
            }

            return false;
        }

        private void Reset() {
            if (State is NodeState.Uninitialized) return;
            try { OnReset(); } catch (Exception e) { Debug.LogException(e); }

            State = NodeState.Uninitialized;
            _scheduled = false;
            children.Reset();
        }

        private void NotifyChildComplete(ConscriptNode child, NodeState result) {
            var next = NodeState.Failed;
            try { next = OnChildComplete(child, result); } catch (Exception e) { Debug.LogException(e); }

            ChangeState(next);
        }


        private void Tick() {
            if (_scheduled) {
                _scheduled = false;
                if (!State.IsActive()) Begin();
            }

            if (!State.IsActive()) return;

            if (State.IsTicking()) {
                var result = NodeState.Failed;
                try {
                    result = OnUpdate(); //
                } catch (Exception e) {
                    Debug.LogException(e); //
                }

                if (ChangeState(result)) return;
            }

            children.Tick();
        }

        private void Begin() {
            Reset();
            var result = NodeState.Failed;
            try { result = OnBegin(); } catch (Exception e) {
                Debug.LogException(e); //
            }

            ChangeState(result);
        }

        private void TickOrBegin() {
            _scheduled = true;
            Tick();
        }

        protected void Schedule(ConscriptNode node) {
#if UNITY_ASSERTIONS
            Debug.Assert(children.Contains(node), $"{node} is not a child of {this}");
            Debug.Assert(node != this, $"{node} tried to schedule itself");
#endif
            node._scheduled = true;
        }

        public override List<DiagnosticsNode> DebugDescribeChildren() {
            return children.Count == 0
                ? new List<DiagnosticsNode>()
                : children.Select(child => child.ToDiagnosticsNodeSafe()).ToList();
        }

        public override void DebugFillProperties(DiagnosticPropertiesBuilder properties) {
            properties.Add(new EnumProperty<NodeState>("state", State));
        }

        internal class RuntimeNodeList : IReadOnlyList<ConscriptNode> {

            private readonly IReadOnlyList<ConscriptNode> _nodes;
            private bool _buildable;

            public RuntimeNodeList(IReadOnlyList<ConscriptNode> nodes) {
                _nodes = nodes;
                _buildable = false;
            }

            public RuntimeNodeList() {
                _nodes = new List<ConscriptNode>();
                _buildable = true;
            }

            public void Tick() {
                for (var i = 0; i < _nodes.Count; i++) { _nodes[i].Tick(); }
            }

            public void Interrupt() {
                for (var i = 0; i < _nodes.Count; i++) { _nodes[i].ChangeState(NodeState.Interrupted); }
            }

            public void Reset() {
                for (var i = 0; i < _nodes.Count; i++) { _nodes[i].Reset(); }
            }

            public void Seal() {
                _buildable = false;
            }

            // Builders will later call this
            public void Add(ConscriptNode node) {
                if (!_buildable) throw new InvalidOperationException("Cannot add nodes after finalization");
                ((List<ConscriptNode>)_nodes).Add(node);
            }


            public ConscriptNode this[int index] => _nodes[index];

            public int Count => _nodes.Count;

            public IEnumerator<ConscriptNode> GetEnumerator() => _nodes.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _nodes.GetEnumerator();

        }

        public IEnumerator<ConscriptNode> GetEnumerator() {
            return children == null ? Enumerable.Empty<ConscriptNode>().GetEnumerator() : children.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }

    public enum NodeState : byte {

        Uninitialized = 0, // Reset

        Running = 1, // Actively running and ticking
        Waiting = 2, // Waiting but still ticking
        Suspended = 3, // Waiting for children, not ticking

        Succeeded = 4, // Result: success
        Failed = 5, // Result: failure
        Interrupted = 6, // Result: interrupted

    }

    public static class NodeStateExtensions {

        public static bool HasTerminatedOrInitial(this NodeState state) =>
            state is > NodeState.Suspended or NodeState.Uninitialized;


        public static bool HasTerminated(this NodeState state) => state > NodeState.Suspended;

        public static bool IsActive(this NodeState state) =>
            state is > NodeState.Uninitialized and < NodeState.Succeeded;

        public static bool IsTicking(this NodeState state) => state is NodeState.Running or NodeState.Waiting;

    }
}