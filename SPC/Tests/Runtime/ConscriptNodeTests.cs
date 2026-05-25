using System.Collections.Generic;
using NUnit.Framework;
using Spookline.SPC.Conscript;

namespace Spookline.SPC.Tests {
    public class ConscriptNodeTests {

        private class TestNode : ConscriptNode {

            public int BeginCount { get; private set; }
            public int UpdateCount { get; private set; }
            public int EndCount { get; private set; }
            public int ResetCount { get; private set; }
            public int InterruptCount { get; private set; }
            public List<NodeState> StateChanges { get; } = new();
            public NodeState NextBeginState { get; set; } = NodeState.Running;
            public NodeState NextUpdateState { get; set; } = NodeState.Succeeded;
            public ConscriptNode NodeToScheduleOnUpdate { get; set; }
            public ConscriptNode NodeToScheduleOnBegin { get; set; }

            protected override NodeState OnBegin() {
                BeginCount++;
                if (NodeToScheduleOnBegin != null) {
                    Schedule(NodeToScheduleOnBegin);
                    NodeToScheduleOnBegin = null;
                }

                return NextBeginState;
            }

            protected override NodeState OnUpdate() {
                UpdateCount++;
                if (NodeToScheduleOnUpdate != null) {
                    Schedule(NodeToScheduleOnUpdate);
                    NodeToScheduleOnUpdate = null;
                }

                return NextUpdateState;
            }

            protected override void OnEnd() {
                EndCount++;
            }

            protected override void OnReset() {
                ResetCount++;
            }

            protected override void OnInterrupt() {
                InterruptCount++;
            }

            protected override void OnStateChange(NodeState previous, NodeState next) {
                StateChanges.Add(next);
            }

            public int OnChildCompleteCount { get; private set; }
            public NodeState LastChildCompleteResult { get; private set; }

            protected override NodeState OnChildComplete(ConscriptNode child, NodeState result) {
                OnChildCompleteCount++;
                LastChildCompleteResult = result;
                return base.OnChildComplete(child, result);
            }

        }

        [Test]
        public void Hierarchy_Link_SetsParentAndIndex() {
            var root = new TestNode();
            var child1 = new TestNode();
            var child2 = new TestNode();
            root.Add(child1);
            root.Add(child2);

            var hierarchy = new ConscriptHierarchy(root);

            Assert.AreEqual(root, child1.Parent);
            Assert.AreEqual(root, child2.Parent);
            Assert.AreEqual(0, root.HierarchyIndex);
            Assert.AreEqual(1, child1.HierarchyIndex);
            Assert.AreEqual(2, child2.HierarchyIndex);
        }

        [Test]
        public void Hierarchy_Tick_StartsRoot() {
            var root = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Succeeded };
            var hierarchy = new ConscriptHierarchy(root);

            hierarchy.Tick();

            Assert.AreEqual(1, root.BeginCount);
            Assert.AreEqual(1, root.UpdateCount);
            Assert.AreEqual(NodeState.Succeeded, root.State);
            Assert.AreEqual(1, root.EndCount);
        }

        [Test]
        public void Hierarchy_Exit_On_Begin() {
            var root = new TestNode { NextBeginState = NodeState.Failed };
            var hierarchy = new ConscriptHierarchy(root);

            hierarchy.Tick(); // Begin & Running

            Assert.AreEqual(1, root.BeginCount);
            Assert.AreEqual(0, root.UpdateCount);
            Assert.AreEqual(NodeState.Failed, root.State);
            Assert.AreEqual(1, root.EndCount);
        }

        private class InterrupterNode : TestNode {

            public override bool IsInterrupter => true;
            public bool ShouldInterrupt = false;

            public override bool WillInterruptNode(ConscriptNode node) {
                return ShouldInterrupt;
            }

        }

