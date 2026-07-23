using UnityEngine;
using UnityEngine.Audio;

namespace Spookline.SPC.Audio {
    /// <summary>Stable runtime services exposed to optional audio integrations.</summary>
    public sealed class AudioPluginContext {

        internal AudioPluginContext(AudioManager manager, SpookAudioModule module, AudioMixer mixer) {
            Manager = manager;
            Module = module;
            Mixer = mixer;
        }

        public AudioManager Manager { get; }
        public SpookAudioModule Module { get; }
        public AudioMixer Mixer { get; }
        public Transform PoolRoot => Manager.PoolRoot;

    }
}
