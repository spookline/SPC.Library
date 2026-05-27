using System;
using System.Collections.Generic;

namespace Spookline.SPC.Conscript.Nodes {
    public abstract partial class FlowScope {

        public class Condition : ConscriptNode {

            public Condition() {
                Observers = new Observers();
            }

            public Condition(Observers observers, ConscriptNode child) {
                Observers = observers;
                Child = child;
            }

            public Observers Observers { get; }
            public ConscriptNode Child { get; set; }

            public override bool IsInterrupter => Observers.hasInterrupter;

            protected override void Initialize() {
                children.AddRange(Observers.Nodes);
                if (Child != null) children.Add(Child);
                base.Initialize();
            }

            protected override NodeState OnBegin() {
                Observers.Reset();
                Child.Reset();
                var matches = Observers.CheckPreconditions(this);
                return matches ? NodeState.Waiting : NodeState.Failed;
            }

            protected override void OnEnd() { }

            protected override NodeState OnUpdate() {
                var conditions = Observers.CheckRunningConditions(this);
                if (conditions) {
                    if (Child != null) return ScheduleImmediate(Child);
                    return NodeState.Succeeded;
                }

                return NodeState.Failed;
            }

            public override bool WillInterruptNode(ConscriptNode node) {
                return Observers.CheckInterruptConditions(this);
            }

        }


        public class Observers {

            private readonly List<ConscriptNode> _nodes = new();
            private readonly List<Observe> _flags = new();

            public bool hasInterrupter;
            public int Count => _nodes.Count;
            public bool IsEmpty => _nodes.Count == 0;
            public List<ConscriptNode> Nodes => _nodes;

            public void Add(Observe observe, ConscriptNode node) {
                _nodes.Add(node);
                _flags.Add(observe);
                if (observe.HasFlag(Observe.Interrupt)) hasInterrupter = true;
            }

            public ConscriptNode this[Observe index] {
                get => throw new NotImplementedException("Indexing by Conditional is not implemented");
                set => Add(index, value);
            }

            public void Get(int i, out ConscriptNode node, out Observe observe) {
                node = _nodes[i];
                observe = _flags[i];
            }

            public bool CheckPreconditions(ConscriptNode parent) {
                for (var i = 0; i < Count; i++) {
                    var opts = _flags[i];
                    if (!opts.HasFlag(Observe.Guard)) continue;
                    var node = _nodes[i];
                    var result = parent.ScheduleImmediateInternal(node);
                    if (result.HasTerminatedNegative()) return false;
                }

                return true;
            }

            public bool CheckRunningConditions(ConscriptNode parent) {
                for (var i = 0; i < Count; i++) {
                    var opts = _flags[i];
                    if (!opts.HasFlag(Observe.Abort)) continue;
                    var node = _nodes[i];
                    var result = parent.ScheduleImmediateInternal(node);
                    if (result.HasTerminatedNegative()) return false;
                }

                return true;
            }

            public bool CheckInterruptConditions(ConscriptNode parent) {
                for (var i = 0; i < Count; i++) {
                    var opts = _flags[i];
                    if (!opts.HasFlag(Observe.Interrupt)) continue;
                    var node = _nodes[i];
                    var result = parent.ScheduleImmediateInternal(node);
                    if (result.HasTerminatedNegative()) return false;
                }

                return true;
            }

            public void Reset() {
                foreach (var node in _nodes) { node.InterruptAndReset(); }
            }

        }

        [Flags]
        public enum Observe {

            /// <summary>
            /// Only check this condition as a precondition.
            /// </summary>
            None = 0,

            /// <summary>
            /// Checks this condition as a precondition.
            /// If this condition fails, the subtree will not begin.
            /// </summary>
            Guard = 1 << 1,

            /// <summary>
            /// Check this condition as a running condition.
            /// If this condition fails, the subtree will be interrupted.
            /// </summary>
            Abort = 1 << 2,

            /// <summary>
            /// This condition will interrupt lower priority siblings if it succeeds.
            /// </summary>
            Interrupt = 1 << 3,

        }

    }
}