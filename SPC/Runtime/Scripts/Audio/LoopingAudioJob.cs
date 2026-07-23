using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Spookline.SPC.Audio {
    public class LoopingAudioJob : IDisposable {

        public readonly float crossfadeDuration;
        public readonly int cycleCount;

        public readonly AudioJob definition;
        public readonly float spatialBlend;
        public readonly Transform tracked;
        private bool _active;

        private int _currentIndex;
        private AudioJobReference[] _cycle;

        public LoopingAudioJob(
            AudioJob definition,
            Transform tracked = null,
            float crossfadeDuration = 0.25f,
            int cycleCount = 2,
            float spatialBlend = 1f
        ) {
            this.definition = definition;
            this.tracked = tracked;
            this.crossfadeDuration = crossfadeDuration;
            this.cycleCount = Mathf.Max(2, cycleCount);
            this.spatialBlend = spatialBlend;
        }

        public bool IsRunning => _active && _cycle != null;

        public bool IsDisposed { get; private set; }

        public void Dispose() {
            if (IsDisposed) return;
            IsDisposed = true;
            Stop();
            if (_cycle == null) return;
            foreach (var reference in _cycle) reference?.Dispose();

            _cycle = null;
        }

        public async UniTask Setup(bool autostart = false) {
            if (_cycle != null) {
                Debug.LogWarning("LoopingAudioJob is already set up, skipping setup.");
                return;
            }

            var refs = new AudioJobReference[cycleCount];
            var tasks = new List<UniTask>();
            for (var i = 0; i < cycleCount; i++) {
                var reference = definition.UnstartedSync(out var task);
                tasks.Add(task);
                refs[i] = reference;
            }

            _cycle = refs;
            await UniTask.WhenAll(tasks).TimeoutWithoutException(TimeSpan.FromMinutes(1));
            if (IsDisposed) return;
            for (var i = 0; i < cycleCount; i++) {
                var reference = refs[i];
                var finalizedIndex = i;
                reference.handle.onContinuation += () => OnFadeOutBegin(finalizedIndex);
                reference.handle.KeepAlive = true;
            }

            _currentIndex = 0;
            if (autostart) Start();
        }

        public void Start() {
            if (_cycle == null) {
                Debug.LogError("LoopingAudioJob is not set up, call Setup() first.");
                return;
            }

            _active = true;
            StartAt(0);
        }

        public void Stop() {
            _active = false;
            if (_cycle == null) return;
            foreach (var reference in _cycle) reference?.Stop();
        }

        private void OnFadeOutBegin(int index) {
            if (!_active) return;
            var nextIndex = (index + 1) % cycleCount;
            StartAt(nextIndex);
        }

        private void StartAt(int index) {
            if (!_active) return;
            var reference = _cycle[index];
            var handle = reference.handle;
            if (crossfadeDuration > 0) {
                handle.Fades.FadeIn(crossfadeDuration);
                handle.Fades.FadeOut(crossfadeDuration);
            }

            if (tracked) {
                handle.source.spatialBlend = spatialBlend;
                handle.PlayTracked(tracked);
            } else {
                handle.source.spatialBlend = 0f;
                handle.Play(Vector3.zero);
            }

            _currentIndex = index;
        }

    }
}