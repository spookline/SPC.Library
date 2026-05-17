using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Spookline.SPC.Debugging;
using Spookline.SPC.Events;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spookline.SPC.Draw {
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class PolyDrawRenderer : PolyDrawCommandRenderer {

        private const string _objectName = "[SPC] PolyDraw Renderer";

        private struct TimedCommand {

            public PolyDrawCommand command;
            public float remaining;

        }

        private struct TimedLine {

            public float3 a;
            public float3 b;
            public float4 color;
            public float remaining;

        }

        private static PolyDrawRenderer _instance;

        private readonly List<TimedCommand> _timedCommands = new(128);
        private readonly List<TimedLine> _timedLines = new(256);
        private bool _isDirty;

        public bool keepAlive = false;
        public bool autoRebuild = true;

#if UNITY_EDITOR
        private double _lastEditorTime;
#endif

        public static PolyDrawRenderer Instance {
            get {
                if (!_instance) _instance = FindExistingInstance();
                if (!_instance) _instance = CreateInstance();
                return _instance;
            }
        }

        public static PolyDrawRenderer InstanceOrNull {
            get {
                if (!_instance) _instance = FindExistingInstance();
                return _instance;
            }
        }

        [ShowInInspector]
        public int TimedCommandCount => _timedCommands.Count;

        [ShowInInspector]
        public int TimedLineCount => _timedLines.Count;

        [ShowInInspector]
        private bool HasTimedEntries => _timedCommands.Count > 0 || _timedLines.Count > 0;

        public void AddCommand(PolyDrawCommand command, float duration) {
            _timedCommands.Add(
                new TimedCommand {
                    command = command,
                    remaining = duration
                }
            );

            _isDirty = true;
            RequestEditorRepaint();
        }

        public void AddLine(float3 a, float3 b, float4 color, float duration) {
            _timedLines.Add(
                new TimedLine {
                    a = a,
                    b = b,
                    color = color,
                    remaining = duration
                }
            );

            _isDirty = true;
            RequestEditorRepaint();
        }

        public void AddLines(
            ReadOnlySpan<Vector3> points,
            float4 color,
            float duration,
            Matrix4x4 matrix = default,
            bool useMatrix = false
        ) {
            for (var i = 0; i < points.Length - 1; i += 2) {
                var a = points[i];
                var b = points[i + 1];
                if (useMatrix) {
                    a = matrix.MultiplyPoint(a);
                    b = matrix.MultiplyPoint(b);
                }

                _timedLines.Add(
                    new TimedLine {
                        a = a,
                        b = b,
                        color = color,
                        remaining = duration
                    }
                );
            }

            _isDirty = true;
            RequestEditorRepaint();
        }

        public void AddStrip(
            ReadOnlySpan<Vector3> points,
            float4 color,
            bool closed,
            float duration,
            Matrix4x4 matrix = default,
            bool useMatrix = false
        ) {
            for (var i = 0; i < points.Length - 1; i++) {
                var a = points[i];
                var b = points[i + 1];
                if (useMatrix) {
                    a = matrix.MultiplyPoint(a);
                    b = matrix.MultiplyPoint(b);
                }

                _timedLines.Add(
                    new TimedLine {
                        a = a,
                        b = b,
                        color = color,
                        remaining = duration
                    }
                );
            }

            if (closed) {
                var last = points[^1];
                var first = points[0];
                if (useMatrix) {
                    last = matrix.MultiplyPoint(last);
                    first = matrix.MultiplyPoint(first);
                }

                _timedLines.Add(
                    new TimedLine {
                        a = last,
                        b = first,
                        color = color,
                        remaining = duration
                    }
                );
            }

            _isDirty = true;
            RequestEditorRepaint();
        }

        public void ClearTimed() {
            _timedCommands.Clear();
            _timedLines.Clear();
            _isDirty = true;

            RebuildCommandsAndMesh();
            RequestEditorRepaint();
        }

        protected override void OnEnable() {
            base.OnEnable();

            if (!_instance) _instance = this;

            rebuildEveryFrame = true;
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            hideFlags = HideFlags.HideAndDontSave;

            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            RegisterEditorPump();
        }

        protected override void OnDisable() {
            if (_instance == this) _instance = null;

            UnregisterEditorPump();

            base.OnDisable();
        }

        protected override void OnDestroy() {
            if (_instance == this) _instance = null;

            UnregisterEditorPump();

            base.OnDestroy();
        }

        protected override void Update() {
            if (Application.isPlaying && rebuildEveryFrame) Tick();
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmos() { }

        protected override void OnValidate() { }
#endif

        protected override void BuildCommands(
            ref PolyDrawCommandWriter writer,
            ref PolyDrawLineWriter lines,
            bool isOffscreenFrame
        ) {
            var deltaTime = GetDeltaTime();

            CompactCommands(ref writer, deltaTime);
            CompactLines(ref lines, deltaTime);

            if (!isOffscreenFrame) {
                _isDirty = false;
            }
        }

        public void Tick() {
            var hadEntriesBeforeBuild = HasTimedEntries;

            if (_isDirty || hadEntriesBeforeBuild) RebuildCommandsAndMesh();

            if (HasTimedEntries) return;

            if (!hadEntriesBeforeBuild && !_isDirty) {
                if (!keepAlive) DestroyHost();
                return;
            }
        }

        private void CompactCommands(ref PolyDrawCommandWriter writer, float deltaTime) {
            var writeIndex = 0;

            for (var readIndex = 0; readIndex < _timedCommands.Count; readIndex++) {
                var entry = _timedCommands[readIndex];
                writer.Add(entry.command);
                entry.remaining -= deltaTime;
                if (entry.remaining >= 0f) _timedCommands[writeIndex++] = entry;
            }

            if (writeIndex >= _timedCommands.Count) return;
            _timedCommands.RemoveRange(writeIndex, _timedCommands.Count - writeIndex);
            _isDirty = true;
        }

        private void CompactLines(ref PolyDrawLineWriter lines, float deltaTime) {
            var writeIndex = 0;

            for (var readIndex = 0; readIndex < _timedLines.Count; readIndex++) {
                var entry = _timedLines[readIndex];
                lines.Line(entry.a, entry.b, entry.color);
                entry.remaining -= deltaTime;
                if (entry.remaining >= 0f) _timedLines[writeIndex++] = entry;
            }

            if (writeIndex >= _timedLines.Count) return;
            _timedLines.RemoveRange(writeIndex, _timedLines.Count - writeIndex);
            _isDirty = true;
        }

        private float GetDeltaTime() {
            if (Application.isPlaying)
                return Time.deltaTime;

#if UNITY_EDITOR
            var now = EditorApplication.timeSinceStartup;

            if (_lastEditorTime <= 0d) {
                _lastEditorTime = now;
                return 0f;
            }

            var delta = now - _lastEditorTime;
            _lastEditorTime = now;

            return Mathf.Clamp((float)delta, 0f, 0.25f);
#else
            return Time.deltaTime;
#endif
        }

        private static PolyDrawRenderer FindExistingInstance() {
            var instances = Resources.FindObjectsOfTypeAll<PolyDrawRenderer>();

            for (var i = 0; i < instances.Length; i++) {
                var candidate = instances[i];
                if (!candidate) continue;

#if UNITY_EDITOR
                if (EditorUtility.IsPersistent(candidate)) continue;
#endif

                return candidate;
            }

            return null;
        }

        private static PolyDrawRenderer CreateInstance() {
            var go = new GameObject(_objectName) {
                hideFlags = HideFlags.HideAndDontSave
            };

            var renderer = go.AddComponent<PolyDrawRenderer>();
            renderer.hideFlags = HideFlags.HideAndDontSave;

            if (Application.isPlaying) DontDestroyOnLoad(go);

            return renderer;
        }

        private void DestroyHost() {
            if (_instance == this) _instance = null;

            DestroyUnityObject(gameObject);
        }

        private static void DestroyUnityObject(Object obj) {
            if (obj == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(obj);
            else Destroy(obj);
#else
            Destroy(obj);
#endif
        }

        private static void RequestEditorRepaint() {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                SceneView.RepaintAll();
                EditorApplication.QueuePlayerLoopUpdate();
            }
#endif
        }

#if UNITY_EDITOR

        private static bool _editorPumpRegistered;

        [InitializeOnLoadMethod]
        private static void InitializeEditorCleanup() {
            AssemblyReloadEvents.beforeAssemblyReload -= DestroyAllEditorInstances;
            AssemblyReloadEvents.beforeAssemblyReload += DestroyAllEditorInstances;

            EditorApplication.quitting -= DestroyAllEditorInstances;
            EditorApplication.quitting += DestroyAllEditorInstances;

            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;

            RegisterEditorPumpStatic();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.ExitingEditMode ||
                state == PlayModeStateChange.ExitingPlayMode) { DestroyAllEditorInstances(); }
        }

        private static void RegisterEditorPump() {
            RegisterEditorPumpStatic();
        }

        private static void RegisterEditorPumpStatic() {
            if (_editorPumpRegistered)
                return;

            EditorApplication.update += EditorPump;
            _editorPumpRegistered = true;
        }

        private static void UnregisterEditorPump() { }

        private static void EditorPump() {
            if (Application.isPlaying) return;

            var activeInstance = InstanceOrNull;
            if (!activeInstance) return;

            activeInstance.Tick();

            if (activeInstance && activeInstance.HasTimedEntries) SceneView.RepaintAll();
        }

        private static void DestroyAllEditorInstances() {
            var instances = Resources.FindObjectsOfTypeAll<PolyDrawRenderer>();

            for (var i = 0; i < instances.Length; i++) {
                var renderer = instances[i];

                if (renderer == null)
                    continue;

                DestroyImmediate(renderer.gameObject, false);
            }

            _instance = null;
        }

#else
        private static void RegisterEditorPump() {}
        private static void UnregisterEditorPump() {}

#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() {
            _instance = null;
        }

    }
}