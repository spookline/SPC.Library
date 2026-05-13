using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Spookline.SPC.Console;
using Spookline.SPC.Draw;
using Spookline.SPC.Events;
using Spookline.SPC.Ext;
using UnityEngine;

namespace Spookline.SPC {

    [HideMonoScript]
    public partial class Globals : SerializedMonoBehaviour {

        public static Globals Instance { get; private set; }

        [SerializeField]
        public List<Module> modules;

        public Dictionary<Type, ModuleInstance> ModulesByType { get; } = new();

        public bool Started { get; private set; }


        private void Awake() {
            Instance = this;
            Debugging = GetEnvironmentDebugging();
            SetupLogMessageReceiver();

            DontDestroyOnLoad(gameObject);
            ModdingEntrypoint();
            foreach (var module in modules) {
                var instance = new ModuleInstance(module);
                ModulesByType[module.GetTypeDelegate()] = instance;
                module.Load();
            }

            AsyncInitFlow().Forget();
        }

        private void OnDestroy() {
            foreach (var module in modules) module.Unload();

            TeardownLogMessageReceiver();
            Instance = null;
            Started = false;
        }

        private void LateUpdate() {
            if (DebugDraw) new DebugDrawEvt {
                drawer = Drawing.Poly(),
                flags = DebugFlags,
                debugging = Debugging
            }.RaiseSafe();
        }

        public static UniTask UntilStarted() {
            return UniTask.WaitUntil(() => Instance && Instance.Started);
        }

        private async UniTask AsyncInitFlow() {
            await new GlobalStartEvt().RaiseAsync();
            Started = true;

            RefreshDebugFlags();
            CommandSystem.Instance.Refresh();
        }

        public void ModdingEntrypoint() { }

    }

    public class GlobalStartEvt : AsyncChainEvt<GlobalStartEvt> { }
}