using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Spookline.SPC.Events;
using UnityEngine;

namespace Spookline.SPC.Ext {
    public abstract class SpookBehaviour<TSelf> : SerializedMonoBehaviour, ISpookBehaviourT<TSelf> where TSelf : SpookBehaviour<TSelf> {

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

        protected virtual void OnDestroy() {
            new SpookBehaviourDestroyEvt<TSelf>((TSelf)this).RaiseSafe();
            foreach (var disposable in _disposables) disposable.Dispose();
        }

        public event Action onStart;
        public event Action onEnable;
        public event Action onDisable;

        public void DisposeOnDestroy(IDisposable disposable) {
            _disposables.Add(disposable);
        }

        public void RemoveOnDestroyDisposal(IDisposable disposable) {
            _disposables.Remove(disposable);
        }

        public EventCallbackBuilder<T> On<T>() where T : Evt<T> {
            return new EventCallbackBuilder<T>(this);
        }

        public EventCallbackBuilder<T> On<T>(EventReactor<T> reactor) where T : Evt<T> {
            return new EventCallbackBuilder<T>(this, reactor);
        }

    }

    public interface IDisposableContainer {

        public void DisposeOnDestroy(IDisposable disposable);

        public void DisposeOnDestroy(Action onDispose) {
            var disposable = new LambdaDisposable(onDispose);
            DisposeOnDestroy(disposable);
        }

        public void RemoveOnDestroyDisposal(IDisposable disposable);

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

    public readonly struct  SpookBehaviourDisableEvt<T> : Evt<SpookBehaviourDisableEvt<T>> where T : ISpookBehaviourT<T> {

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

    public readonly struct SpookBehaviourDestroyEvt<T> : Evt<SpookBehaviourDestroyEvt<T>> where T : ISpookBehaviourT<T> {

        public readonly T behaviour;
        public readonly GameObject gameObject;

        public SpookBehaviourDestroyEvt(T behaviour) {
            this.behaviour = behaviour;
            gameObject = (behaviour as Component)?.gameObject;
        }
    }
}