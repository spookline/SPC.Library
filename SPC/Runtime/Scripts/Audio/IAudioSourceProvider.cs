using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Spookline.SPC.Audio {
    public interface IAudioSourceProvider {

        public bool IsLoaded { get; }
        public UniTask Load();
        public UniTask Unload();
        public AudioJob CreateJob(AudioDefinition definition, CancellationToken cancellationToken = default);
        public AudioClip GetClip(AudioJob job);
        public void Apply(AudioHandle handle, AudioJob job);

    }
}