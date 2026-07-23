using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Spookline.SPC.Audio {
    /// <summary>A pooled runtime voice. Instances are owned by <see cref="AudioManager"/>.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioHandle : MonoBehaviour {

        [HideInInspector]
        public AudioSource source;

        [HideInInspector]
        public Transform tracked;

        public Action onContinuation;
        public Action onEnd;

        private AudioJobReference _jobReference;
        private UniTaskCompletionSource _endedSource;
        private Transform _cachedTransform;
        private float _targetVolume;
        private bool _hasCalledContinuation;

        public AudioHandleState State { get; private set; }
        public AudioFadeController Fades { get; } = new();
        public bool KeepAlive { get; set; }
        public bool IsPlaying => source && source.isPlaying;
        public bool HasEnded => State is AudioHandleState.Ended or AudioHandleState.Released;
        public bool IsReleased => State == AudioHandleState.Released || _jobReference == null;
        internal AudioJob Job => _jobReference?.job ?? default;

        private void Awake() {
            _cachedTransform = transform;
            source = GetComponent<AudioSource>();
        }

        private void Update() {
            if (State is AudioHandleState.Released or AudioHandleState.Idle or
                AudioHandleState.Ended or AudioHandleState.Paused) return;
            if (_jobReference is { IsValid: false }) {
                Kill();
                return;
            }

            if (tracked) _cachedTransform.position = tracked.position;
            else tracked = null;

            if (!source || !source.clip || !source.isPlaying) {
                EndNow();
                return;
            }

            State = AudioHandleState.Playing;
            var clipLength = source.clip.length;
            Fades.Tick(source.time, clipLength, source);
            if (Fades.IsFadeOutActive(source.time, clipLength)) CallContinuation();
        }

        private void OnDestroy() {
            if (!IsReleased && AudioManager.IsInitialized) AudioManager.Instance.Release(this);
        }

        internal void Assign(AudioJobReference reference) {
            _jobReference = reference;
            _endedSource = new UniTaskCompletionSource();
            _hasCalledContinuation = false;
            State = AudioHandleState.Idle;
            CaptureSourceSettings();
        }

        internal void CaptureSourceSettings() {
            _targetVolume = source ? source.volume : 1f;
            Fades.Reset(_targetVolume);
        }

        internal void SetVolume(float volume) {
            _targetVolume = Mathf.Clamp01(volume);
            Fades.SetTargetVolume(_targetVolume);
            if (source) source.volume = _targetVolume;
        }

        internal void ResetForPool() {
            if (source) {
                source.Stop();
                source.clip = null;
                source.outputAudioMixerGroup = null;
                source.loop = false;
                source.mute = false;
                source.bypassEffects = false;
                source.bypassListenerEffects = false;
                source.bypassReverbZones = false;
                AudioOptions.Default.ApplyTo(source);
                source.spatialBlend = 0f;
                source.spatialize = false;
            }

            _jobReference = null;
            State = AudioHandleState.Released;
            Fades.Reset(1f);
            tracked = null;
            KeepAlive = false;
            _hasCalledContinuation = false;
            onEnd = null;
            onContinuation = null;
            _endedSource?.TrySetResult();
            _endedSource = null;
        }

        public bool IsOwnedBy(AudioJobReference reference) {
            return _jobReference != null && ReferenceEquals(_jobReference, reference);
        }

        public async UniTask WaitUntilEnded() {
            if (HasEnded) return;
            if (_endedSource != null) await _endedSource.Task;
        }

        public UniTask.Awaiter GetAwaiter() => WaitUntilEnded().GetAwaiter();

        public void EndNow() {
            if (HasEnded) return;

            if (source) source.Stop();
            State = AudioHandleState.Ended;
            onEnd?.Invoke();
            _endedSource?.TrySetResult();
            CallContinuation();

            if (KeepAlive) {
                State = AudioHandleState.Idle;
                return;
            }

            if (!IsReleased && AudioManager.IsInitialized) AudioManager.Instance.Release(this);
        }

        public void Kill() {
            if (!IsReleased && AudioManager.IsInitialized) AudioManager.Instance.Release(this);
        }

        public void Pause() {
            if (!source || State is not (AudioHandleState.Starting or AudioHandleState.Playing)) return;
            source.Pause();
            State = AudioHandleState.Paused;
        }

        public void Resume() {
            if (!source || State != AudioHandleState.Paused) return;
            source.UnPause();
            State = AudioHandleState.Playing;
        }

        public void Play(Vector3 position) {
            _cachedTransform.position = position;
            tracked = null;
            PrepareStart();
        }

        public void PlayTracked(Transform target) {
            if (!target) {
                Play(Vector3.zero);
                return;
            }

            tracked = target;
            _cachedTransform.position = target.position;
            PrepareStart();
        }

        private void PrepareStart() {
            if (!source || !source.clip) {
                Debug.LogError("Cannot play an audio handle without a clip.");
                EndNow();
                return;
            }

            _hasCalledContinuation = false;
            _endedSource = new UniTaskCompletionSource();
            State = AudioHandleState.Starting;
            source.volume = _targetVolume;
            source.Play();
        }

        private void CallContinuation() {
            if (_hasCalledContinuation) return;
            _hasCalledContinuation = true;
            onContinuation?.Invoke();
        }

    }
}
