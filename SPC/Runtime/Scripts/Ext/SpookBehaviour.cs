using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using Spookline.SPC.Debugging;
using Spookline.SPC.Events;
using UnityEngine;

namespace Spookline.SPC.Ext {
    public abstract class SpookBehaviour<TSelf> : SerializedMonoBehaviour, ISpookBehaviourT<TSelf>
        where TSelf : SpookBehaviour<TSelf> {

        private readonly List<IDisposable> _disposables = new();

        protected ISpookBehaviour Ext => this;

        protected virtual void Start() {
            onStart?.Invoke();
            new SpookBehaviourStartEvt<TSelf>((TSelf)this).RaiseSafe();
        }

        protected virtual void OnEnable() {
            onEnable?.Invoke();
            new SpookBehaviourEnableEvt<TSelf>((TSelf)this).RaiseSafe();
        }

        protected virtual void OnDisable() {
            new SpookBehaviourDisableEvt<TSelf>((TSelf)this).RaiseSafe();
            onDisable?.Invoke();
        }

        public event Action onStart;

        public event Action onEnable;

        public event Action onDisable;

        public EventCallbackBuilder<T> On<T>(bool whileActive = true) where T : Evt<T> {
            var builder = new EventCallbackBuilder<T>(this);
            if (whileActive) builder = builder.ActiveAndEnabled(this);
            return builder;
        }

        public EventCallbackBuilder<T> On<T>(EventReactor<T> reactor, bool whileActive = true) where T : Evt<T> {
            var builder = new EventCallbackBuilder<T>(this, reactor);
            if (whileActive) builder = builder.ActiveAndEnabled(this);
            return builder;
        }

        protected virtual void OnDestroy() {
            new SpookBehaviourDestroyEvt<TSelf>((TSelf)this).RaiseSafe();
            DisposableContainer.DisposeAll(_disposables);
        }

        public void DisposeOnDestroy(IDisposable disposable) {
            DisposableContainer.Add(_disposables, disposable);
        }

        public void RemoveOnDestroyDisposal(IDisposable disposable) {
            DisposableContainer.Remove(_disposables, disposable);
        }

        public void RaiseLocal<T>(ref T evt) where T : Evt<T> {
            DisposableContainer.RaiseLocal(_disposables, ref evt);
        }
    }

    public interface IDisposableContainer {

        public void DisposeOnDestroy(IDisposable disposable);

        public void DisposeOnDestroy(Action onDispose) {
            var disposable = new LambdaDisposable(onDispose);
            DisposeOnDestroy(disposable);
        }

        public void RemoveOnDestroyDisposal(IDisposable disposable);

        public void RaiseLocal<T>(ref T evt) where T : Evt<T>;

    }

    public static class DisposableContainer {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(List<IDisposable> disposables, IDisposable disposable) {
            if (disposable is HandlerRegistration registration) {
                for (var i = 0; i < disposables.Count; i++) {
                    var cursor = disposables[i];
                    if (cursor is not HandlerRegistration other) continue;
                    if (other.Priority <= registration.Priority) continue;
                    disposables.Insert(i, disposable);
                    return;
                }
            }

            disposables.Add(disposable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Remove(List<IDisposable> disposables, IDisposable disposable) {
            disposables.Remove(disposable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeAll(List<IDisposable> disposables) {
            while (disposables.Count > 0) {
                var disposable = disposables[0];
                disposables.RemoveAt(0);
                disposable.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RaiseLocal<T>(List<IDisposable> disposables, ref T evt) where T : Evt<T> {
            for (var i = 0; i < disposables.Count; i++) {
                var disposable = disposables[i];
                if (disposable is HandlerRegistration<T> registration) { registration.Handler.Invoke(ref evt); }
            }
        }
    }

    public interface ISpookBehaviour : IDisposableContainer, ILifecycleContainer { }

    public interface ISpookBehaviourT<TSelf> : ISpookBehaviour where TSelf : ISpookBehaviourT<TSelf> { }

    public readonly struct SpookBehaviourEnableEvt<T> : Evt<SpookBehaviourEnableEvt<T>> where T : ISpookBehaviourT<T> {

        public readonly T behaviour;
        public readonly GameObject gameObject;

        public SpookBehaviourEnableEvt(T behaviour) {
            this.behaviour = behaviour;
            gameObject = (behaviour as Component)?.gameObject;
        }

    }

    public readonly struct SpookBehaviourDisableEvt<T> : Evt<SpookBehaviourDisableEvt<T>>
        where T : ISpookBehaviourT<T> {

        public readonly T behaviour;
        public readonly GameObject gameObject;

        public SpookBehaviourDisableEvt(T behaviour) {
            this.behaviour = behaviour;
            gameObject = (behaviour as Component)?.gameObject;
        }

    }

    public readonly struct SpookBehaviourStartEvt<T> : Evt<SpookBehaviourStartEvt<T>> where T : ISpookBehaviourT<T> {

        public readonly T behaviour;
        public readonly GameObject gameObject;

        public SpookBehaviourStartEvt(T behaviour) {
            this.behaviour = behaviour;
            gameObject = (behaviour as Component)?.gameObject;
        }

    }

    public readonly struct SpookBehaviourDestroyEvt<T> : Evt<SpookBehaviourDestroyEvt<T>>
        where T : ISpookBehaviourT<T> {

        public readonly T behaviour;
        public readonly GameObject gameObject;

        public SpookBehaviourDestroyEvt(T behaviour) {
            this.behaviour = behaviour;
            gameObject = (behaviour as Component)?.gameObject;
        }

    }
}