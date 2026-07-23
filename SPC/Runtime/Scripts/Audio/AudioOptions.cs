using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Spookline.SPC.Audio {
    /// <summary>
    /// Serializable Unity AudioSource settings used by an <see cref="AudioJob"/>.
    /// Keeping these values on the job makes per-play overrides allocation-free.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "ParameterHidesMember")]
    public struct AudioOptions {

        public static readonly AudioOptions Default = new() {
            volume = 1f,
            pitch = 1f,
            spatialBlend = 1f,
            dopplerLevel = 1f,
            reverbZoneMix = 1f,
            minDistance = 1f,
            maxDistance = 15f,
            rolloffMode = AudioRolloffMode.Logarithmic
        };

        [Header("Playback")]
        [Range(0f, 1f)]
        public float volume;

        [Range(-3f, 3f)]
        public float pitch;

        [Header("Spatial")]
        [Range(0f, 1f)]
        public float spatialBlend;

        public bool spatialize;
        public bool spatializePostEffects;

        [Range(0f, 5f)]
        public float dopplerLevel;

        [Range(0f, 360f)]
        public float spread;

        [Range(0f, 1.1f)]
        public float reverbZoneMix;

        [Min(0f)]
        public float minDistance;

        [Min(0f)]
        public float maxDistance;

        public AudioRolloffMode rolloffMode;

        [Tooltip("Used only when Rolloff Mode is Custom. X is distance and Y is volume.")]
        public AnimationCurve customRolloffCurve;

        public AudioOptions Volume(float value) {
            volume = value;
            return this;
        }

        public AudioOptions Pitch(float value) {
            pitch = value;
            return this;
        }

        public AudioOptions SpatialBlend(float value) {
            spatialBlend = value;
            return this;
        }

        public AudioOptions Spatialize(bool value = true) {
            spatialize = value;
            return this;
        }

        public AudioOptions MinDistance(float value) {
            minDistance = value;
            return this;
        }

        public AudioOptions MaxDistance(float value) {
            maxDistance = value;
            return this;
        }

        public AudioOptions Rolloff(AudioRolloffMode mode, AnimationCurve customCurve = null) {
            rolloffMode = mode;
            customRolloffCurve = customCurve;
            return this;
        }

        public readonly void ApplyTo(AudioSource source) {
            if (!source) return;

            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, -3f, 3f);
            source.spatialBlend = Mathf.Clamp01(spatialBlend);
            source.spatialize = spatialize;
            source.spatializePostEffects = spatializePostEffects;
            source.dopplerLevel = Mathf.Clamp(dopplerLevel, 0f, 5f);
            source.spread = Mathf.Clamp(spread, 0f, 360f);
            source.reverbZoneMix = Mathf.Clamp(reverbZoneMix, 0f, 1.1f);
            source.minDistance = Mathf.Max(0f, minDistance);
            source.maxDistance = Mathf.Max(source.minDistance, maxDistance);
            source.rolloffMode = rolloffMode;

            if (rolloffMode == AudioRolloffMode.Custom && customRolloffCurve != null)
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, customRolloffCurve);
        }

    }
}
