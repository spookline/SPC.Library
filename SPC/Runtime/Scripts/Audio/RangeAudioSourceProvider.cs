using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Spookline.SPC.Audio {
    [Serializable]
    public class RangeAudioSourceProvider : IAudioSourceProvider {

        public List<AudioClip> clips = new();
        public bool avoidConsecutiveClipRepeats;

        private int _lastClipIndex = -1;

        public int ClipCount => clips.Count;


        // ReSharper disable Unity.PerformanceAnalysis
        public AudioJob CreateJob(AudioDefinition definition, CancellationToken cancellationToken = default) {
            var clipCount = ClipCount;
            switch (clipCount) {
                case 1:
                    for (var i = 0; i < clips.Count; i++)
                        if (clips[i]) return CreateJob(definition, i, cancellationToken);
                    break;
                case > 1: {
                    var selected = Random.Range(0, avoidConsecutiveClipRepeats ? clipCount - 1 : clipCount);
                    var lastClipIndex = _lastClipIndex;
                    for (var i = 0; i < clips.Count; i++) {
                        if (!clips[i]) continue;
                        if (avoidConsecutiveClipRepeats && i == lastClipIndex) continue;
                        if (selected-- == 0)
                            return CreateJob(definition, i, cancellationToken);
                    }
                    break;
                }
                default:
                    Debug.LogError("No audio clips available in RangeAudioSourceProvider.");
                    return default;
            }

            return default;
        }

        private AudioJob CreateJob(AudioDefinition definition, int clipIndex, CancellationToken cancellationToken) {
            _lastClipIndex = clipIndex;
            return new AudioJob(definition, clipIndex, definition.options, cancellationToken);
        }

        public AudioClip GetClip(AudioJob job) {
            if (clips == null || job.data < 0 || job.data >= clips.Count) return null;
            return clips[job.data];
        }

        public void Apply(AudioHandle handle, AudioJob job) {
            handle.source.clip = GetClip(job);
            job.options.ApplyTo(handle.source);
        }

    }
}