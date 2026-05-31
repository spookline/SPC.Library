using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HELIX.Coloring;
using HELIX.Widgets.Diagnostics;
using HELIX.Widgets.Diagnostics.Properties;
using HELIX.Widgets.Universal;
using Spookline.SPC.Conscript.UI;
using UnityEngine;

namespace Spookline.SPC.Conscript {
    /// <summary>
    /// Represents a node in the Conscript hierarchy that maintains state,
    /// parent-child relationships, and runtime behavior. Implements a DAG tree
    /// structure with diagnostic and runtime capabilities.
    ///
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nodes always possess a state which is initialized with <see cref="NodeStatus.Uninitialized"/>. A node orderly
    /// terminates with either <see cref="NodeStatus.Succeeded"/> <see cref="NodeStatus.Failed"/> but may terminate with
    /// <see cref="NodeStatus.Interrupted"/> when interrupted either by the parent, a sibling, or itself.
    /// While running, a node may be in the <see cref="NodeStatus.Running"/>, <see cref="NodeStatus.Waiting"/> or
    /// <see cref="NodeStatus.Suspended"/> state. While suspended, a node's update function is not called.
    /// Waiting and running behave the same at runtime, with waiting being mostly a common semantic state for flow nodes.
    /// </para>
    ///
    /// <para>
    /// Begin and Tick passes always run in the same frame and are not postponed. Invocation wise, the flow can be
    /// generally described as trickle-down.
    /// </para>
    ///
    /// <para>
    /// All of those passes run inside the parent's frame, a tick may only happen once per frame for a single lifetime.
    /// A node may execute multiple ticks in a single frame if it gets reset.
    /// </para>
    ///
    /// <para>
    /// <b>Regarding Interruption</b>: Nodes will not check for interruption on the same frame they were started in but may
    /// still be interrupted by the parent. Interruption is meant as a way to control long-running graph behaviors, not
    /// immediate control flow.
    /// </para>
    /// </remarks>
    public class ConscriptNode : DiagnosticableTreeBase, IEnumerable<ConscriptNode> {

        // Never null at actual runtime
        internal readonly RuntimeNodeList children = new();

        private readonly List<StateDelta> _stateDeltas = new();
        private bool _scheduled;
        private bool _record;
        private uint _lastInterruptedAt;

        // Runtime
        public NodeStatus Status { get; private set; } = NodeStatus.Uninitialized;
        public ConscriptNode Parent { get; internal set; }
        public int NodeIndex { get; internal set; }
        public ConscriptMachine Machine { get; internal set; }
        public uint LastTick { get; internal set; }
        public bool WasTickedLastFrame => Machine.WasTickedLastFrame(LastTick);
        public bool WasTickedThisFrame => Machine.CurrentTick == LastTick && LastTick != 0;

        public IReadOnlyList<ConscriptNode> Interrupters { get; internal set; }

        public IReadOnlyList<ConscriptNode> Children => children;

        public virtual bool IsInterrupter => false;

        protected virtual bool WillInterruptNode(ConscriptNode node) {
            return false;
        }

        public bool CheckInterrupt(ConscriptNode node) {
            var result = WillInterruptNode(node);
            if (result) {
                _lastInterruptedAt = Machine.CurrentTick;
                RecordStateChange(Status); // Dummy state change to record the interruption event in the state deltas
            }

            return result;
        }

        public virtual bool WillInterrupt() {
            if (!Status.IsActive()) return false;

            if (Interrupters != null) {
                for (var i = 0; i < Interrupters.Count; i++) {
                    var interrupter = Interrupters[i];
                    if (interrupter.CheckInterrupt(this)) return true;
                }
            }

            return false;
        }


        // Will always be called before any runtime methods execute
        internal void PostInit() {
            Initialize();
            children.Seal();
        }

