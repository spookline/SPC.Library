using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Spookline.SPC.Events;
using UnityEngine;

namespace Spookline.SPC.Ext {

    public abstract class Module<TSelf> : Module where TSelf : Module<TSelf> {

        public static bool HasInstance => Instance;
        public static TSelf Instance { get; private set; }


        public override void Load() {
            if (Instance != null) Debug.LogError($"Instance of {typeof(TSelf).Name} already exists.");
            Instance = (TSelf)this;
        }

        public override void Unload() {
            base.Unload();
            Instance = null;
        }

        public override Type GetTypeDelegate() {
            return typeof(TSelf);
        }

    }

    [ShowOdinSerializedPropertiesInInspector]
    public abstract class Module : SerializedScriptableObject, IModule, IDisposableContainer {

        private readonly List<IDisposable> _disposables = new();

        public void DisposeOnDestroy(IDisposable disposable) {
            _disposables.Add(disposable);
        }

        public void RemoveOnDestroyDisposal(IDisposable disposable) {
            _disposables.Remove(disposable);
        }

        public virtual void Load() { }

        public virtual void Unload() {
            foreach (var disposable in _disposables) disposable.Dispose();
        }

        public virtual Type GetTypeDelegate() {
            return GetType();
        }

        public EventCallbackBuilder<T> On<T>() where T : Evt<T> {
            return new EventCallbackBuilder<T>(this);
        }

        public EventCallbackBuilder<T> On<T>(EventReactor<T> reactor) where T : Evt<T> {
            return new EventCallbackBuilder<T>(this, reactor);
        }

    }

    [ShowOdinSerializedPropertiesInInspector]
    public abstract class OdinModule<T> : Module<T> where T : OdinModule<T> { }

    public interface IModule {

        public Type GetTypeDelegate();

        public void Load();
        public void Unload();

    }

    public class ModuleInstance {

        public IModule module;

        public ModuleInstance(IModule module) {
            this.module = module;
        }

    }
}