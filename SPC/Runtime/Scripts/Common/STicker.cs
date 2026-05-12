using System;
using UnityEngine;

namespace Spookline.SPC.Common {
    public class STicker {
        private readonly Action _action;
        private readonly float _delay;
        private float _next;

        public STicker(float tickRate, Action action) {
            _action = action;
            _delay = 1f / tickRate;
        }

        public void Tick() {
            var currentTime = Time.time;
            if (_next > currentTime) return;
            _next = currentTime + _delay;
            _action();
        }
    }
}