        private int FindFirstDeltaAfter(uint tick) {
            var lo = 0;
            var hi = _stateDeltas.Count;

            while (lo < hi) {
                var mid = lo + ((hi - lo) >> 1);
                if (_stateDeltas[mid].tick <= tick) lo = mid + 1;
                else hi = mid;
            }

            return lo;
        }

        private int FindFirstDeltaAtOrAfter(uint tick) {
            var lo = 0;
            var hi = _stateDeltas.Count;

            while (lo < hi) {
                var mid = lo + ((hi - lo) >> 1);
                if (_stateDeltas[mid].tick < tick) lo = mid + 1;
                else hi = mid;
            }

            return lo;
        }

        internal NodeStatus GetFinalState(uint tick, out StateDeltaFlags flags) {
            var index = FindFirstDeltaAfter(tick) - 1;
            if (index < 0) {
                flags = GetCurrentFlags(false);
                return NodeStatus.Uninitialized;
            } else {
                flags = _stateDeltas[index].flags;
                return _stateDeltas[index].next;
            }
        }

        internal NodeStatus GetStartState(uint tick) {
            var index = FindFirstDeltaAtOrAfter(tick);

            if (index < _stateDeltas.Count && _stateDeltas[index].tick == tick) { return _stateDeltas[index].previous; }

            index--;
            return index < 0 ? NodeStatus.Uninitialized : _stateDeltas[index].next;
        }

        internal void SetRecording(bool record) {
            if (record == _record) return;
            _record = record;
            if (record) {
                RecordStateChange(Status);
            } else {
                _stateDeltas.Clear();
                _stateDeltas.Capacity = 0;
            }
        }


        protected virtual void Initialize() { }

        protected internal virtual NodeTemplate BuildWidget() {
            return new NodeTemplate(this) {
                content = new HText(this.DescribeIdentity()),
                sequence = children.Count == 0 ? null : children.Select(x => x.BuildWidget()).ToList()
            };
        }

        protected virtual void OnReset() { }

        protected virtual void OnEnd() { }

        protected virtual NodeStatus OnBegin() {
            return NodeStatus.Running;
        }

        protected virtual void OnInterrupt() { }

        protected virtual NodeStatus OnUpdate() => NodeStatus.Succeeded;

        protected virtual void OnStateChange(NodeStatus previous, NodeStatus next) { }

        private void ChangeState(NodeStatus next, bool tick = false) {
            var previous = Status;
            if (previous == next) return;
            if (!previous.IsActive() && next is NodeStatus.Interrupted) return;

            var willTerminate = next.HasTerminatedOrInitial();
            if (previous is NodeStatus.Uninitialized) goto body;

            if (previous.HasTerminated()) return;
            if (willTerminate) children.Interrupt();

            if (next is NodeStatus.Interrupted) {
                try { OnInterrupt(); } catch (Exception e) {
                    Debug.LogException(e); //
                }
            }

            body:
            { }
            try { OnStateChange(previous, next); } catch (Exception e) {
                Debug.LogException(e); //
            }

            if (next is NodeStatus.Yield) {
                Debug.Log($"{this} has yielded");
                Machine.NotifyYield();
            }

            RecordStateChange(next, tick);
            Status = next;

            if (willTerminate) {
                try { OnEnd(); } catch (Exception e) {
                    Debug.LogException(e); //
                }
            }
        }

        internal void RecordStateChange(NodeStatus next, bool tick = false) {
            if (!_record) return;
            _stateDeltas.Add(
                new StateDelta {
                    previous = Status,
                    next = next,
                    tick = Machine.CurrentTick,
                    flags = GetCurrentFlags(tick)
                }
            );
        }

        private StateDeltaFlags GetCurrentFlags(bool tick) {
            StateDeltaFlags flags = 0;
            if (WasTickedThisFrame || tick) flags |= StateDeltaFlags.WasTickedThisFrame;
            if (WasTickedLastFrame) flags |= StateDeltaFlags.WasTickedLastFrame;
            if (_lastInterruptedAt == Machine.CurrentTick && _lastInterruptedAt != 0)
                flags |= StateDeltaFlags.HasInterrupted;
            return flags;
        }

