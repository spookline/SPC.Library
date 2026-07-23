using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Spookline.SPC.Audio {
    /// <summary>Gapless looping implemented with a reusable ring of cross-faded pooled voices.</summary>
    public sealed class LoopingAudioJob : IDisposable {

        public readonly float crossfadeDuration;
        public readonly int cycleCount;
        public readonly AudioJob definition;
        public readonly Transform tracked;
        public readonly AudioFadeCurve fadeCurve;
        public readonly bool randomizeFirstStartTime;

        private bool _active;
        private bool _paused;
        private bool _hasStarted;
        private bool _setupInProgress;
        private int _currentIndex;
        private AudioJobReference[] _cycle;
        private float _spatialBlend;

        public LoopingAudioJob(
            AudioJob definition,
            Transform tracked = null,
            float crossfadeDuration = 0.25f,
            int cycleCount = 2,
            float spatialBlend = 1f,
            AudioFadeCurve fadeCurve = AudioFadeCurve.SmoothStep,
            bool randomizeFirstStartTime = false
        ) {
            this.definition = definition;
            this.tracked = tracked;
            this.crossfadeDuration = Mathf.Max(0f, crossfadeDuration);
            this.cycleCount = Mathf.Max(2, cycleCount);
            _spatialBlend = Mathf.Clamp01(spatialBlend);
            this.fadeCurve = fadeCurve;
            this.randomizeFirstStartTime = randomizeFirstStartTime;
        }

        public bool IsRunning => _active && _cycle != null;
        public bool IsPaused => _paused && _cycle != null;
        public bool IsPrepared => _cycle != null && !IsDisposed;
        public float SpatialBlend => _spatialBlend;
        public bool IsDisposed { get; private set; }

        public async UniTask Setup(bool autostart = false) {
            if (IsDisposed) throw new ObjectDisposedException(nameof(LoopingAudioJob));
            if (_cycle != null || _setupInProgress) return;
            if (!definition.IsValid) throw new InvalidOperationException("A valid AudioJob is required.");

            _setupInProgress = true;
            var references = new AudioJobReference[cycleCount];
            try {
                var tasks = new UniTask[cycleCount];
                for (var i = 0; i < cycleCount; i++)
                    references[i] = definition.UnstartedSync(out tasks[i]);

                await UniTask.WhenAll(tasks).Timeout(TimeSpan.FromMinutes(1));
                if (IsDisposed) return;

                foreach (var reference in references) {
                    if (reference is not { IsValid: true })
                        throw new InvalidOperationException("Could not prepare every looping audio voice.");
                    reference.handle.onContinuation += HandleContinuation;
                    reference.handle.KeepAlive = true;
                }

                _cycle = references;
                _currentIndex = 0;
                if (autostart) Start();
            } catch {
                foreach (var reference in references) reference?.Dispose();
                throw;
            } finally {
                _setupInProgress = false;
            }
        }

        public void Start() {
            if (IsDisposed) throw new ObjectDisposedException(nameof(LoopingAudioJob));
            if (_cycle == null) {
                Debug.LogError("LoopingAudioJob is not set up. Await Setup() before Start().");
                return;
            }

            if (_active) return;
            if (_paused) {
                Resume();
                return;
            }
            _active = true;
            StartAt(_currentIndex);
        }

        public void Stop() {
            _active = false;
            _paused = false;
            if (_cycle == null) return;
            foreach (var reference in _cycle) {
                if (reference?.handle)
                    reference.handle.Fades.Reset(reference.handle.source.volume);
                reference?.Stop();
            }
        }

        public void Pause() {
            if (!_active || _paused || _cycle == null) return;
            _paused = true;
            _active = false;
            foreach (var reference in _cycle)
                if (reference?.handle) reference.handle.Pause();
        }

        public void Resume() {
            if (!_paused || _cycle == null) return;
            _paused = false;
            _active = true;
            foreach (var reference in _cycle)
                if (reference?.handle) reference.handle.Resume();
        }

        public void SetVolume(float volume) {
            if (_cycle == null) return;
            var clamped = Mathf.Clamp01(volume);
            foreach (var reference in _cycle) {
                if (reference == null) continue;
                reference.job = reference.job.With(options => options.Volume(clamped));
                if (reference.handle) reference.handle.SetVolume(clamped);
            }
        }

        public void SetPitch(float pitch) {
            if (_cycle == null) return;
            var clamped = Mathf.Clamp(pitch, -3f, 3f);
            foreach (var reference in _cycle) {
                if (reference == null) continue;
                reference.job = reference.job.With(options => options.Pitch(clamped));
                if (reference.handle?.source) reference.handle.source.pitch = clamped;
            }
        }

        public void SetSpatialBlend(float spatialBlend) {
            _spatialBlend = Mathf.Clamp01(spatialBlend);
            if (_cycle == null) return;
            foreach (var reference in _cycle) {
                if (reference == null) continue;
                reference.job = reference.job.With(options => options.SpatialBlend(_spatialBlend));
                if (reference.handle?.source)
                    reference.handle.source.spatialBlend = tracked ? _spatialBlend : 0f;
            }
        }

        public void SetMuted(bool muted) {
            if (_cycle == null) return;
            foreach (var reference in _cycle)
                if (reference?.handle?.source) reference.handle.source.mute = muted;
        }

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

        private void HandleContinuation() {
            if (!_active) return;
            StartAt((_currentIndex + 1) % cycleCount);
        }

        private void StartAt(int index) {
            if (!_active) return;
            var reference = _cycle[index];
            if (reference is not { IsValid: true }) {
                _active = false;
                Debug.LogError("LoopingAudioJob lost one of its pooled voices.");
                return;
            }

            var handle = reference.handle;
            if (crossfadeDuration > 0f) {
                handle.Fades.FadeIn(crossfadeDuration, fadeCurve);
                handle.Fades.FadeOut(crossfadeDuration, fadeCurve);
            }

            handle.source.spatialBlend = tracked ? _spatialBlend : 0f;
            if (!_hasStarted && randomizeFirstStartTime && handle.source.clip)
                handle.source.time = UnityEngine.Random.Range(
                    0f, Mathf.Max(0f, handle.source.clip.length - crossfadeDuration));
            AudioManager.Instance.BeforePlay(handle, reference.job);
            if (tracked) handle.PlayTracked(tracked);
            else handle.Play(Vector3.zero);
            _currentIndex = index;
            _hasStarted = true;
        }

    }
}
