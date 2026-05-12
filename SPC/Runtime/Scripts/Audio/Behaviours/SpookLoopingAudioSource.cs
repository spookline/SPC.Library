using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Spookline.SPC.Ext;
using UnityEngine.AddressableAssets;

namespace Spookline.SPC.Audio.Behaviours {
    public class SpookLoopingAudioSource : SpookBehaviour<SpookLoopingAudioSource> {

        private LoopingAudioJob _loopingAudioJob;

        public AssetReferenceT<AudioDefinition> sound;
        public bool spacialize = true;
        public float crossfadeDuration = 0.25f;

        [HideLabel]
        public AudioOptionOverride overrides;

        protected override void OnEnable() {
            base.OnEnable();
            TryStart().Forget();
        }

        protected override void OnDisable() {
            base.OnDisable();
            _loopingAudioJob?.Dispose();
            _loopingAudioJob = null;
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            _loopingAudioJob?.Dispose();
        }

        private async UniTask TryStart() {
            await SpookAudioModule.Ready;
            _loopingAudioJob?.Dispose();
            var audioDef = SpookAudioRegistry.Instance.GetByGuid(sound.AssetGUID);
            var job = audioDef.Job();
            if (overrides.hasOverride) job = job.WithOptions(overrides.options);
            _loopingAudioJob = new LoopingAudioJob(job, spacialize ? transform : null, crossfadeDuration);
            await _loopingAudioJob.Setup(true);
        }

    }
}