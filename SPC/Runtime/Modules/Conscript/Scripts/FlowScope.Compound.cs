using UnityEngine;

namespace Spookline.SPC.Conscript.Nodes {
    public abstract partial class FlowScope {

        public class Sequence : CollectionConscriptNode {

            private int _index;

            protected override void OnReset() {
                _index = 0;
            }

            protected override NodeState OnBegin() {
                InterruptAndReset();
                _index = 0;
                return NodeState.Waiting;
            }

            protected override NodeState OnUpdate() {
                while (_index < Children.Count) {
                    var child = Children[_index];
                    if (child.State is NodeState.Succeeded) {
                        _index++;
                        continue;
                    } else if (child.State.HasTerminatedNegative()) {
                        return NodeState.Failed;
                    }

                    var status = ScheduleImmediate(child);
                    if (status.IsActive()) {
                        return NodeState.Waiting;
                    }
                }

                return NodeState.Succeeded;
            }

        }

        public class Select : CollectionConscriptNode {

            private int _index;
            public bool retryOnInterrupt = true;

            protected override void OnReset() {
                _index = 0;
            }

            protected override NodeState OnBegin() {
                _index = 0;
                children.InterruptAndReset();
                return NodeState.Waiting;
            }

            protected override NodeState OnUpdate() {
                while (_index < Children.Count) {
                    var child = Children[_index];
                    if (child.State is NodeState.Succeeded) {
                        return NodeState.Succeeded;
                    } else if (child.State is NodeState.Interrupted) {
                        if (retryOnInterrupt) {
                            children.InterruptAndReset();
                            _index = 0;
                            Debug.LogWarning("Retrying interrupted node");
                            return NodeState.Yield;
                        }
                        return NodeState.Interrupted;
                    } else if (child.State.HasTerminatedNegative()) {
                        _index++;
                        continue;
                    }

                    var status = ScheduleImmediate(child);
                    if (status.IsActive()) {
                        return NodeState.Waiting;
                    }
                }

                return NodeState.Failed;
            }
        }

    }
}