using UnityEngine;

namespace Spookline.SPC.Audio {
    /// <summary>Owns volume automation for one reusable AudioSource.</summary>
    public sealed class AudioFadeController {

        private AudioFade _fadeIn;
        private AudioFade _fadeOut;
        private float _targetVolume;

        public bool HasFadeOut => _fadeOut.duration > 0f;

        public void Reset(float targetVolume) {
            _fadeIn = default;
            _fadeOut = default;
            _targetVolume = Mathf.Clamp01(targetVolume);
        }

        public void SetTargetVolume(float targetVolume) {
            _targetVolume = Mathf.Clamp01(targetVolume);
        }

        public void FadeIn(float duration, AudioFadeCurve curve = AudioFadeCurve.SmoothStep) {
            _fadeIn = new AudioFade(duration, curve);
        }

        public void FadeOut(float duration, AudioFadeCurve curve = AudioFadeCurve.SmoothStep) {
            _fadeOut = new AudioFade(duration, curve);
        }

        public bool IsFadeOutActive(float time, float clipLength) {
            return _fadeOut.duration > 0f && time >= Mathf.Max(0f, clipLength - _fadeOut.duration);
        }

        public void Tick(float time, float clipLength, AudioSource source) {
            if (!source || !source.isPlaying) return;

            var volume = _targetVolume;
            if (_fadeIn.duration > 0f) volume *= _fadeIn.Evaluate(time);

            if (IsFadeOutActive(time, clipLength)) {
                var elapsed = time - Mathf.Max(0f, clipLength - _fadeOut.duration);
                volume *= 1f - _fadeOut.Evaluate(elapsed);
            }

            source.volume = Mathf.Clamp01(volume);
        }

    }
}
