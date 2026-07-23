using System;
using System.Threading;
using UnityEngine.AddressableAssets;

namespace Spookline.SPC.Audio {
    public static class SpookAudioRegistryExtensions {

        public static AudioDefinition FromRegistry(this AssetReferenceT<AudioDefinition> reference) {
            if (reference == null) throw new ArgumentNullException(nameof(reference));
            return SpookAudioRegistry.Instance.GetByGuid(reference.AssetGUID);
        }

        public static AudioJob Job(
            this AudioDefinition definition,
            CancellationToken cancellationToken = default
        ) {
            return SpookAudioModule.Instance.CreateJob(definition, cancellationToken);
        }

    }
}
