using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Spookline.SPC.Audio {
    public class AudioJobReference : IDisposable {

        public AudioHandle handle;
        public AudioJob job;
        internal AudioJobReferenceState state = AudioJobReferenceState.Uninitialized;

        public bool IsValid => handle && handle.IsOwnedBy(this);
        public bool IsPlaying => IsValid && handle.IsPlaying;

        public bool IsPending => state == AudioJobReferenceState.Pending ||
                                 (state == AudioJobReferenceState.Ready && !handle.HasEnded);


        private CancellationTokenRegistration _registration;

        /// <summary>
        ///     Fully disposes this job reference even if it is still pending.
        /// </summary>
        public void Dispose() {
            if (state == AudioJobReferenceState.Killed) return;
            state = AudioJobReferenceState.Killed;
            _registration.Dispose();
            if (!IsValid) return;
            Stop();
            if (!handle.IsReleased)
                AudioManager.Instance.Release(handle);
            else Debug.LogWarning($"Handle {handle.GetEntityId()} already released.");
        }

        internal void RegisterToken(CancellationToken token) {
            _registration.Dispose();
            if (token.IsCancellationRequested) {
                Dispose();
                return;
            }

            if (token.CanBeCanceled) {
                _registration = token.RegisterWithoutCaptureExecutionContext(r => ((AudioJobReference)r).Dispose(), this);
            }
        }

        public async UniTask WaitUntilEnded(CancellationToken cancellationToken = default) {
            if (!IsValid) return;
            using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, 
                job.cancellationToken);
            await handle.WaitUntilEnded().AttachExternalCancellation(combined.Token);
        }

        /// <summary>
        ///     Stops a job if it is currently playing.
        /// </summary>
        public void Stop() {
            if (IsValid && !handle.HasEnded) handle.EndNow();
        }

    }
}