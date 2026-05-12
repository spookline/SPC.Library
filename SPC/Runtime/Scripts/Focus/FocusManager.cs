using System.Collections.Generic;
using System.Linq;
using Spookline.SPC.Ext;
using UnityEngine;

namespace Spookline.SPC.Focus {
    public class FocusManager : Singleton<FocusManager> {

        public readonly Stack<FocusContext> stack = new();
        private readonly List<IFocusHandler> _handlers = new();

        public bool IsFocused => stack.Count > 0;
        public FocusContext ActiveFocus => IsFocused ? stack.Peek() : null;

        public FocusManager() {
            Register(new CursorFocusHandler());
        }

        public void Register(IFocusHandler handler) {
            if (_handlers.Contains(handler)) return;
            _handlers.Add(handler);
        }

        public void Unregister(IFocusHandler handler) {
            _handlers.Remove(handler);
        }

        public void RequestFocus(IFocusable focusable) {
            var context = new FocusContext(focusable);
            if (stack.Count > 0) {
                var previous = stack.Peek();
                previous.AsHandler?.OnSuspendFocus(previous);
                foreach (var handler in _handlers) {
                    handler.OnSuspendFocus(previous);
                }
            }

            stack.Push(context);
            context.AsHandler?.OnFocusGained(context);
            foreach (var handler in _handlers) {
                handler.OnFocusGained(context);
            }
        }

        public void ReleaseFocus(IFocusable focusable) {
            if (stack.Count == 0 || stack.Peek().Focusable != focusable) return;
            var removed = stack.Pop();
            removed.AsHandler?.OnFocusLost(removed);
            foreach (var handler in _handlers) {
                handler.OnFocusLost(removed);
            }

            if (stack.Count > 0) {
                var previous = stack.Peek();
                previous.AsHandler?.OnResumeFocus(previous);
                foreach (var handler in _handlers) {
                    handler.OnResumeFocus(previous);
                }
            }
        }

        public void Clear() {
            while (stack.Count > 0) {
                var removed = stack.Pop();
                removed.AsHandler?.OnFocusLost(removed);
                foreach (var handler in _handlers) {
                    handler.OnFocusLost(removed);
                }
            }
        }

    }

    public class FocusContext {

        public IFocusable Focusable { get; }
        

        public IFocusHandler AsHandler => Focusable as IFocusHandler;

        public short FocusFlags => Focusable.FocusFlags;

        public FocusContext(IFocusable focusable) {
            Focusable = focusable;
        }

        public bool HasFlag(short flag) {
            return (FocusFlags & flag) != 0;
        }

        public bool IsFlagActiveInStack(short flag) {
            return FocusManager.Instance.stack.Any(context => (context.FocusFlags & flag) != 0);
        }

    }

    public interface IFocusable {

        short FocusFlags { get; }
        
        bool IsBeingFocused => FocusManager.Instance.ActiveFocus?.Focusable == this;

    }

    public interface IFocusHandler {

        void OnFocusGained(FocusContext context);

        void OnFocusLost(FocusContext context);

        void OnSuspendFocus(FocusContext context) { }

        void OnResumeFocus(FocusContext context) { }

    }

    public abstract class FocusHandlerBehaviour : MonoBehaviour, IFocusHandler {

        protected virtual void OnEnable() {
            FocusManager.Instance.Register(this);
        }

        protected virtual void OnDisable() {
            FocusManager.Instance.Unregister(this);
        }

        public abstract void OnFocusGained(FocusContext context);

        public abstract void OnFocusLost(FocusContext context);

    }
}