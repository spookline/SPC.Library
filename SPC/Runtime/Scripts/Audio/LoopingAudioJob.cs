using System;
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
        private bool _setupInProgress;

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
            foreach (var reference in _cycle) {
                if (reference?.handle) {
                    reference.handle.onContinuation -= HandleContinuation;
                    reference.handle.KeepAlive = false;
                }
                reference?.Dispose();
            }

            _cycle = null;
        }

        public async UniTask Setup(bool autostart = false) {
            if (IsDisposed || _cycle != null || _setupInProgress) {
                Debug.LogWarning("LoopingAudioJob is already set up, skipping setup.");
                return;
            }

            if (!definition.definition) {
                Debug.LogError("LoopingAudioJob requires a valid AudioJob definition.");
                return;
            }

            _setupInProgress = true;
            var refs = new AudioJobReference[cycleCount];
            try {
                var tasks = new UniTask[cycleCount];
                for (var i = 0; i < cycleCount; i++) {
                    refs[i] = definition.UnstartedSync(out tasks[i]);
                }

                await UniTask.WhenAll(tasks).Timeout(TimeSpan.FromMinutes(1));
                if (IsDisposed) return;
                for (var i = 0; i < cycleCount; i++) {
                    if (refs[i] == null || !refs[i].IsValid) {
                        Debug.LogError("LoopingAudioJob could not prepare all audio handles.");
                        foreach (var prepared in refs) prepared?.Dispose();
                        return;
                    }
                    refs[i].handle.onContinuation += HandleContinuation;
                    refs[i].handle.KeepAlive = true;
                }

                _cycle = refs;
                _currentIndex = 0;
                if (autostart) Start();
            } catch (Exception exception) {
                Debug.LogException(exception);
                foreach (var reference in refs) reference?.Dispose();
            } finally {
                _setupInProgress = false;
            }
        }

        public void Start() {
            if (IsDisposed || _cycle == null) {
                Debug.LogError("LoopingAudioJob is not set up, call Setup() first.");
                return;
            }

            if (_active) return;
            _active = true;
            StartAt(_currentIndex);
        }

        public void Stop() {
            _active = false;
            if (_cycle == null) return;
            foreach (var reference in _cycle) {
                if (reference?.handle) reference.handle.Fades.Reset(reference.handle.source.volume);
                reference?.Stop();
            }
        }

        private void HandleContinuation() {
            if (!_active) return;
            var nextIndex = (_currentIndex + 1) % cycleCount;
            StartAt(nextIndex);
        }

        private void StartAt(int index) {
            if (!_active) return;
            var reference = _cycle[index];
            if (reference is not { IsValid: true }) {
                _active = false;
                Debug.LogError("LoopingAudioJob lost its audio handle.");
                return;
            }
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