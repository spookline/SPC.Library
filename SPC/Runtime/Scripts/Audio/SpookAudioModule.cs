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
    /// <summary>
    /// Loads Addressable audio definitions, owns the AudioSource pool, and coordinates integrations.
    /// </summary>
    [CreateAssetMenu(fileName = "SpookAudioModule", menuName = "Modules/SpookAudioModule")]
    public sealed class SpookAudioModule : OdinModule<SpookAudioModule> {

        public AudioMixer mixer;

        [Tooltip("Addressables label applied to AudioDefinition assets.")]
        public string addressableLabel = SpookAudioRegistry.DefaultAddressableLabel;

        [Min(0)]
        [Tooltip("Number of AudioSources prepared when the module starts.")]
        public int audioCacheSize = 10;

        [Min(1)]
        [Tooltip("Maximum number of inactive AudioSources retained by the pool.")]
        public int maxAudioSources = 64;

        [TypeDrawerSettings(BaseType = typeof(Enum), Filter = TypeInclusionFilter.IncludeConcreteTypes)]
        [Tooltip("Optional enum whose member names map to AudioDefinition asset names.")]
        public Type lookupEnum;

        [SerializeField]
        private List<AudioGlobalPlugin> globalPlugins = new();

        [ShowInInspector, ReadOnly]
        public bool ready;

        // Retained for compatibility with existing module assets. Individual fades select their own curve.
        [HideInInspector]
        public AnimationCurve curve;

        private readonly List<AudioGlobalPlugin> _initializedPlugins = new();
        private CancellationTokenSource _lifetimeSource;
        private bool _registryLoaded;
        private bool _shuttingDown;

        public static UniTask Ready => UniTask.WaitUntil(() => HasInstance && Instance.ready);
        public IReadOnlyList<AudioDefinition> Definitions => SpookAudioRegistry.Instance.Objects;
        public IReadOnlyList<AudioGlobalPlugin> Plugins => globalPlugins;

        public override void Load() {
            base.Load();
            ready = false;
            _shuttingDown = false;
            _lifetimeSource?.Dispose();
            _lifetimeSource = new CancellationTokenSource();
            On<GlobalStartEvt>().ChainDo(OnGlobalStart, -100);
        }

        public override void Unload() {
            ready = false;
            _shuttingDown = true;
            _lifetimeSource?.Cancel();
            ShutdownAsync().Forget();
            base.Unload();
        }

        public AudioJob CreateJob(
            AudioDefinition definition,
            CancellationToken cancellationToken = default
        ) {
            if (!definition) throw new ArgumentNullException(nameof(definition));
            if (definition.provider == null)
                throw new InvalidOperationException($"Audio definition '{definition.name}' has no clip provider.");
            return definition.provider.CreateJob(definition, cancellationToken);
        }

        public void ClearAudioPool() => AudioManager.Instance.ClearAudioPool();

        public AudioClip GetClip(AudioJob job) {
            if (!job.IsValid) return null;
            return job.definition.provider.GetClip(job);
        }

        public async UniTask Unstarted(
            AudioJobReference reference,
            CancellationToken cancellationToken = default
        ) {
            await PrepareReference(reference, cancellationToken);
        }

        public async UniTask Play(
            AudioJobReference reference,
            CancellationToken cancellationToken = default
        ) {
            await PlayInternal(reference, handle => handle.Play(Vector3.zero), false, cancellationToken);
        }

        public async UniTask Play(
            AudioJobReference reference,
            Vector3 position,
            CancellationToken cancellationToken = default
        ) {
            await PlayInternal(reference, handle => handle.Play(position), true, cancellationToken);
        }

        public async UniTask Play(
            AudioJobReference reference,
            Transform target,
            CancellationToken cancellationToken = default
        ) {
            await PlayInternal(reference, handle => handle.PlayTracked(target), true, cancellationToken);
        }

        private async UniTask OnGlobalStart(GlobalStartEvt _) {
            try {
                var token = _lifetimeSource.Token;
                SpookAudioRegistry.Instance.Label = addressableLabel;
                await SpookAudioRegistry.Instance.Load(lookupEnum);
                _registryLoaded = true;
                token.ThrowIfCancellationRequested();

                var manager = AudioManager.Instance;
                manager.Initialize(audioCacheSize, Mathf.Max(audioCacheSize, maxAudioSources));
                await InitializePlugins(manager, token);
                manager.SetGlobalPlugins(_initializedPlugins);
                manager.WarmPool();
                ready = true;
            } catch (OperationCanceledException) {
                // Normal during module teardown.
            } catch (Exception exception) {
                Debug.LogException(exception, this);
                await ShutdownAsync();
            }
        }

        private async UniTask<AudioHandle> PrepareReference(
            AudioJobReference reference,
            CancellationToken cancellationToken
        ) {
            if (reference == null) throw new ArgumentNullException(nameof(reference));
            if (!reference.IsPending) return reference.handle;

            try {
                cancellationToken.ThrowIfCancellationRequested();
                if (!ready) await Ready.AttachExternalCancellation(cancellationToken);
                if (!reference.IsPending) return null;

                var job = reference.job;
                if (!job.IsValid) throw new InvalidOperationException("Cannot play an invalid audio job.");

                var manager = AudioManager.Instance;
                var handle = manager.Lease(job);
                reference.handle = handle;
                handle.Assign(reference);

                handle.source.outputAudioMixerGroup = job.definition.group;
                job.definition.provider.Apply(handle, job);
                if (!handle.source.clip)
                    throw new InvalidOperationException(
                        $"Provider '{job.definition.provider.GetType().Name}' returned no clip for '{job.definition.name}'.");
                handle.CaptureSourceSettings();

                reference.MarkReady(handle, cancellationToken);
                return reference.IsValid ? handle : null;
            } catch (OperationCanceledException) {
                reference.Dispose();
                return null;
            } catch (Exception exception) {
                if (reference.IsValid) AudioManager.Instance.Release(reference.handle);
                reference.MarkFailed();
                Debug.LogException(exception, this);
                return null;
            }
        }

        private async UniTask PlayInternal(
            AudioJobReference reference,
            Action<AudioHandle> playAction,
            bool spatial,
            CancellationToken cancellationToken
        ) {
            var handle = await PrepareReference(reference, cancellationToken);
            if (!handle || !reference.IsValid) return;

            handle.source.spatialBlend = spatial ? Mathf.Clamp01(reference.job.options.spatialBlend) : 0f;
            AudioManager.Instance.BeforePlay(handle, reference.job);
            if (reference.IsValid) playAction(handle);
        }

        private async UniTask InitializePlugins(AudioManager manager, CancellationToken token) {
            _initializedPlugins.Clear();
            var context = new AudioPluginContext(manager, this, mixer);
            foreach (var plugin in globalPlugins
                         .Where(candidate => candidate)
                         .OrderBy(candidate => candidate.order)) {
                token.ThrowIfCancellationRequested();
                await plugin.Initialize(context).AttachExternalCancellation(token);
                _initializedPlugins.Add(plugin);
            }
        }

        private async UniTask ShutdownAsync() {
            if (!_shuttingDown) _shuttingDown = true;
            ready = false;

            var manager = AudioManager.Instance;
            manager.Shutdown();
            var context = new AudioPluginContext(manager, this, mixer);
            for (var i = _initializedPlugins.Count - 1; i >= 0; i--) {
                var plugin = _initializedPlugins[i];
                if (!plugin) continue;
                try {
                    await plugin.Shutdown(context);
                } catch (Exception exception) {
                    Debug.LogException(exception, plugin);
                }
            }

            _initializedPlugins.Clear();
            if (_registryLoaded) {
                SpookAudioRegistry.Instance.Dispose();
                _registryLoaded = false;
            }

            _lifetimeSource?.Dispose();
            _lifetimeSource = null;
        }

    }
}
