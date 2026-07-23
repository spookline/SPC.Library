using UnityEngine;

namespace Spookline.SPC.Audio {
    /// <summary>Owns volume automation for a single reusable audio source.</summary>
    public sealed class AudioFadeController {
        private AudioFade _fadeIn;
        private AudioFade _fadeOut;
        private float _targetVolume;

        public void Reset(float targetVolume) {
            _fadeIn = default;
            _fadeOut = default;
            _targetVolume = targetVolume;
        }

        public void FadeIn(float duration, AudioFadeCurve curve = AudioFadeCurve.SmoothStep) {
            _fadeIn = new AudioFade(duration, curve);
        }

        public void FadeOut(float duration, AudioFadeCurve curve = AudioFadeCurve.SmoothStep) {
            _fadeOut = new AudioFade(duration, curve);
        }

        public bool Tick(float time, float clipLength, AudioSource source) {
            if (!source || !source.isPlaying) return false;
            var volume = _targetVolume;
            var controlled = false;
            if (_fadeIn.duration > 0f) {
                volume *= _fadeIn.Evaluate(time);
                controlled = true;
            }

            if (_fadeOut.duration > 0f && clipLength > 0f) {
                var elapsed = time - Mathf.Max(0f, clipLength - _fadeOut.duration);
                if (elapsed >= 0f) {
                    volume *= 1f - _fadeOut.Evaluate(elapsed);
                    controlled = true;
                }
            }

            if (controlled) source.volume = volume;
            return controlled;
        }
    }
}