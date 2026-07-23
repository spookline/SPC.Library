using System.Threading;
using UnityEngine;

namespace Spookline.SPC.Audio {
    /// <summary>
    /// Polymorphic clip selection and source-configuration strategy.
    /// Implementations are serialized inside an <see cref="AudioDefinition"/>.
    /// </summary>
    public interface IAudioSourceProvider {

        bool IsValid { get; }

        AudioJob CreateJob(AudioDefinition definition, CancellationToken cancellationToken = default);

        AudioClip GetClip(AudioJob job);

        void Apply(AudioHandle handle, AudioJob job);

    }
}
