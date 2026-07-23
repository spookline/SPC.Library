using System;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private List<AudioGlobalPlugin> globalPlugins = new();
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
            await InitializePlugins();
            AudioManager.Instance.SetGlobalPlugins(globalPlugins);
            ready = true;
        }

        public async UniTask ShutdownPlugins() {
            if (!HasInstance) return;
            var context = new AudioPluginContext(AudioManager.Instance, mixer);
            for (var i = globalPlugins.Count - 1; i >= 0; i--) {
                var plugin = globalPlugins[i];
                if (plugin != null) await plugin.Shutdown(context);
            }
            AudioManager.Instance.SetGlobalPlugins(null);
            ready = false;
        }

        private async UniTask InitializePlugins() {
            var context = new AudioPluginContext(AudioManager.Instance, mixer);
            foreach (var plugin in globalPlugins.Where(plugin => plugin != null).OrderBy(plugin => plugin.order))
                await plugin.Initialize(context);
        }



        public AudioJob CreateJob(AudioDefinition definition, CancellationToken cancellationToken = default) {
            return definition.provider.CreateJob(definition, cancellationToken);
        }

        private async UniTask Lease(AudioJobReference reference, CancellationToken cancellationToken = default) {
            var job = reference.job;
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

        public AudioClip GetClip(AudioJob job) {
            return job.definition.provider.GetClip(job);
        }

    }
}