        internal void Reset() {
            if (Status is NodeStatus.Uninitialized) return;
            try { OnReset(); } catch (Exception e) { Debug.LogException(e); }

            RecordStateChange(NodeStatus.Uninitialized);
            Status = NodeStatus.Uninitialized;
            _scheduled = false;
            LastTick = 0;
            children.Reset();
        }

        internal void Interrupt() {
            if (Status.IsActive()) ChangeState(NodeStatus.Interrupted);
        }

        internal void InterruptAndReset() {
            Interrupt();
            Reset();
        }

        internal bool Tick() {
            if (WasTickedThisFrame) {
                _scheduled = false;
                return true;
            }

            var wasActive = Status.IsActive();
            if (_scheduled) {
                _scheduled = false;
                if (!wasActive) {
                    Begin();
                    goto skipInterrupts;
                }
            }

            if (!wasActive) return false;

            // TODO: Check for interrupts and probably handle child checking differently: Do not call events on children
            // That aren't supposed to receive it probably.

            if (WasTickedLastFrame) {
                if (WillInterrupt()) {
                    ChangeState(NodeStatus.Interrupted, true);
                    return true;
                }
            }

            skipInterrupts:
            if (Status.IsTicking()) {
                var result = NodeStatus.Failed;
                try {
                    result = OnUpdate(); //
                } catch (Exception e) {
                    Debug.LogException(e); //
                }

                ChangeState(result, true);
            }

            LastTick = Machine.CurrentTick;
            return true;
        }

        private void Begin() {
            var result = NodeStatus.Failed;
            try { result = OnBegin(); } catch (Exception e) {
                Debug.LogException(e); //
            }

            ChangeState(result, true);
        }

        internal void TickOrBegin() {
            if (WasTickedThisFrame) return;
            if (Status.HasTerminated()) Reset();
            _scheduled = true;
            Tick();
        }

        internal void RootTick() {
            if (Status.HasTerminated()) Reset();
            TickOrBegin();
        }

        protected internal void Reset(ConscriptNode node) => node.InterruptAndReset();
        protected internal void Interrupt(ConscriptNode node) => node.Interrupt();
        protected internal void InterruptChildren() => children.Interrupt();
        protected internal void InterruptAndResetChildren() => children.InterruptAndReset();

        protected internal void Schedule(ConscriptNode node) {
#if UNITY_ASSERTIONS
            Debug.Assert(children.Contains(node), $"{node} is not a child of {this}");
            Debug.Assert(node != this, $"{node} tried to schedule itself");
#endif
            node._scheduled = true;
        }

        protected internal NodeStatus Continue(ConscriptNode node) {
#if UNITY_ASSERTIONS
            Debug.Assert(children.Contains(node), $"{node} is not a child of {this}");
            Debug.Assert(node != this, $"{node} tried to schedule itself");
#endif
            if (!node.Tick()) return NodeStatus.Interrupted;
            return node.Status;
        }

        protected internal NodeStatus Run(ConscriptNode node) {
#if UNITY_ASSERTIONS
            Debug.Assert(children.Contains(node), $"{node} is not a child of {this}");
            Debug.Assert(node != this, $"{node} tried to schedule itself");
#endif
            node.TickOrBegin();
            return node.Status;
        }

        /// <summary>
        /// Executes a full clean tick of a child node and return the result after the first frame.
        /// </summary>
        /// <remarks>
        /// This should only be used with nodes that are expected to terminate in one frame.
        /// </remarks>
        protected NodeStatus Execute(ConscriptNode node) {
#if UNITY_ASSERTIONS
            Debug.Assert(children.Contains(node), $"{node} is not a child of {this}");
            Debug.Assert(node != this, $"{node} tried to schedule itself");
            Debug.Assert(node.Status.HasTerminatedOrInitial(), $"{node} is already active");
#endif
            node.InterruptAndReset();
            node.TickOrBegin();
            var endState = node.Status;
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
            properties.Add(new EnumProperty<NodeStatus>("state", Status));
            properties.Add(new FlagProperty("WasTickedThisFrame", WasTickedThisFrame, ifTrue: "Ticked"));
            properties.Add(new FlagProperty("WasTickedLastFrame", WasTickedLastFrame, ifTrue: "Ticked Previously"));
            properties.Add(new FlagProperty("HasInterrupted", _lastInterruptedAt == Machine.CurrentTick, ifTrue: "Has Interrupted"));
        }

