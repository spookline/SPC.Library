using System.Threading;
using UnityEngine.AddressableAssets;

namespace Spookline.SPC.Audio {
    public static class SpookAudioRegistryExtensions {

        public static AudioDefinition FromRegistry(this AssetReferenceT<AudioDefinition> reference) {
            return SpookAudioRegistry.Instance.GetByGuid(reference.AssetGUID);
        }


        public static AudioJob Job(this AudioDefinition def, CancellationToken cancellationToken = default) {
            return SpookAudioModule.Instance.CreateJob(def, cancellationToken);
        }

    }
}