        [Test]
        public void Interruption_Siblings_InterruptsWhenConditionMet() {
            var root = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Running };
            var interrupter = new InterrupterNode();
            var target = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Running };

            root.Add(interrupter);
            root.Add(target);

            var hierarchy = new ConscriptHierarchy(root);
            hierarchy.Tick();
            root.NodeToScheduleOnUpdate = target;
            hierarchy.Tick();

            Assert.AreEqual(NodeState.Running, target.State);
            Assert.AreEqual(1, target.Interrupters.Count);
            Assert.IsFalse(target.WillInterrupt());

            interrupter.ShouldInterrupt = true;
            Assert.IsTrue(target.WillInterrupt());

            hierarchy.Tick();
            Assert.AreEqual(1, target.InterruptCount);
            Assert.AreEqual(NodeState.Interrupted, target.State);
        }

        [Test]
        public void Hierarchy_Reset_ResetsAllNodes() {
            var root = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Running };
            var child = new TestNode { NextBeginState = NodeState.Running };
            root.Add(child);
            var hierarchy = new ConscriptHierarchy(root);
            root.NodeToScheduleOnBegin = child;
            hierarchy.Tick();

            Assert.AreEqual(NodeState.Running, root.State);
            Assert.AreEqual(NodeState.Succeeded, child.State);

            hierarchy.Reset();
            Assert.AreEqual(NodeState.Uninitialized, root.State);
            Assert.AreEqual(NodeState.Uninitialized, child.State);
            Assert.AreEqual(1, root.ResetCount);
            Assert.AreEqual(1, child.ResetCount);
        }

        [Test]
        public void Node_StateChange_NotifiesParent() {
            var root = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Running };
            var child = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Running };
            root.Add(child);
            var hierarchy = new ConscriptHierarchy(root);

            root.NodeToScheduleOnBegin = child;
            hierarchy.Tick();
            Assert.AreEqual(NodeState.Running, root.State);
            Assert.AreEqual(NodeState.Running, child.State);

            child.NextUpdateState = NodeState.Succeeded;
            hierarchy.Tick();
            Assert.AreEqual(NodeState.Succeeded, child.State);
            Assert.AreEqual(1, root.OnChildCompleteCount);
            Assert.AreEqual(NodeState.Succeeded, root.LastChildCompleteResult);
        }

        [Test]
        public void Suspend_State_DoesNotTick() {
            var root = new TestNode { NextBeginState = NodeState.Suspended };
            var hierarchy = new ConscriptHierarchy(root);

            hierarchy.Tick();
            Assert.AreEqual(NodeState.Suspended, root.State);
            Assert.AreEqual(0, root.UpdateCount);
            Assert.AreEqual(0, root.EndCount);
        }

        [Test]
        public void Waiting_State_ContinuesTicking() {
            var root = new TestNode { NextBeginState = NodeState.Waiting, NextUpdateState = NodeState.Succeeded };
            var hierarchy = new ConscriptHierarchy(root);

            hierarchy.Tick();
            Assert.AreEqual(1, root.UpdateCount);
            Assert.AreEqual(NodeState.Succeeded, root.State);
        }

        [Test]
        public void Child_Interruption_PropagatesFromParent() {
            var root = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Succeeded };
            var child1 = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Running };
            var child2 = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Running };
            root.Add(child1);
            root.Add(child2);
            var hierarchy = new ConscriptHierarchy(root);

            root.NodeToScheduleOnBegin = child1;
            hierarchy.Tick();
            child1.NodeToScheduleOnUpdate = child2;
            hierarchy.Tick();

            Assert.AreEqual(NodeState.Running, child1.State);
            Assert.AreEqual(NodeState.Running, child2.State);

            hierarchy.Tick();

            Assert.AreEqual(NodeState.Succeeded, root.State);
            Assert.AreEqual(NodeState.Interrupted, child1.State);
            Assert.AreEqual(NodeState.Interrupted, child2.State);
            Assert.AreEqual(1, child1.InterruptCount);
            Assert.AreEqual(1, child2.InterruptCount);
        }

        [Test]
        public void Multiple_Interrupters_OnlyCurrentOnesInterrupt() {
            var root = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Running };
            var interrupter1 = new InterrupterNode();
            var interrupter2 = new InterrupterNode();
            var target = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Running };

            root.Add(interrupter1);
            root.Add(interrupter2);
            root.Add(target);

            var hierarchy = new ConscriptHierarchy(root);
            hierarchy.Tick();

            root.NodeToScheduleOnUpdate = target;
            hierarchy.Tick();

            Assert.AreEqual(2, target.Interrupters.Count);

            interrupter1.ShouldInterrupt = true;
            hierarchy.Tick();

            Assert.AreEqual(NodeState.Interrupted, target.State);
            Assert.AreEqual(1, target.InterruptCount);
        }

        [Test]
        public void OnChildComplete_CanChangeParentState() {
            var root = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Running };
            var child = new TestNode { NextBeginState = NodeState.Failed };
            root.Add(child);
            var hierarchy = new ConscriptHierarchy(root);

            root.NodeToScheduleOnBegin = child;
            hierarchy.Tick();

            Assert.AreEqual(1, root.OnChildCompleteCount);
            Assert.AreEqual(NodeState.Failed, root.LastChildCompleteResult);
        }

        [Test]
        public void StateChanges_AreTracked() {
            var node = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Succeeded };
            var hierarchy = new ConscriptHierarchy(node);

            hierarchy.Tick();

            Assert.AreEqual(2, node.StateChanges.Count);
            Assert.AreEqual(NodeState.Running, node.StateChanges[0]);
            Assert.AreEqual(NodeState.Succeeded, node.StateChanges[1]);
        }

        [Test]
        public void Node_DoesNotCheckInterruptsOnStartFrame() {
            var root = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Running };
            var interrupter = new InterrupterNode { ShouldInterrupt = true };
            var target = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Running };

            root.Add(interrupter);
            root.Add(target);

            var hierarchy = new ConscriptHierarchy(root);
            hierarchy.Tick();
            Assert.AreEqual(NodeState.Uninitialized, target.State);

            root.NodeToScheduleOnUpdate = target;
            hierarchy.Tick();
            Assert.AreEqual(NodeState.Running, target.State);

            hierarchy.Tick();
            Assert.AreEqual(NodeState.Interrupted, target.State);
        }

        [Test]
        public void Reset_DoesNotResetUninitializedNode() {
            var node = new TestNode();
            var hierarchy = new ConscriptHierarchy(node);

            hierarchy.Reset();
            Assert.AreEqual(0, node.ResetCount);
            Assert.AreEqual(NodeState.Uninitialized, node.State);
        }

        [Test]
        public void ParentReceivesCompletionAfterInterruption() {
            var root = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Running };
            var interrupter = new InterrupterNode { ShouldInterrupt = true };
            var target = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Running };

            root.Add(interrupter);
            root.Add(target);

            var hierarchy = new ConscriptHierarchy(root);
            hierarchy.Tick();

            root.NodeToScheduleOnUpdate = target;
            hierarchy.Tick();
            Assert.AreEqual(0, root.OnChildCompleteCount);

            hierarchy.Tick();
            Assert.AreEqual(NodeState.Interrupted, target.State);
            Assert.AreEqual(1, root.OnChildCompleteCount);
            Assert.AreEqual(NodeState.Interrupted, root.LastChildCompleteResult);
        }

        [Test]
        public void ChildrenEnumeration_Works() {
            var root = new TestNode();
            var child1 = new TestNode();
            var child2 = new TestNode();
            root.Add(child1);
            root.Add(child2);

            var hierarchy = new ConscriptHierarchy(root);

            var childList = new List<ConscriptNode>(root);
            Assert.AreEqual(2, childList.Count);
            Assert.AreEqual(child1, childList[0]);
            Assert.AreEqual(child2, childList[1]);
        }

        [Test]
        public void MultipleChildrenComplete_ParentTracksIndividually() {
            var root = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Running };
            var child1 = new TestNode { NextBeginState = NodeState.Succeeded };
            var child2 = new TestNode { NextBeginState = NodeState.Failed };
            var child3 = new TestNode { NextBeginState = NodeState.Interrupted };

            root.Add(child1);
            root.Add(child2);
            root.Add(child3);

            var hierarchy = new ConscriptHierarchy(root);
            hierarchy.Tick();

            root.NodeToScheduleOnUpdate = child1;
            hierarchy.Tick();
            Assert.AreEqual(1, root.OnChildCompleteCount);

            root.NodeToScheduleOnUpdate = child2;
            hierarchy.Tick();
            Assert.AreEqual(2, root.OnChildCompleteCount);

            root.NodeToScheduleOnUpdate = child3;
            hierarchy.Tick();
            Assert.AreEqual(3, root.OnChildCompleteCount);

            Assert.AreEqual(NodeState.Interrupted, root.LastChildCompleteResult);
        }

        [Test]
        public void FailedBegin_ImmediatelyTerminates() {
            var root = new TestNode { NextBeginState = NodeState.Failed };
            var hierarchy = new ConscriptHierarchy(root);

            hierarchy.Tick();

            Assert.AreEqual(NodeState.Failed, root.State);
            Assert.AreEqual(1, root.BeginCount);
            Assert.AreEqual(0, root.UpdateCount);
            Assert.AreEqual(1, root.EndCount);
        }

        [Test]
        public void InterruptedBegin_ImmediatelyTerminates() {
            var root = new TestNode { NextBeginState = NodeState.Interrupted };
            var hierarchy = new ConscriptHierarchy(root);

            hierarchy.Tick();

            Assert.AreEqual(NodeState.Interrupted, root.State);
            Assert.AreEqual(1, root.BeginCount);
            Assert.AreEqual(0, root.UpdateCount);
        }

        [Test]
        public void WillInterrupt_ChecksInterrupters() {
            var root = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Running };
            var interrupter = new InterrupterNode { ShouldInterrupt = false };
            var target = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Running };

            root.Add(interrupter);
            root.Add(target);

            var hierarchy = new ConscriptHierarchy(root);
            hierarchy.Tick();

            root.NodeToScheduleOnUpdate = target;
            hierarchy.Tick();

            Assert.IsFalse(target.WillInterrupt());

            interrupter.ShouldInterrupt = true;
            Assert.IsTrue(target.WillInterrupt());
        }

        [Test]
        public void SequentialChildScheduling_ExecutesInOrder() {
            var root = new TestNode { NextBeginState = NodeState.Running, NextUpdateState = NodeState.Running };
            var child1 = new TestNode { NextBeginState = NodeState.Succeeded };
            var child2 = new TestNode { NextBeginState = NodeState.Succeeded };

            root.Add(child1);
            root.Add(child2);

            var hierarchy = new ConscriptHierarchy(root);
            hierarchy.Tick();

            root.NodeToScheduleOnUpdate = child1;
            hierarchy.Tick();
            Assert.AreEqual(NodeState.Succeeded, child1.State);
            Assert.AreEqual(NodeState.Uninitialized, child2.State);

            root.NodeToScheduleOnUpdate = child2;
            hierarchy.Tick();
            Assert.AreEqual(NodeState.Succeeded, child1.State);
            Assert.AreEqual(NodeState.Succeeded, child2.State);
        }

    }
}