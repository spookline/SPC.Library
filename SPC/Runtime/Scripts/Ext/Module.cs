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


        public virtual void Load() { }

        public virtual void Unload() {
            DisposableContainer.DisposeAll(_disposables);
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

        protected virtual void OnDestroy() {
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