using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Spookline.SPC.Audio {
    [Serializable]
    public class RangeAudioSourceProvider : IAudioSourceProvider {

        public List<AudioClip> clips = new();


        // ReSharper disable Unity.PerformanceAnalysis
        public AudioJob CreateJob(AudioDefinition definition, CancellationToken cancellationToken = default) {
            switch (clips.Count) {
                case 1:
                    return new AudioJob(definition, 0, definition.options, cancellationToken);
                case > 1: {
                    var randomIndex = Random.Range(0, clips.Count);
                    return new AudioJob(definition, randomIndex, definition.options, cancellationToken);
                }
                default:
                    Debug.LogError("No audio clips available in RangeAudioSourceProvider.");
                    return default;
            }
        }

        public AudioClip GetClip(AudioJob job) {
            return clips[job.data];
        }

        public void Apply(AudioHandle handle, AudioJob job) {
            handle.source.clip = clips[job.data];
            job.options.ApplyTo(handle.source);
        }

    }
}