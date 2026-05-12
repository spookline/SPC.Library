using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Random = UnityEngine.Random;

namespace Spookline.SPC.Audio {
    [Serializable]
    public class RangeAudioSourceProvider : IAudioSourceProvider {

        public List<AudioAssetEntry> clips = new();

        [NonSerialized]
        private List<AsyncOperationHandle> _handles;

        [NonSerialized]
        private List<AudioClip> _loadedClips;

        public bool IsLoaded { get; private set; } // ReSharper disable Unity.PerformanceAnalysis

        public async UniTask Load() {
            if (IsLoaded) {
                Debug.LogWarning("AudioSourceProvider is already loaded.");
                return;
            }

            _handles = new List<AsyncOperationHandle>();
            _loadedClips = new List<AudioClip>();

            foreach (var entry in clips) {
                var handle = Addressables.LoadAssetAsync<AudioClip>(entry.clip.AssetGUID);
                var clip = await handle;
                if (!clip) {
                    Debug.LogError($"Failed to load audio clip: {entry.clip}");
                    continue;
                }

                _handles!.Add(handle);
                _loadedClips!.Add(clip);
            }

            IsLoaded = true;
        }

        public UniTask Unload() {
            foreach (var handle in _handles)
                if (handle.IsValid())
                    Addressables.Release(handle);

            _loadedClips?.Clear();
            _handles?.Clear();
            IsLoaded = false;
            return UniTask.CompletedTask;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public AudioJob CreateJob(AudioDefinition definition, CancellationToken cancellationToken = default) {
            switch (clips.Count) {
                case 1:
                    return new AudioJob(definition, 0, definition.options, cancellationToken);
                case > 1: {
                    var randomIndex = Random.Range(0, clips.Count);
                    return new AudioJob(definition, randomIndex, definition.options, cancellationToken);
                }
                default:
                    Debug.LogError("No audio clips available in RangeAudioSourceProvider.");
                    return default;
            }
        }

        public AudioClip GetClip(AudioJob job) {
            return _loadedClips[job.data];
        }

        public void Apply(AudioHandle handle, AudioJob job) {
            handle.source.clip = _loadedClips[job.data];
            job.options.ApplyTo(handle.source);
        }

    }
}