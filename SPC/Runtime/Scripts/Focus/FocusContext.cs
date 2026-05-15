using System;
using System.Threading;

namespace Spookline.SPC.Focus {
    public class FocusContext : IDisposable {

        // ReSharper disable once SuspiciousTypeConversion.Global
        public IFocusHandler AsHandler => focusable as IFocusHandler;


        private readonly CancellationTokenSource _cancellation;
        public readonly IFocusable focusable;
        public readonly int flags;
        public bool active;
        public bool suspended;

        public CancellationToken CancellationToken => _cancellation.Token;
        public bool IsCancellationRequested => _cancellation.IsCancellationRequested;

        public FocusContext(IFocusable focusable) {
            this.focusable = focusable;
            flags = focusable.FocusFlags;
            _cancellation = new CancellationTokenSource();
        }

        public FocusContext(IFocusable focusable, CancellationToken token) {
            this.focusable = focusable;
            flags = focusable.FocusFlags;
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        }

        public bool HasFlag(int flag) {
            return (flags & flag) != 0;
        }

        public void Dispose() {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
        }

    }
}