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
            public List<NodeState> StateChanges { get; } = new List<NodeState>();

            public NodeState NextBeginState = NodeState.Running;
            public NodeState NextUpdateState = NodeState.Succeeded;
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

            // 1st Tick: Root Begins -> Running. Ticks children. 
            // Interrupter and target are not scheduled yet.
            hierarchy.Tick();

            // Schedule target from root update in next tick
            root.NodeToScheduleOnUpdate = target;
            hierarchy.Tick();


            Assert.AreEqual(NodeState.Running, target.State);
            Assert.AreEqual(1, target.Interrupters.Count);

            // Interruption is NOT automatic in Tick, it depends on node logic calling WillInterrupt()
            // and then changing state.
            Assert.IsFalse(target.WillInterrupt());

            // Set interruption condition
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
            
            // Tick 1: Root Begins, schedules child. Then Root ticks children -> child Begins -> Succeeded.
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

            // Tick 1: Root Begins (Running), schedules child. Root ticks children -> child Begins (Running).
            hierarchy.Tick();

            Assert.AreEqual(NodeState.Running, root.State);
            Assert.AreEqual(NodeState.Running, child.State);

            child.NextUpdateState = NodeState.Succeeded;
            // Tick 2: Root Ticks (stays Running). Root ticks children -> child Ticks (Succeeded).
            // Child success calls Parent.NotifyChildComplete.
            hierarchy.Tick();

            Assert.AreEqual(NodeState.Succeeded, child.State);
            Assert.AreEqual(1, root.OnChildCompleteCount);
            Assert.AreEqual(NodeState.Succeeded, root.LastChildCompleteResult);
        }
    }
}






