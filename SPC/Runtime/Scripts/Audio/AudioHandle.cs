using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Spookline.SPC.Audio {
    public class AudioHandle : MonoBehaviour {

        [HideInInspector]
        public AudioSource source;

        [HideInInspector]
        public Transform tracked;
        private bool _fadeIn;
        private float _fadeInEnd;

        private float _fadeInStart;
        private bool _fadeOut;
        private float _fadeOutEnd;

        private float _fadeOutStart;
        private bool _hasCalledContinuation;

        private AudioJobReference _jobReference;
        private bool _startedFadeIn;
        private bool _startedFadeOut;

        private float _targetVolume;
        private Transform _transform;
        public Action onContinuation;

        public Action onEnd;

        public AudioHandleState State { get; private set; }

        public bool IsPlaying => source.isPlaying;
        public bool HasEnded => State is AudioHandleState.Ended or AudioHandleState.Released;
        public bool KeepAlive { get; set; }

        public bool IsReleased => State == AudioHandleState.Released || _jobReference == null;

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

                    if (_fadeIn) source.volume = 0;

                    break;
                case AudioHandleState.Playing:
                    if (!source.isPlaying) {
                        EndNow();
                        break;
                    }

                    var time = source.time;
                    float controlVolume = -1;
                    if (_fadeIn) {
                        if (time >= _fadeInStart && time <= _fadeInEnd) {
                            if (!_startedFadeIn) {
                                _startedFadeIn = true;
                                controlVolume = 0;
                            } else {
                                var t = (time - _fadeInStart) / (_fadeInEnd - _fadeInStart);
                                controlVolume = _targetVolume * t;
                            }
                        } else if (time >= _fadeInStart && time < _fadeInEnd) {
                            controlVolume = _targetVolume;
                            _fadeIn = false;
                            _startedFadeIn = false;
                        }
                    }

                    if (_fadeOut) {
                        if (time >= _fadeOutStart && time <= _fadeOutEnd) {
                            if (!_startedFadeOut) {
                                _startedFadeOut = true;
                                controlVolume = _targetVolume;
                                CallContinuation();
                            } else {
                                var t = (time - _fadeOutStart) / (_fadeOutEnd - _fadeOutStart);
                                controlVolume = _targetVolume * (1f - t);
                            }
                        } else if (time >= _fadeOutStart && time < _fadeOutEnd) {
                            controlVolume = 0f;
                            EndNow();
                        }
                    }

                    if (controlVolume >= 0) {
                        var curvedVolume = Mathf.Log10(controlVolume * 100f + 1f) / 2f;
                        source.volume = curvedVolume;
                    }

                    break;
            }
        }


        private void OnDestroy() {
            if (!IsReleased) AudioManager.Instance.Release(this);
        }

        public void ApplyChanges(AudioJobReference reference) {
            _targetVolume = source.volume;
            _jobReference = reference;
            State = AudioHandleState.Idle;
        }

        public void ReleaseCallback() {
            _jobReference = null;
            State = AudioHandleState.Released;
            _fadeOut = false;
            _fadeIn = false;
            _startedFadeIn = false;
            _startedFadeOut = false;
            onEnd = null;
            onContinuation = null;
            source.clip = null;
            source.spatialize = false;
            source.spatialBlend = 0f;
            tracked = null;
            _hasCalledContinuation = false;
            KeepAlive = false;
        }


        public bool IsOwnedBy(AudioJobReference reference) {
            return _jobReference != null && _jobReference.Equals(reference);
        }

        public void ImmediateFadeOut(float duration) {
            var time = source.time;
            var endTime = Mathf.Min(time + duration, source.clip.length);
            _fadeOutStart = time;
            _fadeOutEnd = endTime;
            _fadeOut = true;
        }

        public void SetFadeIn(float duration) {
            _fadeInStart = 0f;
            _fadeInEnd = duration;
            _fadeIn = true;
        }

        public void SetFadeOut(float duration) {
            if (!source) {
                _fadeOut = false;
                return;
            }
            _fadeOutStart = source.clip.length - duration;
            _fadeOutEnd = source.clip.length;
            _fadeOut = true;
        }

        public async UniTask WaitUntilEnded() {
            if (HasEnded) return;
            var tcs = new UniTaskCompletionSource();
            onEnd += () => tcs.TrySetResult();
            await tcs.Task;
        }

        public void EndNow() {
            if (HasEnded) return;

            source.Stop();
            State = AudioHandleState.Ended;
            _startedFadeIn = false;
            _startedFadeOut = false;

            onEnd?.Invoke();
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
            _startedFadeIn = false;
            _startedFadeOut = false;
            _hasCalledContinuation = false;
            State = AudioHandleState.Starting;
            if (_fadeIn) source.volume = 0f;
            source.Play();
        }

    }
}