using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Spookline.SPC.Audio {
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "ParameterHidesMember")]
    public struct AudioOptions {

        public static readonly AudioOptions Default = new() {
            volume = 1f,
            pitch = 1f,
            minDistance = 1f,
            maxDistance = 15f
        };

        public float volume;
        public float pitch;
        public float minDistance;
        public float maxDistance;

        public AudioOptions Volume(float volume) {
            this.volume = volume;
            return this;
        }

        public AudioOptions Pitch(float pitch) {
            this.pitch = pitch;
            return this;
        }

        public AudioOptions MinDistance(float minDistance) {
            this.minDistance = minDistance;
            return this;
        }

        public AudioOptions MaxDistance(float maxDistance) {
            this.maxDistance = maxDistance;
            return this;
        }

        public readonly void ApplyTo(AudioSource source) {
            source.volume = volume;
            source.pitch = pitch;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
        }

    }
}