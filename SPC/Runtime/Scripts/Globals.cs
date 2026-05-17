using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Spookline.SPC.Console;
using Spookline.SPC.Debugging;
using Spookline.SPC.Draw;
using Spookline.SPC.Events;
using Spookline.SPC.Ext;
using Spookline.SPC.Focus;
using Spookline.SPC.Geometry;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Spookline.SPC {
    [HideMonoScript]
    [DefaultExecutionOrder(-500)]
    public partial class Globals : SerializedMonoBehaviour {

        public static Globals Instance { get; private set; }

        [OdinSerialize]
        public List<IModule> modules = new();

        public Dictionary<Type, ModuleInstance> ModulesByType { get; } = new();

        public IScreenOverlayAPI debugScreenOverlay = new NoOpScreenOverlayAPI();
        public IWorldOverlayAPI debugWorldOverlay = new NoOpWorldOverlayAPI();

        public bool Started { get; private set; }

        public bool loadResourceModules = true;


        private void Awake() {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            modules ??= new List<IModule>();
            if (loadResourceModules) {
                // ReSharper disable once Unity.UnknownResource
                var resourceCandidates = Resources.LoadAll("Modules");
                foreach (var candidate in resourceCandidates) {
                    if (candidate is IModule module) modules.Add(module);
                }
            }

            // Add default modules
            AddDefaultModules();

            SetupCoreSystem();
            ModdingEntrypoint();
            foreach (var module in modules) {
                var instance = new ModuleInstance(module);
                ModulesByType[module.GetTypeDelegate()] = instance;
                module.Load();
            }

            AsyncInitFlow().Forget();
        }


        public void AddDefaultModules() {
            if (!modules.OfType<FocusModule>().Any()) {
                var fallback = ScriptableObject.CreateInstance<FocusModule>();
                fallback.hideFlags = HideFlags.DontSave;
                modules.Add(fallback);
            }
        }

        private void OnDestroy() {
            foreach (var module in modules) module.Unload();

            TeardownLogMessageReceiver();
            Instance = null;
            Started = false;
        }

        private DateTime _lastDrawTime = DateTime.MinValue;
        public int drawFrequency = 2;
        public int screenOverlayFrequency = 2;
        public int worldOverlayFrequency = 1;
        public double debugRefreshInterval = 1 / 120.0;
        private uint debugFrameCount = 0;

        private void LateUpdate() {
            DebugTick();
        }

        private void Update() {
            new GlobalTickEvt {
                time = Time.time,
                deltaTime = Time.deltaTime
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

        public void SetupCoreSystem() {
            // Debugging
            Debugging = GetEnvironmentDebugging();
            SetupLogMessageReceiver();
        }

    }

    public class GlobalStartEvt : AsyncChainEvt<GlobalStartEvt> { }

    public struct GlobalTickEvt : Evt<GlobalTickEvt> {

        public float time;
        public float deltaTime;

    }

    public struct GlobalModeChangedEvt : Evt<GlobalModeChangedEvt> {

        public string mode;

    }
}