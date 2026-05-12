using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Spookline.SPC.Audio {
    [Serializable]
    public struct AudioAssetEntry {

        [HideLabel]
        public AssetReferenceT<AudioClip> clip;

    }
}