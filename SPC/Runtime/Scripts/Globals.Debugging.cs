using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Spookline.SPC.Debugging;
using Spookline.SPC.Draw;
using Spookline.SPC.Events;
using Spookline.SPC.Geometry;
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
            DebugDraw = value;
            CallFlagsChanged();
        }

        public void SetDebugGizmos(bool value) {
            DebugGizmos = value;
            CallFlagsChanged();
        }

        public void SetDebugScreenOverlay(bool value) {
            DebugScreenOverlay = value;
            CallFlagsChanged();
        }

        public void SetDebugWorldOverlay(bool value) {
            DebugWorldOverlay = value;
            CallFlagsChanged();
        }

        public void SetDebugFlags(IEnumerable<string> flags) {
            DebugFlags.Clear();
            foreach (var flag in flags) DebugFlags.Add(flag);
            CallFlagsChanged();
        }

        public void SetDebugFlag(string flag) {
            DebugFlags.Add(flag);
            CallFlagsChanged();
        }

        public void RemoveDebugFlag(string flag) {
            DebugFlags.Remove(flag);
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
                    if (isWorldOverlayPass) debugWorldOverlay.Tick();
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
}