using Sirenix.OdinInspector;
using Spookline.SPC.Ext;
using UnityEngine;
using UnityEngine.Audio;

namespace Spookline.SPC.Audio {
    /// <summary>
    /// Describes a playable sound independently from the system that supplies its clips.
    /// Audio definitions are discovered by <see cref="SpookAudioModule"/> through Addressables.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioDefinition", menuName = "Spookline/Audio Definition", order = 1)]
    public class AudioDefinition : RegistryObject {

        [Tooltip("Optional mixer group used by every voice created from this definition.")]
        public AudioMixerGroup group;

        [BoxGroup("Default Audio Options")]
        [HideLabel]
        public AudioOptions options = AudioOptions.Default;

        [Title("Clip Provider")]
        [Tooltip("A polymorphic clip-selection strategy. Custom providers can live in separate assemblies.")]
        [PolymorphicDrawerSettings(ShowBaseType = false)]
        [HideLabel]
        [InlineProperty]
        public IAudioSourceProvider provider = new RangeAudioSourceProvider();

        public bool IsPlayable => provider != null && provider.IsValid;

    }
}
