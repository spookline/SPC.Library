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
        }

        /// <summary>
        /// Performs a single full tick of the behavior hierarchy tree.
        /// </summary>
        public void Tick() {
            Root.TickOrBegin();
        }

        /// <summary>
        /// Fully resets the behavior hierarchy tree.
        /// </summary>
        public void Reset() {
            Root.Reset();
        }

    }


    /// <summary>
    /// Represents a node in the Conscript hierarchy that maintains state,
    /// parent-child relationships, and runtime behavior. Implements a DAG tree
    /// structure with diagnostic and runtime capabilities.
    ///
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nodes always possess a state which is initialized with <see cref="NodeState.Uninitialized"/>. A node orderly
    /// terminates with either <see cref="NodeState.Succeeded"/> <see cref="NodeState.Failed"/> but may terminate with
    /// <see cref="NodeState.Interrupted"/> when interrupted either by the parent, a sibling, or itself.
    /// While running, a node may be in the <see cref="NodeState.Running"/>, <see cref="NodeState.Waiting"/> or
    /// <see cref="NodeState.Suspended"/> state. While suspended, a node's update function is not called.
    /// Waiting and running behave the same at runtime, with waiting being mostly a common semantic state for flow nodes.
    /// </para>
    ///
    /// <para>
    /// Begin and Tick passes always run in the same frame and are not postponed. Invocation wise, the flow can be
    /// described as the following event flows:
    /// <list type="bullet">
    /// <item>Reset (Trickle-Down)</item>
    /// <item>Begin (Trickle-Down)</item>
    /// <item>Interrupts (Trickle-Down, evaluated by neighbors, self, and parents)</item>
    /// <item>Tick (Trickle-Down)</item>
    /// <item>Completion (Bubble-Up)</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// All of those passes run inside the parent's frame, possibly multiple times should the parent choose to do so.
    /// </para>
    ///
    /// <para>
    /// <b>Regarding Interruption</b>: Nodes will not check for interruption on the same frame they were started in but may
    /// still be interrupted by the parent. Interruption is meant as a way to control long-running graph behaviors, not
    /// immediate control flow. Once a node is interrupted, the parent will receive the completion notification like
    /// any other completion and may then choose to perform further action in his tick, i.E reevaluating itself.
    /// A frame loop guard should be used in this case to prevent accidental infinite loops.
    /// </para>
    /// </remarks>
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

        private void ChangeState(NodeState next) {
            var previous = State;
            if (previous == next) return;
            var willTerminate = next.HasTerminatedOrInitial();
            if (previous is NodeState.Uninitialized) goto body;

            if (previous.HasTerminated()) return;
            if (willTerminate) children.Interrupt();

            if (next is NodeState.Interrupted) {
                try { OnInterrupt(); } catch (Exception e) {
                    Debug.LogException(e); //
                }
            }

            body:
            { }
            try { OnStateChange(previous, next); } catch (Exception e) {
                Debug.LogException(e); //
            }

            State = next;

            if (willTerminate) {
                try { OnEnd(); } catch (Exception e) {
                    Debug.LogException(e); //
                }

                Parent?.NotifyChildComplete(this, next);
            }
        }

        internal void Reset() {
            if (State is NodeState.Uninitialized) return;
            try { OnReset(); } catch (Exception e) { Debug.LogException(e); }

            State = NodeState.Uninitialized;
            _scheduled = false;
            children.Reset();
        }

        internal void InterruptAndReset() {
            if (State.IsActive()) ChangeState(NodeState.Interrupted);
            Reset();
        }

        private void NotifyChildComplete(ConscriptNode child, NodeState result) {
            var next = NodeState.Failed;
            try { next = OnChildComplete(child, result); } catch (Exception e) { Debug.LogException(e); }

            ChangeState(next);
        }


        internal void Tick() {
            var wasActive = State.IsActive();
            if (_scheduled) {
                _scheduled = false;
                if (!wasActive) Begin();
                goto body;
            }

            if (!wasActive) return;

            // TODO: Check for interrupts and probably handle child checking differently: Do not call events on children
            // That aren't supposed to receive it probably.

            if (WillInterrupt()) {
                ChangeState(NodeState.Interrupted);
                return;
            }

            body:
            { }
            if (State.IsTicking()) {
                var result = NodeState.Failed;
                try {
                    result = OnUpdate(); //
                } catch (Exception e) {
                    Debug.LogException(e); //
                }

                ChangeState(result);
            }

            if (State.IsActive()) children.Tick();
        }

        private void Begin() {
            Reset();
            var result = NodeState.Failed;
            try { result = OnBegin(); } catch (Exception e) {
                Debug.LogException(e); //
            }

            ChangeState(result);
        }

        internal void TickOrBegin() {
            _scheduled = true;
            Tick();
        }

        /// <summary>
        /// Schedules a child node to run after the current nodes tick if it is not already scheduled.
        /// </summary>
        protected void Schedule(ConscriptNode node) {
#if UNITY_ASSERTIONS
            Debug.Assert(children.Contains(node), $"{node} is not a child of {this}");
            Debug.Assert(node != this, $"{node} tried to schedule itself");
#endif
            node._scheduled = true;
        }

        /// <summary>
        /// Executes a full clean tick of a child node and return the result after the first frame.
        /// </summary>
        /// <remarks>
        /// This should only be used with nodes that are expected to terminate in one frame.
        /// </remarks>
        protected NodeState Execute(ConscriptNode node) {
#if UNITY_ASSERTIONS
            Debug.Assert(children.Contains(node), $"{node} is not a child of {this}");
            Debug.Assert(node != this, $"{node} tried to schedule itself");
            Debug.Assert(node.State.HasTerminatedOrInitial(), $"{node} is already active");
#endif
            node.InterruptAndReset();
            node.TickOrBegin();
            var endState = node.State;
#if UNITY_ASSERTIONS
            Debug.Assert(endState.HasTerminatedOrInitial(), $"{node} did not terminate in one frame");
#endif
            return endState;
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