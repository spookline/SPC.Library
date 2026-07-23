using System.Threading;
using UnityEngine;

namespace Spookline.SPC.Audio {
    public interface IAudioSourceProvider {

        public AudioJob CreateJob(AudioDefinition definition, CancellationToken cancellationToken = default);
        public AudioClip GetClip(AudioJob job);
        public void Apply(AudioHandle handle, AudioJob job);

    }
}