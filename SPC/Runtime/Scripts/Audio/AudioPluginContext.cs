using UnityEngine.Audio;

namespace Spookline.SPC.Audio {
    /// <summary>Stable runtime services exposed to optional audio integrations.</summary>
    public sealed class AudioPluginContext {
        internal AudioPluginContext(AudioManager manager, AudioMixer mixer) {
            Manager = manager;
            Mixer = mixer;
        }

        public AudioManager Manager { get; }
        public AudioMixer Mixer { get; }
    }
}