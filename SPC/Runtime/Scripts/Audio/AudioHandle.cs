using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Spookline.SPC.Audio {
    public class AudioHandle : MonoBehaviour {

        [HideInInspector]
        public AudioSource source;

        [HideInInspector]
        public Transform tracked;
        private bool _hasCalledContinuation;

        private AudioJobReference _jobReference;
        private UniTaskCompletionSource _endedSource;
        private float _targetVolume;
        private Transform _transform;
        public Action onContinuation;

        public Action onEnd;

        public AudioHandleState State { get; private set; }

        public bool IsPlaying => source.isPlaying;
        public bool HasEnded => State is AudioHandleState.Ended or AudioHandleState.Released;
        public bool KeepAlive { get; set; }

        public bool IsReleased => State == AudioHandleState.Released || _jobReference == null;
        public AudioFadeController Fades { get; } = new();

        private void Awake() {
            _transform = transform;
            source = GetComponent<AudioSource>();
        }


        private void Update() {
            if (State == AudioHandleState.Released) return;
            if (_jobReference is { IsValid: false }) {
                Kill();
                return;
            }

            if (tracked) _transform.position = tracked.position;
            switch (State) {
                case AudioHandleState.Ended:
                case AudioHandleState.Idle:
                    break;
                case AudioHandleState.Starting:

                    if (source.time > 0 && source.isPlaying) State = AudioHandleState.Playing;

                    break;
                case AudioHandleState.Playing:
                    if (!source.isPlaying) {
                        EndNow();
                        break;
                    }

                    Fades.Tick(source.time, source.clip ? source.clip.length : 0f, source);

                    break;
            }
        }


        private void OnDestroy() {
            if (!IsReleased) AudioManager.Instance.Release(this);
        }

        public void ApplyChanges(AudioJobReference reference) {
            _targetVolume = source.volume;
            Fades.Reset(_targetVolume);
            _jobReference = reference;
            _endedSource = new UniTaskCompletionSource();
            State = AudioHandleState.Idle;
        }

        public void ReleaseCallback() {
            _jobReference = null;
            State = AudioHandleState.Released;
            Fades.Reset(1f);
            onEnd = null;
            onContinuation = null;
            source.clip = null;
            source.spatialize = false;
            source.spatialBlend = 0f;
            tracked = null;
            _hasCalledContinuation = false;
            _endedSource?.TrySetResult();
            _endedSource = null;
            KeepAlive = false;
        }


        public bool IsOwnedBy(AudioJobReference reference) {
            return _jobReference != null && _jobReference.Equals(reference);
        }


        public async UniTask WaitUntilEnded() {
            if (HasEnded) return;
            if (_endedSource != null) await _endedSource.Task;
        }

        public UniTask.Awaiter GetAwaiter() {
            return WaitUntilEnded().GetAwaiter();
        }

        public void EndNow() {
            if (HasEnded) return;

            source.Stop();
            State = AudioHandleState.Ended;

            onEnd?.Invoke();
            _endedSource?.TrySetResult();
            CallContinuation();
            if (KeepAlive)
                State = AudioHandleState.Idle;
            else if (!IsReleased) AudioManager.Instance.Release(this);
        }

        public void Kill() {
            if (!IsReleased) AudioManager.Instance.Release(this);
        }

        private void CallContinuation() {
            if (_hasCalledContinuation) return;
            _hasCalledContinuation = true;
            onContinuation?.Invoke();
        }


        public void Play(Vector3 position) {
            _transform.position = position;
            tracked = null;
            PrepareStart();
        }

        public void PlayTracked(Transform tracked) {
            this.tracked = tracked;
            _transform.position = tracked.position;
            PrepareStart();
        }

        private void PrepareStart() {
            _hasCalledContinuation = false;
            State = AudioHandleState.Starting;
            Fades.Reset(_targetVolume);
            source.Play();
        }

    }
}