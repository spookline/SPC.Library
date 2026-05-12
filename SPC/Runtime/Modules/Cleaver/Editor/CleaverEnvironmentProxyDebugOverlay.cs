using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.Cleaver.Editor {
    [Overlay(typeof(SceneView), "Cleaver Proxies", true)]
    internal sealed class CleaverEnvironmentProxyDebugOverlay : Overlay, ITransientOverlay {

        private CleaverRegionsDebugState DebugState => CleaverRegionsDebugState.Instance;

        public bool visible => ToolManager.activeToolType == typeof(CleaverEnvironmentProxyDebugTool);

        public override VisualElement CreatePanelContent() {
            var container = new IMGUIContainer(DrawOverlayGUI);
            container.style.minWidth = 300f;
            return container;
        }

        private void DrawOverlayGUI() {
            var environment = Selection.activeTransform
                ? Selection.activeTransform.GetComponentInParent<CleaverEnvironment>()
                : null;

            if (environment == null) {
                EditorGUILayout.LabelField("Select a CleaverEnvironment", EditorStyles.boldLabel);
                return;
            }

            // Baked data info
            var proxyGroupCount = environment.proxyGroups.IsCreated ? environment.proxyGroups.Length : 0;
            var proxyCount = environment.proxies.IsCreated ? environment.proxies.Length : 0;
            var sampleCount = environment.samplePoints.IsCreated ? environment.samplePoints.Length : 0;
            EditorGUILayout.LabelField($"Groups: {proxyGroupCount} Proxies: {proxyCount} Sample Points: {sampleCount}");
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Display Options", EditorStyles.boldLabel);
            // Toggle visibility options
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            DebugState.showGroups = GUILayout.Toggle(DebugState.showGroups, "Groups", "Button");
            DebugState.showProxies = GUILayout.Toggle(DebugState.showProxies, "Proxies", "Button");
            DebugState.showSamplePoints = GUILayout.Toggle(DebugState.showSamplePoints, "Sample Points", "Button");
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);

            // Selection management
            if (environment.proxyGroups is { IsCreated: true, Length: > 0 }) {
                EditorGUI.BeginChangeCheck();
                DebugState.selectedGroup = EditorGUILayout.IntSlider(
                    "Selected Group",
                    DebugState.selectedGroup,
                    -1,
                    environment.proxyGroups.Length - 1
                );
                if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();

                if (DebugState.selectedGroup >= 0) DisplayRegionInfo(environment, DebugState.selectedGroup);
            } else
                DebugState.selectedGroup = -1;

            if (environment.proxies is { IsCreated: true, Length: > 0 }) {
                EditorGUI.BeginChangeCheck();
                DebugState.selectedProxy = EditorGUILayout.IntSlider(
                    "Selected Proxy",
                    DebugState.selectedProxy,
                    -1,
                    environment.proxies.Length - 1
                );
                if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();

                if (DebugState.selectedProxy >= 0) DisplayProxyInfo(environment, DebugState.selectedProxy);
            } else
                DebugState.selectedProxy = -1;
        }

        private void DisplayRegionInfo(CleaverEnvironment environment, int groupIndex) {
            var region = environment.proxyGroups[groupIndex];
            EditorGUILayout.LabelField(
                $"Group #{groupIndex} Proxies: {region.proxyIndex}[{region.proxyCount}] Parent: {region.parentIndex} Mask: {region.mask}",
                EditorStyles.miniLabel
            );
        }

        private void DisplayProxyInfo(CleaverEnvironment environment, int proxyIndex) {
            var proxy = environment.proxies[proxyIndex];
            EditorGUILayout.LabelField(
                $"Proxy #{proxyIndex} Points: {proxy.pointIndex}[{proxy.pointCount}] Radius: {proxy.radius}",
                EditorStyles.miniLabel
            );
        }

    }
}