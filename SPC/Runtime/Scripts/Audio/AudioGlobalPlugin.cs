using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Spookline.SPC.Audio {
    /// <summary>
    /// Optional integration point for external spatial audio systems such as Steam Audio.
    /// Implementations live in separate packages, so SPC never needs a hard plugin dependency.
    /// </summary>
    public abstract class AudioGlobalPlugin : ScriptableObject {

        [Tooltip("Lower values initialize first and shut down last.")]
        public int order;

        public virtual UniTask Initialize(AudioPluginContext context) => UniTask.CompletedTask;

        public virtual UniTask Shutdown(AudioPluginContext context) => UniTask.CompletedTask;

        /// <summary>Called once when a new pooled GameObject is constructed.</summary>
        public virtual void OnHandleCreated(AudioHandle handle) { }

        /// <summary>Called whenever a pooled voice is assigned to a job.</summary>
        public virtual void OnHandleLeased(AudioHandle handle, AudioJob job) { }

        /// <summary>Called immediately before AudioSource.Play.</summary>
        public virtual void OnBeforePlay(AudioHandle handle, AudioJob job) { }

        /// <summary>Called before the voice is reset and returned to the pool.</summary>
        public virtual void OnHandleReleased(AudioHandle handle) { }

        /// <summary>Called once before a pooled GameObject is destroyed.</summary>
        public virtual void OnHandleDestroyed(AudioHandle handle) { }

    }
}
