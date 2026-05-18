using System;
using System.Collections.Generic;
using System.Linq;
using Dahomey.Cbor.ObjectModel;
using Sirenix.OdinInspector;
using Spookline.SPC.Debugging;
using Spookline.SPC.Draw;
using Spookline.SPC.Events;
using Spookline.SPC.Geometry;
using Spookline.SPC.Save;
using UnityEngine;

namespace Spookline.SPC {
    public partial class Globals {

        public static bool IsDebugging => Instance?.Debugging ?? GetEnvironmentDebugging();

        private static bool GetEnvironmentDebugging() {
#if DEBUG
            return true;
#endif
            return false;
        }

        public static bool HasDebugFlag(string flag) => Instance?.DebugFlags.Contains(flag) ?? false;
        public static bool HasDebugFlagOrDebugging(string flag) => IsDebugging || HasDebugFlag(flag);

        [ShowInInspector, HideInEditorMode]
        public bool Debugging { get; set; }

        [ShowInInspector, HideInEditorMode]
        public bool DebugGizmos { get; private set; } = true;

        [ShowInInspector, HideInEditorMode]
        public bool DebugDraw { get; private set; } = true;

        [ShowInInspector, HideInEditorMode]
        public bool DebugScreenOverlay { get; private set; } = true;

        [ShowInInspector, HideInEditorMode]
        public bool DebugWorldOverlay { get; private set; } = true;

        [NonSerialized]
        public Camera debugCamera;

        public HashSet<string> DebugFlags { get; } = new();
        public HashSet<string> AvailableDebugFlags { get; } = new();

        [ShowInInspector, LabelText("Debug Flags"), HideInEditorMode]
        private ISet<string> EditorDebugFlags {
            get => DebugFlags;
            set => SetDebugFlags(value);
        }

        private void CallFlagsChanged() =>
            new DebugFlagsChangedEvt { flags = DebugFlags, debugging = Debugging }.Raise();

        public void SetDebugDraw(bool value) {
            if (value == DebugDraw) return;
            DebugDraw = value;
            CallFlagsChanged();
        }

        public void SetDebugGizmos(bool value) {
            if (value == DebugGizmos) return;
            DebugGizmos = value;
            CallFlagsChanged();
        }

        public void SetDebugScreenOverlay(bool value) {
            if (value == DebugScreenOverlay) return;
            DebugScreenOverlay = value;
            CallFlagsChanged();
        }

        public void SetDebugWorldOverlay(bool value) {
            if (value == DebugWorldOverlay) return;
            DebugWorldOverlay = value;
            CallFlagsChanged();
        }

        public void SetDebugFlags(IEnumerable<string> flags) {
            var newFlags = new HashSet<string>(flags);
            if (newFlags.Intersect(DebugFlags).Count() == newFlags.Count) return;
            DebugFlags.Clear();
            foreach (var flag in newFlags) DebugFlags.Add(flag);
            CallFlagsChanged();
        }

        public void SetDebugFlag(string flag) {
            if (!DebugFlags.Add(flag)) return;
            CallFlagsChanged();
        }

        public void RemoveDebugFlag(string flag) {
            if (!DebugFlags.Remove(flag)) return;
            CallFlagsChanged();
        }

        public void ToggleDebugFlag(string flag) {
            if (DebugFlags.Contains(flag)) RemoveDebugFlag(flag);
            else SetDebugFlag(flag);
        }

        public void RefreshDebugFlags() {
            AvailableDebugFlags.Clear();
            new CollectDebugFlagsEvt { flags = AvailableDebugFlags }.Raise();
        }

        private void SetupLogMessageReceiver() {
            var buffer = LogHistoryBuffer.Instance;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        public void TeardownLogMessageReceiver() {
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type) {
            LogHistoryBuffer.Instance.AddLogMessage(condition, stackTrace, type);
        }

        private void DebugTick() {
            if (DebugGizmos) {
                var cam = debugCamera ?? Camera.main;
                Frustum6 frustum = default;
                if (cam) cam.CalculateFrustum6(ref frustum);
                var now = DateTime.Now;

                var refreshTime = Math.Abs((now - _lastDrawTime).TotalSeconds);

                if (refreshTime > debugRefreshInterval) {
                    _lastDrawTime = now;
                    unchecked { debugFrameCount++; }

                    var isDrawPass = (debugFrameCount + 1) % drawFrequency == 0 && DebugDraw;
                    var isScreenOverlayPass = debugScreenOverlay != null && DebugScreenOverlay &&
                                              debugFrameCount % screenOverlayFrequency == 0;
                    var isWorldOverlayPass = debugWorldOverlay != null && DebugWorldOverlay &&
                                             debugFrameCount % worldOverlayFrequency == 0;

                    if (isDrawPass) {
                        var poly = PolyDrawRenderer.Instance;
                        poly.keepAlive = true;
                        poly.skipRefresh = true;
                        poly.rebuildEveryFrame = false;
                    }

                    new GizmoEvt {
                        type = GizmoType.Runtime,
                        drawer = isDrawPass ? Drawing.Poly() : null,
                        screenOverlay = isScreenOverlayPass ? debugScreenOverlay : null,
                        worldOverlay = isWorldOverlayPass ? debugWorldOverlay : null,
                        HasFrustum = cam,
                        Frustum = frustum
                    }.RaiseSafe();

                    if (isDrawPass) PolyDrawRenderer.InstanceOrNull?.Tick();
                    if (isScreenOverlayPass) debugScreenOverlay.Tick();
                    if (isWorldOverlayPass) {
                        debugWorldOverlay.SetCamera(cam);
                        debugWorldOverlay.Tick();
                    }
                }
            }

            if (!DebugDraw || !DebugGizmos) {
                var poly = PolyDrawRenderer.InstanceOrNull;
                if (!poly) return;
                poly.keepAlive = false;
                poly.skipRefresh = false;
                poly.rebuildEveryFrame = true;
            }
        }

    }

    public class DebugConfig {

        public const string Key = "debugging";

        public HashSet<string> flags;

        public void Load() {
            var globals = Globals.Instance;
            if (globals) { flags = globals.DebugFlags; }
        }

        public void Apply() {
            var globals = Globals.Instance;
            if (globals) { globals.SetDebugFlags(flags); }
        }

        public static CborObject Encode(DebugConfig config) {
            return new CborObject {
                ["flags"] = config.flags.ToCbor(x => x.ToCbor())
            };
        }

        public static DebugConfig Decode(CborObject cbor) {
            var reader = new DataReader(cbor);
            var flags = new HashSet<string>();
            reader.MemberOptional("flags")?.Collection(flags, v => v.Value<string>());

            var config = new DebugConfig {
                flags = flags
            };
            return config;
        }

    }
}