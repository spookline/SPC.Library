using System;
using System.Linq;
using UnityEngine;

namespace Spookline.SPC.Conscript.Nodes {
    public class TimeoutNode : CollectionConscriptNode {

        public float timeout;
        private float _startTime;

        public TimeoutNode(float timeout) {
            this.timeout = timeout;
        }

        protected override int MaxCount => 1;

        protected override void Initialize() {
            var child = children.FirstOrDefault();
            if (child == null) throw new Exception("TimeoutNode must have exactly one child.");
            base.Initialize();
        }

        protected override void OnReset() {
            base.OnReset();
            _startTime = 0;
        }

        protected override NodeStatus OnBegin() {
            _startTime = Time.time;
            Schedule(children.First());
            return NodeStatus.Waiting;
        }

        protected override NodeStatus OnUpdate() {
            if (Time.time - _startTime > timeout) return NodeStatus.Interrupted;
            var child = children.First();
            if (Continue(child).HasTerminated()) return child.Status;
            return NodeStatus.Waiting;
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

        protected override NodeStatus OnBegin() {
            _startTime = Time.time;
            return NodeStatus.Running;
        }

        protected override NodeStatus OnUpdate() {
            if (Time.time - _startTime > waitTime) return NodeStatus.Succeeded;
            return NodeStatus.Running;
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

        protected override NodeStatus OnBegin() {
            return condition() ? NodeStatus.Succeeded : NodeStatus.Failed;
        }

    }
}