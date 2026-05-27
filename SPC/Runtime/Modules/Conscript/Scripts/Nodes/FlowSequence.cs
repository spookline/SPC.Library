using System;
using UnityEngine;

namespace Spookline.SPC.Conscript.Nodes {


    public class TimeoutNode : ConscriptNode {

        public float timeout;
        private float _startTime;
        public ConscriptNode Child { get; set; }

        protected override void Initialize() {
            children.Add(Child);
            base.Initialize();
        }

        protected override void OnReset() {
            base.OnReset();
            _startTime = 0;
        }

        protected override NodeState OnBegin() {
            _startTime = Time.time;
            Schedule(Child);
            return NodeState.Waiting;
        }

        protected override NodeState OnUpdate() {
            if (Time.time - _startTime > timeout) return NodeState.Interrupted;
            if (TickChild(Child).HasTerminated()) return Child.State;
            return NodeState.Waiting;
        }
    }

    public class WaitNode : ConscriptNode {

        public float waitTime;
        private float _startTime;

        public WaitNode(float waitTime) {
            this.waitTime = waitTime;
        }

        protected override void OnReset() {
            base.OnReset();
            _startTime = 0;
        }

        protected override NodeState OnBegin() {
            _startTime = Time.time;
            return NodeState.Waiting;
        }

        protected override NodeState OnUpdate() {
            if (Time.time - _startTime > waitTime) return NodeState.Succeeded;
            return NodeState.Waiting;
        }

    }

    public class SimpleCondition : ConscriptNode {

        public readonly Func<bool> condition;

        public SimpleCondition(Func<bool> condition) {
            this.condition = condition;
        }

        public SimpleCondition(bool constant) {
            condition = () => constant;
        }

        protected override NodeState OnBegin() {
            return condition() ? NodeState.Succeeded : NodeState.Failed;
        }

    }

    public class Test : FlowScope {

        public void T() {

            new Condition {
                Observers = {
                    [Observe.None] = new SimpleCondition(true),
                    [Observe.Abort] = new WaitNode(2f),
                    [Observe.Interrupt] = new WaitNode(3f)
                },
                Child = new SimpleCondition(true)
            };

            new Sequence {
                new TimeoutNode()
            };
        }

    }
}