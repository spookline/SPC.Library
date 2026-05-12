using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Spookline.SPC.Ext;
using UnityEngine;
using UnityEngine.Audio;

namespace Spookline.SPC.Audio {
    [CreateAssetMenu(fileName = "SpookAudioModule", menuName = "Modules/SpookAudioModule")]
    public class SpookAudioModule : OdinModule<SpookAudioModule> {

        public AudioMixer mixer;
        public int audioCacheSize = 10;
        public bool ready;

        [TypeDrawerSettings(BaseType = typeof(Enum), Filter = TypeInclusionFilter.IncludeConcreteTypes)]
        public Type lookupEnum;

        public AnimationCurve curve;
        public static UniTask Ready => UniTask.WaitUntil(() => HasInstance && Instance.ready);

        public override void Load() {
            base.Load();
            ready = false;
            On<GlobalStartEvt>().ChainDo(OnGlobalStart, -100);
        }

        public void ClearAudioPool() {
            AudioManager.Instance.ClearAudioPool();
        }

        private async UniTask OnGlobalStart(GlobalStartEvt arg) {
            await SpookAudioRegistry.Instance.Load(lookupEnum);
            AudioManager.Instance.Initialize(audioCacheSize);
            ready = true;
        }

        public AudioJob CreateJob(AudioDefinition definition, CancellationToken cancellationToken = default) {
            return definition.provider.CreateJob(definition, cancellationToken);
        }

        private async UniTask Prepare(AudioDefinition definition, CancellationToken cancellationToken = default) {
            if (!definition.provider.IsLoaded) await definition.provider.Load().AttachExternalCancellation(cancellationToken);
            else await UniTask.Yield();
        }

        private async UniTask Lease(AudioJobReference reference, CancellationToken cancellationToken = default) {
            var job = reference.job;
            await Prepare(job.definition, cancellationToken);
            if (!reference.IsPending) return;
            var handle = AudioManager.Instance.Lease();
            handle.source.outputAudioMixerGroup = job.definition.group;
            job.definition.provider.Apply(handle, job);
            reference.handle = handle;
            handle.ApplyChanges(reference);
        }

        public async UniTask Unstarted(AudioJobReference reference, CancellationToken cancellationToken = default) {
            await Lease(reference, cancellationToken);
            if (!reference.IsPending) return;
            reference.state = AudioJobReferenceState.Ready;
            reference.RegisterToken(cancellationToken);
        }

        private async UniTask PlayInternal(AudioJobReference reference, Action<AudioHandle> playAction,
            float spatialBlend, CancellationToken cancellationToken = default) {
            await Lease(reference, cancellationToken);
            if (!reference.IsPending) return;
            var handle = reference.handle;
            handle.source.spatialBlend = spatialBlend;
            reference.state = AudioJobReferenceState.Ready;
            playAction(handle);
            reference.RegisterToken(cancellationToken);
        }

        public async UniTask Play(AudioJobReference reference, CancellationToken cancellationToken = default) {
            await PlayInternal(reference, h => h.Play(Vector3.zero), 0f, cancellationToken);
        }

        public async UniTask Play(AudioJobReference reference, Vector3 position,
            CancellationToken cancellationToken = default) {
            await PlayInternal(reference, h => h.Play(position), 1f, cancellationToken);
        }

        public async UniTask Play(AudioJobReference reference, Transform transform,
            CancellationToken cancellationToken = default) {
            await PlayInternal(reference, h => h.PlayTracked(transform), 1f, cancellationToken);
        }

        public async UniTask<AudioJobReference> PlayAndAwait(AudioJobReference reference,
            CancellationToken cancellationToken = default) {
            await Play(reference, cancellationToken);
            await reference.WaitUntilEnded(cancellationToken);
            return reference;
        }

        public async UniTask<AudioJobReference> PlayAndAwait(AudioJobReference reference, Vector3 position,
            CancellationToken cancellationToken = default) {
            await Play(reference, position, cancellationToken);
            await reference.WaitUntilEnded(cancellationToken);
            return reference;
        }

        public async UniTask<AudioJobReference> PlayAndAwait(AudioJobReference reference, Transform transform,
            CancellationToken cancellationToken = default) {
            await Play(reference, transform, cancellationToken);
            await reference.WaitUntilEnded(cancellationToken);
            return reference;
        }

        public async UniTask<AudioClip> GetClip(AudioJob job, CancellationToken cancellationToken = default) {
            await Prepare(job.definition, cancellationToken);
            return job.definition.provider.GetClip(job);
        }

    }
}