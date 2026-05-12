using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Spookline.SPC.Cleaver.Editor {
    [EditorTool("Cleaver Proxies", typeof(CleaverEnvironment))]
    public sealed class CleaverEnvironmentProxyDebugTool : EditorTool {

        public override GUIContent toolbarIcon =>
            new(EditorGUIUtility.IconContent("d_ProbeVolume Icon").image, "Cleaver Proxies");

        private static CleaverRegionsDebugState DebugState => CleaverRegionsDebugState.Instance;

        public override void OnToolGUI(EditorWindow window) {
            var environment = Selection.activeTransform
                ? Selection.activeTransform.GetComponentInParent<CleaverEnvironment>()
                : null;
            if (!environment) return;

            DrawEnvironmentDebugGizmos(environment, window as SceneView);
        }

        private static void DrawEnvironmentDebugGizmos(CleaverEnvironment environment, SceneView sceneView) {
            var camera = sceneView?.camera;
            var frustumPlanes = camera != null ? GeometryUtility.CalculateFrustumPlanes(camera) : null;

            // Draw regions
            if (DebugState.showGroups && environment.proxyGroups.IsCreated) DrawGroups(environment, frustumPlanes);

            // Draw proxies and their sample points
            if ((DebugState.showProxies || DebugState.showSamplePoints) && environment.proxies.IsCreated)
                DrawProxies(environment, frustumPlanes);
        }

        private static void DrawGroups(CleaverEnvironment environment, Plane[] frustumPlanes) {
            var groups = environment.proxyGroups;
            for (var i = 0; i < groups.Length; i++) {
                // Only hide if something is selected and this isn't it
                if (DebugState.selectedGroup >= 0 && DebugState.selectedGroup != i) continue;
                var group = groups[i];

                // Frustum culling
                var bounds = new Bounds(group.bounds.Center, group.bounds.Extents);
                if (frustumPlanes != null && !GeometryUtility.TestPlanesAABB(frustumPlanes, bounds)) continue;

                // Draw wireframe
                var color = DebugState.selectedGroup == i ? Color.cyan : new Color(0, 0.7f, 0.7f);
                Handles.color = color;
                Handles.DrawWireCube(group.bounds.Center, group.bounds.Extents);

                // Draw label
                CleaverEditorHelpers.DrawLabel($"R{i}", group.bounds.Center, Color.cyan);

                // Draw parent connection
                if (group.parentIndex >= 0 && group.parentIndex < groups.Length) {
                    var parentRegion = groups[group.parentIndex];
                    Handles.color = new Color(1, 0.5f, 0, 0.5f);
                    Handles.DrawLine(group.bounds.Center, parentRegion.bounds.Center);
                }
            }
        }

        private static void DrawProxies(CleaverEnvironment environment, Plane[] frustumPlanes) {
            var proxies = environment.proxies;
            var samplePoints = environment.samplePoints;

            for (var i = 0; i < proxies.Length; i++) {
                // Only hide if something is selected and this isn't it
                if (DebugState.selectedProxy >= 0 && DebugState.selectedProxy != i) continue;
                var proxy = proxies[i];

                // Frustum culling
                var bounds = new Bounds(proxy.bounds.Center, proxy.bounds.Extents);
                if (frustumPlanes != null && !GeometryUtility.TestPlanesAABB(frustumPlanes, bounds)) continue;

                if (DebugState.showProxies) {
                    // Draw wireframe
                    Handles.color = DebugState.selectedProxy == i ? Color.green : new Color(0, 0.7f, 0);
                    Handles.DrawWireCube(proxy.bounds.Center, proxy.bounds.Extents);

                    // Draw OBB
                    CleaverEditorHelpers.DrawObbWithLabel(proxy.query, $"P{i}", new Color(1, 0.5f, 0, 0.8f));
                }

                // Draw sample points for this proxy
                if (DebugState.showSamplePoints && samplePoints.IsCreated) {
                    for (var j = 0; j < proxy.pointCount; j++) {
                        var pointIdx = proxy.pointIndex + j;
                        if (pointIdx >= samplePoints.Length) break;
                        var point = samplePoints[pointIdx];

                        // Frustum culling for point
                        if (frustumPlanes != null) {
                            var pointBounds = new Bounds(point, Vector3.one * 0.1f);
                            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, pointBounds)) continue;
                        }

                        Handles.color = new Color(1, 0.5f, 0, 0.8f);
                        DrawHandlesWireSphere(point, 0.05f);
                    }
                }
            }
        }

        private static void DrawHandlesWireSphere(Vector3 center, float radius) {
            // Draw three orthogonal circles
            Handles.DrawWireArc(center, Vector3.right, Vector3.up, 360f, radius);
            Handles.DrawWireArc(center, Vector3.up, Vector3.forward, 360f, radius);
            Handles.DrawWireArc(center, Vector3.forward, Vector3.right, 360f, radius);
        }

    }
}