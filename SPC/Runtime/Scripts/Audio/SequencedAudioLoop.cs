using System;
using UnityEngine;

namespace Spookline.SPC.Audio {
    public sealed class SequencedAudioLoop : IDisposable {
        private readonly LoopingAudioJob _loop;
        private bool _started;

        public SequencedAudioLoop(LoopingAudioJob loop) {
            _loop = loop ?? throw new ArgumentNullException(nameof(loop));
        }

        public bool IsRunning => _started && _loop.IsRunning;

        public void Start() {
            if (_started) return;
            _started = true;
            _loop.Start();
        }

        public void Stop() {
            if (!_started) return;
            _started = false;
            _loop.Stop();
        }

        public void Dispose() {
            Stop();
            _loop.Dispose();
        }
    }
}