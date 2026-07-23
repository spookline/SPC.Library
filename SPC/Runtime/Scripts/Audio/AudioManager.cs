using System;
using System.Collections.Generic;
using Spookline.SPC.Audio.Events;
using Spookline.SPC.Events;
using Spookline.SPC.Ext;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Spookline.SPC.Audio {
    /// <summary>Owns pooled AudioSources and dispatches optional integration callbacks.</summary>
    public sealed class AudioManager : Singleton<AudioManager> {

        private IReadOnlyList<AudioGlobalPlugin> _globalPlugins = Array.Empty<AudioGlobalPlugin>();
        private AudioMixer _mixer;
        private ObjectPool<AudioHandle> _pool;
        private Transform _poolParent;
        private bool _shuttingDown;
        private int _initialCapacity;

        public AudioMixer Mixer => _mixer ??= SpookAudioModule.HasInstance ? SpookAudioModule.Instance.mixer : null;
        public Transform PoolRoot => _poolParent;
        public int CountActive => _pool?.CountActive ?? 0;
        public int CountInactive => _pool?.CountInactive ?? 0;
        public int CountAll => _pool?.CountAll ?? 0;

        internal void Initialize(int initialCapacity, int maxPoolSize) {
            Shutdown();
            _shuttingDown = false;

            var capacity = Mathf.Max(0, initialCapacity);
            var maximum = Mathf.Max(1, maxPoolSize);
            capacity = Mathf.Min(capacity, maximum);
            _initialCapacity = capacity;

            var poolObject = new GameObject("[SPC] Audio Source Pool");
            Object.DontDestroyOnLoad(poolObject);
            _poolParent = poolObject.transform;
            _pool = new ObjectPool<AudioHandle>(
                OnPoolCreate,
                OnPoolGet,
                OnPoolRelease,
                OnPoolDestroy,
                true,
                capacity,
                maximum);
        }

        internal void WarmPool() {
            if (_pool == null || _initialCapacity == 0) return;
            var warmup = new AudioHandle[_initialCapacity];
            for (var i = 0; i < warmup.Length; i++) warmup[i] = _pool.Get();
            for (var i = 0; i < warmup.Length; i++) _pool.Release(warmup[i]);
        }

        internal void Shutdown() {
            if (_shuttingDown) return;
            _shuttingDown = true;
            _pool?.Dispose();
            _pool = null;
            _globalPlugins = Array.Empty<AudioGlobalPlugin>();
            _mixer = null;
            _initialCapacity = 0;

            if (_poolParent) Object.Destroy(_poolParent.gameObject);
            _poolParent = null;
        }

        internal void ClearAudioPool() {
            _pool?.Clear();
        }

        internal void SetGlobalPlugins(IReadOnlyList<AudioGlobalPlugin> plugins) {
            _globalPlugins = plugins ?? Array.Empty<AudioGlobalPlugin>();
        }

        internal AudioHandle Lease(AudioJob job) {
            if (_pool == null) throw new InvalidOperationException("The audio pool has not been initialized.");
            var handle = _pool.Get();
            InvokePlugins(plugin => plugin.OnHandleLeased(handle, job));
            return handle;
        }

        internal void BeforePlay(AudioHandle handle, AudioJob job) {
            InvokePlugins(plugin => plugin.OnBeforePlay(handle, job));
        }

        internal void Release(AudioHandle handle) {
            if (!handle || _pool == null || _shuttingDown || handle.IsReleased) return;
            _pool.Release(handle);
        }
        
        public bool ChangeMixerVolume(string parameter, float linearVolume) {
            if (!Mixer || string.IsNullOrWhiteSpace(parameter)) return false;
            var decibels = linearVolume <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp01(linearVolume)) * 20f;
            return Mixer.SetFloat(parameter, decibels);
        }

        private AudioHandle OnPoolCreate() {
            var sourceObject = new GameObject("Pooled Audio Source");
            sourceObject.transform.SetParent(_poolParent, false);
            sourceObject.AddComponent<AudioSource>();
            var handle = sourceObject.AddComponent<AudioHandle>();
            new AudioHandleCreatedEvt { SourceObject = sourceObject, Handle = handle }.Raise();
            InvokePlugins(plugin => plugin.OnHandleCreated(handle));
            return handle;
        }

        private static void OnPoolGet(AudioHandle handle) {
            handle.gameObject.SetActive(true);
        }

        private void OnPoolRelease(AudioHandle handle) {
            InvokePlugins(plugin => plugin.OnHandleReleased(handle));
            handle.ResetForPool();
            handle.gameObject.SetActive(false);
            handle.transform.SetParent(_poolParent, false);
        }

        private void OnPoolDestroy(AudioHandle handle) {
            if (!handle) return;
            InvokePlugins(plugin => plugin.OnHandleDestroyed(handle));
            new AudioHandleDestroyedEvt { SourceObject = handle.gameObject, Handle = handle }.Raise();
            Object.Destroy(handle.gameObject);
        }

        private void InvokePlugins(Action<AudioGlobalPlugin> callback) {
            foreach (var plugin in _globalPlugins) {
                if (!plugin) continue;
                try {
                    callback(plugin);
                } catch (Exception exception) {
                    Debug.LogException(exception, plugin);
                }
            }
        }

    }
}
