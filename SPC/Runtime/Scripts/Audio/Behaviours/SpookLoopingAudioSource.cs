using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Spookline.SPC.Ext;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace Spookline.SPC.Audio.Behaviours {
    public enum LoopDisableAction {
        Release,
        Stop,
        Pause,
        Continue
    }

    /// <summary>
    /// Scene-facing controller for a pooled, cross-faded looping AudioDefinition.
    /// It can be driven by component lifecycle events or controlled manually.
    /// </summary>
    public sealed class SpookLoopingAudioSource : SpookBehaviour<SpookLoopingAudioSource> {

        [TitleGroup("Sound")]
        public AssetReferenceT<AudioDefinition> sound;

        [TitleGroup("Playback")]
        [Tooltip("Automatically requests playback whenever this component becomes enabled.")]
        public bool playOnEnable = true;

        [TitleGroup("Playback")]
        [Tooltip("What happens to prepared pooled voices when this component becomes disabled.")]
        [FormerlySerializedAs("onDisable")]
        public LoopDisableAction disableAction = LoopDisableAction.Release;

        [TitleGroup("Playback")]
        [MinValue(0f)]
        [Tooltip("Delay before automatic or manually requested playback is prepared.")]
        public float startDelay;

        [TitleGroup("Playback")]
        [Tooltip("Makes Start Delay independent of Time.timeScale.")]
        public bool useUnscaledDelay;

        [TitleGroup("Loop")]
        [MinValue(0f)]
        public float crossfadeDuration = 0.25f;

        [TitleGroup("Loop")]
        [MinValue(2)]
        [Tooltip("Number of pooled voices in the loop ring. Raise this for short or highly varied clips.")]
        public int cycleCount = 2;

        [TitleGroup("Loop")]
        public AudioFadeCurve fadeCurve = AudioFadeCurve.SmoothStep;

        [TitleGroup("Loop")]
        [Tooltip("Starts at a random point in the first clip, useful for unsynchronized ambient emitters.")]
        public bool randomizeFirstStartTime;

        [TitleGroup("Spatial")]
        [FormerlySerializedAs("spacialize")]
        public bool spatialize = true;

        [TitleGroup("Spatial")]
        [Tooltip("Optional target to follow. Defaults to this component's transform.")]
        public Transform trackingTarget;

        [TitleGroup("Spatial")]
        [Tooltip("Pauses and resumes this loop when the application is suspended.")]
        public bool pauseWithApplication = true;

        [TitleGroup("Overrides")]
        [HideLabel]
        public AudioOptionOverride overrides = new();

        private LoopingAudioJob _loopingAudioJob;
        private CancellationTokenSource _preparationCancellation;
        private bool _isPreparing;
        private bool _lastPreparationSucceeded;
        private bool _muted;
        private bool _pausedByApplication;
        private bool _hasRuntimeVolume;
        private bool _hasRuntimePitch;
        private bool _hasRuntimeSpatialBlend;
        private float _runtimeVolume;
        private float _runtimePitch;
        private float _runtimeSpatialBlend;

        public bool IsPreparing => _isPreparing;
        public bool IsPrepared => _loopingAudioJob is { IsPrepared: true };
        public bool IsPlaying => _loopingAudioJob is { IsRunning: true };
        public bool IsPaused => _loopingAudioJob is { IsPaused: true };
        public bool IsMuted => _muted;
        public LoopingAudioJob Loop => _loopingAudioJob;

        protected override void OnEnable() {
            base.OnEnable();
            if (playOnEnable) Play();
        }

        protected override void OnDisable() {
            switch (disableAction) {
                case LoopDisableAction.Release:
                    Release();
                    break;
                case LoopDisableAction.Stop:
                    Stop();
                    break;
                case LoopDisableAction.Pause:
                    Pause();
                    CancelPreparation();
                    break;
                case LoopDisableAction.Continue:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            base.OnDisable();
        }

        protected override void OnDestroy() {
            CancelPreparation();
            DisposeLoop();
            base.OnDestroy();
        }

        private void OnApplicationPause(bool paused) {
            if (!pauseWithApplication) return;
            if (paused) {
                _pausedByApplication = IsPlaying;
                if (_pausedByApplication) Pause();
            } else if (_pausedByApplication) {
                _pausedByApplication = false;
                Resume();
            }
        }

        private void OnValidate() {
            startDelay = Mathf.Max(0f, startDelay);
            crossfadeDuration = Mathf.Max(0f, crossfadeDuration);
            cycleCount = Mathf.Max(2, cycleCount);
        }

        [Button]
        public void Play() {
            PlayAsync().Forget();
        }

        public async UniTask<bool> PlayAsync(CancellationToken cancellationToken = default) {
            if (!await EnsurePrepared(true, cancellationToken)) return false;
            if (!this || _loopingAudioJob == null) return false;

            if (_loopingAudioJob.IsPaused) {
                Resume();
                return true;
            }

            if (_loopingAudioJob.IsRunning) return true;
            _loopingAudioJob.Start();
            return _loopingAudioJob.IsRunning;
        }

        public UniTask<bool> Prepare(CancellationToken cancellationToken = default) {
            return EnsurePrepared(false, cancellationToken);
        }

        [Button]
        public void Stop() {
            CancelPreparation();
            if (_loopingAudioJob == null) return;
            _loopingAudioJob.Stop();
            _pausedByApplication = false;
        }

        [Button]
        public void Restart() {
            Stop();
            Play();
        }

        [Button]
        public void Pause() {
            if (_loopingAudioJob is not { IsRunning: true }) return;
            _loopingAudioJob.Pause();
        }

        [Button]
        public void Resume() {
            if (_loopingAudioJob is not { IsPaused: true }) return;
            _loopingAudioJob.Resume();
        }

        [Button]
        public void Release() {
            CancelPreparation();
            DisposeLoop();
        }

        public async UniTask<bool> Rebuild(
            bool playAfterRebuild = true,
            CancellationToken cancellationToken = default
        ) {
            Release();
            return playAfterRebuild
                ? await PlayAsync(cancellationToken)
                : await Prepare(cancellationToken);
        }

        public void SetVolume(float volume) {
            _hasRuntimeVolume = true;
            _runtimeVolume = Mathf.Clamp01(volume);
            _loopingAudioJob?.SetVolume(_runtimeVolume);
        }

        public void SetPitch(float pitch) {
            _hasRuntimePitch = true;
            _runtimePitch = Mathf.Clamp(pitch, -3f, 3f);
            _loopingAudioJob?.SetPitch(_runtimePitch);
        }

        public void SetSpatialBlend(float spatialBlend) {
            _hasRuntimeSpatialBlend = true;
            _runtimeSpatialBlend = Mathf.Clamp01(spatialBlend);
            _loopingAudioJob?.SetSpatialBlend(_runtimeSpatialBlend);
        }

        public void SetMuted(bool muted) {
            _muted = muted;
            _loopingAudioJob?.SetMuted(muted);
        }

        public void SetTrackingTarget(Transform target, bool rebuildIfPrepared = true) {
            if (trackingTarget == target) return;
            trackingTarget = target;
            if (rebuildIfPrepared && IsPrepared) Rebuild(IsPlaying).Forget();
        }

        public void ClearRuntimeOverrides() {
            _hasRuntimeVolume = false;
            _hasRuntimePitch = false;
            _hasRuntimeSpatialBlend = false;
        }

        private async UniTask<bool> EnsurePrepared(
            bool applyStartDelay,
            CancellationToken cancellationToken
        ) {
            if (IsPrepared) return true;
            if (_isPreparing) {
                try {
                    await UniTask.WaitUntil(() => !_isPreparing)
                        .AttachExternalCancellation(cancellationToken);
                } catch (OperationCanceledException) {
                    return false;
                }

                if (_lastPreparationSucceeded && IsPrepared) return true;
                return await EnsurePrepared(applyStartDelay, cancellationToken);
            }

            _isPreparing = true;
            _lastPreparationSucceeded = false;
            _preparationCancellation = new CancellationTokenSource();
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _preparationCancellation.Token,
                this.GetCancellationTokenOnDestroy());

            LoopingAudioJob pendingLoop = null;
            try {
                var token = linkedCancellation.Token;
                await SpookAudioModule.Ready.AttachExternalCancellation(token);

                if (applyStartDelay && startDelay > 0f)
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(startDelay),
                        ignoreTimeScale: useUnscaledDelay,
                        cancellationToken: token);

                if (sound == null || string.IsNullOrWhiteSpace(sound.AssetGUID))
                    throw new InvalidOperationException("No AudioDefinition is assigned.");

                var definition = sound.FromRegistry();
                if (!definition)
                    throw new InvalidOperationException(
                        $"Audio definition '{sound.AssetGUID}' is not loaded.");

                var job = definition.Job(token);
                if (overrides is { hasOverride: true }) job = job.WithOptions(overrides.options);
                if (_hasRuntimeVolume) job = job.With(options => options.Volume(_runtimeVolume));
                if (_hasRuntimePitch) job = job.With(options => options.Pitch(_runtimePitch));
                if (_hasRuntimeSpatialBlend)
                    job = job.With(options => options.SpatialBlend(_runtimeSpatialBlend));

                var target = spatialize ? (trackingTarget ? trackingTarget : transform) : null;
                pendingLoop = new LoopingAudioJob(
                    job,
                    target,
                    crossfadeDuration,
                    cycleCount,
                    spatialize ? job.options.spatialBlend : 0f,
                    fadeCurve,
                    randomizeFirstStartTime);
                await pendingLoop.Setup();
                token.ThrowIfCancellationRequested();

                _loopingAudioJob = pendingLoop;
                pendingLoop = null;
                _loopingAudioJob.SetMuted(_muted);
                _lastPreparationSucceeded = true;
                return true;
            } catch (OperationCanceledException) {
                return false;
            } catch (Exception exception) {
                if (linkedCancellation.IsCancellationRequested) return false;
                Debug.LogException(exception, this);
                return false;
            } finally {
                pendingLoop?.Dispose();
                _isPreparing = false;
                _preparationCancellation?.Dispose();
                _preparationCancellation = null;
            }
        }

        private void CancelPreparation() {
            if (_preparationCancellation is { IsCancellationRequested: false })
                _preparationCancellation.Cancel();
        }

        private void DisposeLoop() {
            if (_loopingAudioJob == null) return;
            _loopingAudioJob.Dispose();
            _loopingAudioJob = null;
            _pausedByApplication = false;
        }

    }
}
