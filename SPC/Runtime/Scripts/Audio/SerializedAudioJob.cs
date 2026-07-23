using System;
using System.Threading;

namespace Spookline.SPC.Audio {
    /// <summary>Portable description of a selected job, suitable for save data or networking.</summary>
    [Serializable]
    public sealed class SerializedAudioJob {

        public string guid;
        public int data;
        public AudioOptions options;

        public bool TryToAudioJob(out AudioJob job, CancellationToken cancellationToken = default) {
            job = default;
            if (string.IsNullOrWhiteSpace(guid)) return false;
            if (!SpookAudioRegistry.Instance.TryGetByGuid(guid, out var definition) || !definition) return false;
            job = new AudioJob(definition, data, options, cancellationToken);
            return true;
        }

        public AudioJob ToAudioJob(CancellationToken cancellationToken = default) {
            if (TryToAudioJob(out var job, cancellationToken)) return job;
            throw new InvalidOperationException($"Audio definition '{guid}' is not loaded.");
        }

    }
}
