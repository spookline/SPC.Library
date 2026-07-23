using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Spookline.SPC.Audio {
    /// <summary>
    ///     Optional integration point for systems that need to observe or configure the audio runtime.
    ///     Implementations are assets, so integrations can be kept in a separate package without a hard
    ///     reference from the audio system (for example, Steam Audio).
    /// </summary>
    public abstract class AudioGlobalPlugin : ScriptableObject {
        [Tooltip("Lower values are initialized first.")]
        public int order;

        public virtual UniTask Initialize(AudioPluginContext context) => UniTask.CompletedTask;

        public virtual UniTask Shutdown(AudioPluginContext context) => UniTask.CompletedTask;

        public virtual void OnHandleCreated(AudioHandle handle) { }

        public virtual void OnHandleReleased(AudioHandle handle) { }
    }
}