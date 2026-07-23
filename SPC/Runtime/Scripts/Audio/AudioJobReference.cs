using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Spookline.SPC.Audio {
    /// <summary>Lifetime token for a pending or active audio job.</summary>
    public sealed class AudioJobReference : IDisposable {

        public AudioHandle handle;
        public AudioJob job;

        internal AudioJobReferenceState state = AudioJobReferenceState.Uninitialized;

        private CancellationTokenRegistration _registration;
        private UniTaskCompletionSource _readySource;

        public AudioJobReferenceState State => state;
        public bool IsValid => handle && handle.IsOwnedBy(this);
        public bool IsPlaying => IsValid && handle.IsPlaying;
        public bool IsPending => state == AudioJobReferenceState.Pending;

        public void Dispose() {
            if (state == AudioJobReferenceState.Killed) return;
            state = AudioJobReferenceState.Killed;
            _registration.Dispose();
            _readySource?.TrySetResult();
            if (!IsValid) return;

            if (!handle.HasEnded) handle.EndNow();
            if (handle && !handle.IsReleased && AudioManager.IsInitialized)
                AudioManager.Instance.Release(handle);
        }

        internal void MarkPending() {
            state = AudioJobReferenceState.Pending;
            _readySource = new UniTaskCompletionSource();
        }

        internal void MarkReady(AudioHandle leasedHandle, CancellationToken token) {
            if (state != AudioJobReferenceState.Pending) return;
            handle = leasedHandle;
            state = AudioJobReferenceState.Ready;
            RegisterToken(token);
            _readySource?.TrySetResult();
        }

        internal void MarkFailed() {
            if (state == AudioJobReferenceState.Killed) return;
            state = AudioJobReferenceState.Failed;
            _readySource?.TrySetResult();
        }

        internal void RegisterToken(CancellationToken token) {
            _registration.Dispose();
            if (token.IsCancellationRequested) {
                Dispose();
                return;
            }

            if (token.CanBeCanceled)
                _registration = token.RegisterWithoutCaptureExecutionContext(
                    value => ((AudioJobReference)value).Dispose(), this);
        }

        public async UniTask WaitUntilReady(CancellationToken cancellationToken = default) {
            if (state != AudioJobReferenceState.Pending) return;
            if (_readySource != null)
                await _readySource.Task.AttachExternalCancellation(cancellationToken);
        }

        public async UniTask WaitUntilEnded(CancellationToken cancellationToken = default) {
            using var combined = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, job.cancellationToken);
            await WaitUntilReady(combined.Token);
            if (IsValid) await handle.WaitUntilEnded().AttachExternalCancellation(combined.Token);
        }

        public UniTask.Awaiter GetAwaiter() => WaitUntilEnded().GetAwaiter();

        public void Stop() {
            if (IsValid && !handle.HasEnded) handle.EndNow();
        }

    }
}
