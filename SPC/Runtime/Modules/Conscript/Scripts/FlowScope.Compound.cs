using System.Linq;
using HELIX.Widgets.Universal;
using Spookline.SPC.Conscript.UI;
using UnityEngine;

namespace Spookline.SPC.Conscript.Nodes {
    public abstract partial class FlowScope {

        public class Sequence : CollectionConscriptNode {

            private int _index;

            protected override void OnReset() {
                _index = 0;
            }

            protected override NodeStatus OnBegin() {
                InterruptAndReset();
                _index = 0;
                return NodeStatus.Waiting;
            }

            protected override NodeStatus OnUpdate() {
                while (_index < Children.Count) {
                    var child = Children[_index];
                    if (child.Status is NodeStatus.Succeeded) {
                        _index++;
                        continue;
                    } else if (child.Status.HasTerminatedNegative()) { return NodeStatus.Failed; }

                    var status = Run(child);
                    if (status.IsActive()) { return NodeStatus.Waiting; }
                }

                return NodeStatus.Succeeded;
            }

        }

        public class Select : CollectionConscriptNode {

            private int _index;
            public bool retryOnInterrupt;

            public Select(bool retryOnInterrupt = true) {
                this.retryOnInterrupt = retryOnInterrupt;
            }

            protected internal override NodeTemplate BuildWidget() {
                return new NodeTemplate(this) {
                    content = new HText("Select"),
                    parallel = children.Select(c => c.BuildWidget()).ToList()
                };
            }


            protected override void OnReset() {
                _index = 0;
            }

            protected override NodeStatus OnBegin() {
                _index = 0;
                children.InterruptAndReset();
                return NodeStatus.Waiting;
            }

            protected override NodeStatus OnUpdate() {
                while (_index < Children.Count) {
                    var child = Children[_index];
                    if (child.Status is NodeStatus.Succeeded) { return NodeStatus.Succeeded; } else if
                        (child.Status is NodeStatus.Interrupted) {
                        if (retryOnInterrupt) {
                            children.InterruptAndReset();
                            _index = 0;
                            Debug.LogWarning("Retrying interrupted node");
                            return NodeStatus.Yield;
                        }

                        return NodeStatus.Interrupted;
                    } else if (child.Status.HasTerminatedNegative()) {
                        _index++;
                        continue;
                    }

                    var status = Run(child);
                    if (status.IsActive()) { return NodeStatus.Waiting; }
                }

                return NodeStatus.Failed;
            }

        }

        public class Parallel : CollectionConscriptNode {

            public readonly WaitMode wait;
            public readonly bool failing;

            public Parallel(WaitMode wait = WaitMode.None, bool failing = true) {
                this.wait = wait;
                this.failing = failing;
            }

            protected internal override NodeTemplate BuildWidget() {
                return new NodeTemplate(this) {
                    content = new HText("Parallel"),
                    parallel = children.Select(c => c.BuildWidget()).ToList()
                };
            }

            protected override NodeStatus OnBegin() {
                foreach (var child in children) { Schedule(child); }

                return NodeStatus.Waiting;
            }

            protected override NodeStatus OnUpdate() {
                var completedCount = 0;
                var hasFailed = false;
                if (wait == WaitMode.None) {
                    foreach (var child in children) {
                        var state = Run(child);
                        if (state.HasTerminatedNegative()) hasFailed = true;
                    }
                }

                foreach (var child in children) {
                    if (child.Status.IsActive()) { Continue(child); } else {
                        completedCount++;
                        if (child.Status.HasTerminatedNegative()) hasFailed = true;
                    }
                }

                if (failing && hasFailed) {
                    InterruptAndResetChildren();
                    return NodeStatus.Failed;
                }

                return wait switch {
                    WaitMode.None => NodeStatus.Running,
                    WaitMode.All => completedCount >= children.Count ? NodeStatus.Succeeded : NodeStatus.Waiting,
                    WaitMode.Any => completedCount > 0 ? NodeStatus.Succeeded : NodeStatus.Waiting,
                    _ => NodeStatus.Running
                };
            }

        }

        public enum WaitMode {

            None,
            All,
            Any

        }

    }
}