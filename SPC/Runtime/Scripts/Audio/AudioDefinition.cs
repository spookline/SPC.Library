using Sirenix.OdinInspector;
using Spookline.SPC.Ext;
using UnityEngine;
using UnityEngine.Audio;

namespace Spookline.SPC.Audio {
    [CreateAssetMenu(fileName = "AudioDefinition", menuName = "Spookline/AudioDefinition", order = 1)]
    public class AudioDefinition : RegistryObject {

        public AudioMixerGroup group;

        [BoxGroup("Default Audio Options")]
        [HideLabel]
        public AudioOptions options = AudioOptions.Default;

        [Title("Provider")]
        [PolymorphicDrawerSettings(ShowBaseType = false)]
        [HideLabel]
        [InlineProperty]
        public IAudioSourceProvider provider;

    }
}