        internal struct StateDelta {

            public NodeStatus previous;
            public NodeStatus next;
            public uint tick;
            public StateDeltaFlags flags;

        }

        [Flags]
        internal enum StateDeltaFlags : byte {

            WasTickedThisFrame = 1 << 0,
            WasTickedLastFrame = 1 << 1,
            HasInterrupted = 1 << 2

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

            public void Interrupt() {
                for (var i = 0; i < _nodes.Count; i++) { _nodes[i].ChangeState(NodeStatus.Interrupted); }
            }

            public void Reset() {
                for (var i = 0; i < _nodes.Count; i++) { _nodes[i].Reset(); }
            }

            public void InterruptAndReset() {
                for (var i = 0; i < _nodes.Count; i++) { _nodes[i].InterruptAndReset(); }
            }

            public void Seal() {
                _buildable = false;
            }

            // Builders will later call this
            public void Add(ConscriptNode node) {
                if (!_buildable) throw new InvalidOperationException("Cannot add nodes after finalization");
                ((List<ConscriptNode>)_nodes).Add(node);
            }

            public void AddRange(IEnumerable<ConscriptNode> nodes) {
                if (!_buildable) throw new InvalidOperationException("Cannot add nodes after finalization");
                ((List<ConscriptNode>)_nodes).AddRange(nodes);
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

    public abstract class CollectionConscriptNode : ConscriptNode {

        protected virtual int MaxCount => int.MaxValue;

        // Used by the builder dsl
        public void Add(ConscriptNode node) {
            if (children.Count >= MaxCount) {
                throw new InvalidOperationException($"Cannot add more than {MaxCount} children to {GetType().Name}");
            }

            children.Add(node);
        }

    }

    public enum NodeStatus : byte {

        Uninitialized = 0, // Reset

        Running = 1, // Actively running and ticking
        Waiting = 2, // Waiting but still ticking
        Yield = 3, // Retry again in the same frame
        Suspended = 4, // Waiting for children, not ticking

        Succeeded = 5, // Result: success
        Failed = 6, // Result: failure
        Interrupted = 7, // Result: interrupted

    }

    public static class NodeStateExtensions {

        public static Color ToColor(this NodeStatus status) {
            return status switch {
                NodeStatus.Succeeded => Colors.Green,
                NodeStatus.Failed => Colors.Red,
                NodeStatus.Interrupted => Colors.Orange,

                NodeStatus.Running => Colors.Blue,
                NodeStatus.Waiting => Colors.Blue.WithOpacity(0.75f),
                NodeStatus.Suspended => Colors.Blue.WithOpacity(0.35f),
                NodeStatus.Yield => Colors.Purple,

                _ => Colors.Grey
            };
        }

        public static bool HasTerminatedOrInitial(this NodeStatus status) =>
            status is >= NodeStatus.Succeeded or NodeStatus.Uninitialized;


        public static bool HasTerminated(this NodeStatus status) => status >= NodeStatus.Succeeded;

        public static bool HasTerminatedNegative(this NodeStatus status) => status > NodeStatus.Succeeded;

        public static bool IsActive(this NodeStatus status) =>
            status is > NodeStatus.Uninitialized and < NodeStatus.Succeeded;

        public static bool IsTicking(this NodeStatus status) => status is >= NodeStatus.Running and <= NodeStatus.Yield;

    }

    // Note to Fields: Abstraction over getters with value formatting, Abstraction over setters, possible bidirectional
}