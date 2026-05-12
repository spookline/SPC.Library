using System;
using System.Collections.Generic;
using FishNet.Object;
using Spookline.SPC.Events;
using Spookline.SPC.Ext;

namespace Spookline.SPC.FishNet {
    public abstract class SpookNetworkBehaviour<TSelf> : NetworkBehaviour, ISpookBehaviourT<TSelf>
        where TSelf : SpookNetworkBehaviour<TSelf> {

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
}