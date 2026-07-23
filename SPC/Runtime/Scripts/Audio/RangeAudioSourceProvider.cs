using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Spookline.SPC.Audio {
    /// <summary>Selects a random non-null clip from a definition.</summary>
    [Serializable]
    public sealed class RangeAudioSourceProvider : IAudioSourceProvider {

        public List<AudioClip> clips = new();
        public bool avoidConsecutiveClipRepeats;

        [NonSerialized]
        private int _lastClipIndex = -1;

        public int ClipCount => clips?.Count ?? 0;

        public bool IsValid {
            get {
                if (clips == null) return false;
                foreach (var clip in clips)
                    if (clip) return true;
                return false;
            }
        }

        public AudioJob CreateJob(AudioDefinition definition, CancellationToken cancellationToken = default) {
            if (!definition) throw new ArgumentNullException(nameof(definition));

            var selectedIndex = SelectClipIndex();
            if (selectedIndex < 0) {
                Debug.LogError($"Audio definition '{definition.name}' has no valid clips.");
                return default;
            }

            _lastClipIndex = selectedIndex;
            return new AudioJob(definition, selectedIndex, definition.options, cancellationToken);
        }

        public AudioClip GetClip(AudioJob job) {
            if (clips == null || job.data < 0 || job.data >= clips.Count) return null;
            return clips[job.data];
        }

        public void Apply(AudioHandle handle, AudioJob job) {
            if (!handle) return;
            handle.source.clip = GetClip(job);
            job.options.ApplyTo(handle.source);
        }

        private int SelectClipIndex() {
            if (clips == null) return -1;

            var selected = -1;
            var candidates = 0;

            // Reservoir sampling avoids a temporary list and remains uniform when entries are null.
            for (var i = 0; i < clips.Count; i++) {
                if (!clips[i]) continue;
                if (avoidConsecutiveClipRepeats && i == _lastClipIndex) continue;
                candidates++;
                if (Random.Range(0, candidates) == 0) selected = i;
            }

            if (selected >= 0) return selected;

            // A single valid clip is still playable when repeat avoidance is enabled.
            if (_lastClipIndex >= 0 && _lastClipIndex < clips.Count && clips[_lastClipIndex])
                return _lastClipIndex;

            return -1;
        }

    }
}
