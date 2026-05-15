using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Spookline.SPC.Events;
using Spookline.SPC.Ext;

namespace Spookline.SPC.Focus {
    public class FocusManager : SpookManagerBehaviour<FocusManager> {

        public readonly List<FocusContext> stack = new();

        public int flags = 0;
        public int stackFlags = 0;
        public FocusContext active = null;
        public bool IsFocused => active != null;

        public bool HasFlag(int flag) => (flags & flag) != 0;
        public bool HasStackFlag(int flag) => (stackFlags & flag) != 0;


        protected override void Awake() {
            base.Awake();
            On<GlobalTickEvt>().Throttled(1f / 30).Do(_ => ProcessFocus());

            // On global mode changes, listeners and contexts might need updates
            On<GlobalModeChangedEvt>().Do(
                _ => {
                    ProcessFocus();
                    Replay();
                },
                EventPriority.Late
            );
        }

        public void SendAmbiguousEscapeInput() {
            if (SendCancelInput()) return;
            SendMenuInput();
        }

        public bool SendCancelInput() {
            if (HasFlag(DefaultFocusFlags.EscapeCancelable)) {
                Release(active);
                return true;
            }

            var result = new CancelInputEvt().Raise();
            return result.handled;
        }

        public bool SendMenuInput() {
            var result = new MenuInputEvt().Raise();
            return result.handled;
        }

        public void Release(FocusContext context) {
            if (!context.suspended && !context.active) return;
            context.suspended = false;
            context.active = false;
            context.AsHandler?.OnFocusLost(context);
            context.Dispose();

            var hasRemoved = stack.Remove(context);
            if (hasRemoved) ProcessFocus();
        }

        public void Release(IFocusable focusable) {
            var hasRemoved = false;
            for (var i = stack.Count - 1; i >= 0; i--) {
                var context = stack.ElementAt(i);
                if (context.focusable != focusable) continue;
                stack.RemoveAt(i);
                Removed(context);
                hasRemoved = true;
            }

            if (hasRemoved) ProcessFocus();
        }

        public void Focus(FocusContext context) {
            if (stack.Contains(context)) stack.Remove(context);
            stack.Add(context);
            ProcessFocus();
        }

        public FocusContext Focus(IFocusable focusable) {
            var current = stack.LastOrDefault(c => c.focusable == focusable);
            current ??= new FocusContext(focusable);
            Focus(current);
            return current;
        }

        public FocusContext Focus(IFocusable focusable, CancellationToken token) {
            var current = stack.LastOrDefault(c => c.focusable == focusable);
            current ??= new FocusContext(focusable, token);
            Focus(current);
            return current;
        }

        public bool HasFocus(IFocusable focusable, out FocusContext context) {
            context = stack.LastOrDefault(c => c.focusable == focusable);
            return context != null;
        }

        public void ProcessFocus() {
            // Cleanup context using the cancellation token
            for (var i = stack.Count - 1; i >= 0; i--) {
                var current = stack.ElementAt(i);
                if (!current.IsCancellationRequested) continue;
                stack.RemoveAt(i);
                Removed(current);
            }

            var stackFlagsAcc = 0;

            // Make sure the tail has the correct focus
            if (stack.Count > 1) {
                for (var i = stack.Count - 2; i >= 0; i--) {
                    var current = stack.ElementAt(i);
                    stackFlagsAcc |= current.flags;
                    if (!current.active) continue;
                    current.active = false;
                    current.AsHandler?.OnSuspendFocus(current);
                }
            }

            // Make sure the head has the correct focus
            if (stack.Count > 0) {
                var head = stack.Last();
                stackFlagsAcc |= head.flags;
                if (head.suspended) {
                    head.suspended = false;
                    head.active = true;
                    head.AsHandler?.OnResumeFocus(head);
                } else if (!head.active) {
                    head.active = true;
                    head.AsHandler?.OnFocusGained(head);
                } else return; // Nothing to do, already active

                active = head;
                flags = head.flags;
                stackFlags = stackFlagsAcc;

                new FocusChangedEvt(this).Raise();
            } else { SetCleared(); }
        }

        public void Clear() {
            while (stack.Count > 0) {
                var removed = Pop();
                Removed(removed);
            }

            SetCleared();
        }

        public void Replay() {
            new FocusChangedEvt(this).Raise();
        }

        private void SetCleared() {
            flags = 0;
            stackFlags = 0;
            active = null;
            new FocusChangedEvt(this).Raise();
        }

        private FocusContext Pop() {
            var removed = stack.Last();
            stack.RemoveAt(stack.Count - 1);
            return removed;
        }

        private void Removed(FocusContext removed) {
            var hadState = removed.active || removed.suspended;
            removed.active = false;
            removed.suspended = false;
            if (hadState) removed.AsHandler?.OnFocusLost(removed);
            removed.Dispose();
        }

    }

    public interface IFocusable {

        int FocusFlags { get; }

    }

    public class IdentityFocusable : IFocusable {

        public int FocusFlags { get; }

        public IdentityFocusable(int flags) {
            FocusFlags = flags;
        }

    }

    public interface IFocusHandler {

        void OnFocusGained(FocusContext context);

        void OnFocusLost(FocusContext context);

        void OnSuspendFocus(FocusContext context) { }

        void OnResumeFocus(FocusContext context) { }

    }

    public interface IFocusListener {

        void OnFocusChanged(FocusChangedEvt evt);

    }

    public struct FocusChangedEvt : Evt<FocusChangedEvt> {

        public int flags;
        public int stackFlags;
        public FocusContext active;

        public FocusChangedEvt(FocusManager manager) : this() {
            flags = manager.flags;
            stackFlags = manager.stackFlags;
            active = manager.active;
        }

        public bool HasFlag(int flag) => (flags & flag) != 0;
        public bool HasStackFlag(int flag) => (stackFlags & flag) != 0;

    }

    public struct CancelInputEvt : Evt<CancelInputEvt> {

        public bool handled;

    }

    public struct MenuInputEvt : Evt<MenuInputEvt> {

        public bool handled;

    }

}