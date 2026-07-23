using UnityEngine;

namespace Spookline.SPC.Audio {
    public enum AudioFadeCurve {
        Linear,
        SmoothStep,
        Logarithmic
    }

    public readonly struct AudioFade {
        public readonly float duration;
        public readonly AudioFadeCurve curve;

        public AudioFade(float duration, AudioFadeCurve curve = AudioFadeCurve.SmoothStep) {
            this.duration = Mathf.Max(0f, duration);
            this.curve = curve;
        }

        public float Evaluate(float elapsed) {
            if (duration <= 0f) return 1f;
            var t = Mathf.Clamp01(elapsed / duration);
            return curve switch {
                AudioFadeCurve.Linear => t,
                AudioFadeCurve.Logarithmic => Mathf.Log10(1f + 9f * t),
                _ => t * t * (3f - 2f * t)
            };
        }
    }
}