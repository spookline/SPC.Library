using Unity.Mathematics;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Spookline.SPC.Cleaver.Editor {
    [EditorTool("Cleaver Sections", typeof(CleaverEnvironment))]
    public sealed class CleaverEnvironmentSectionsDebugTool : EditorTool {

        public override GUIContent toolbarIcon =>
            new(EditorGUIUtility.IconContent("d_LightmapParameters Icon").image, "Debug Cleaver Sections");

        private static CleaverSectionsDebugState DebugState => CleaverSectionsDebugState.Instance;

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

            // Draw sections
            if (DebugState.showSections && environment.sections.IsCreated) DrawSections(environment, frustumPlanes);

            // Draw portals
            if (DebugState.showPortals && environment.portals.IsCreated) DrawPortals(environment, frustumPlanes);
        }

        private static void DrawSections(CleaverEnvironment environment, Plane[] frustumPlanes) {
            var sections = environment.sections;
            var volumes = environment.volumes;

            for (var i = 0; i < sections.Length; i++) {
                // Only hide if something is selected and this isn't it
                if (DebugState.selectedSection >= 0 && DebugState.selectedSection != i) continue;
                var section = sections[i];

                // Frustum culling
                var bounds = new Bounds(section.bounds.Center, section.bounds.Extents);
                if (frustumPlanes != null && !GeometryUtility.TestPlanesAABB(frustumPlanes, bounds)) continue;

                // Draw wireframe
                var color = DebugState.selectedSection == i ? Color.magenta : new Color(0.7f, 0, 0.7f);
                Handles.color = color;
                Handles.DrawWireCube(section.bounds.Center, section.bounds.Extents + new float3(0.01f, 0.01f, 0.01f));

                // Draw volumes if selected
                if (DebugState.selectedSection == i && section.volumeCount > 0) {
                    for (var j = 0; j < section.volumeCount; j++) {
                        var volumeIdx = section.volumeIndex + j;
                        if (volumeIdx >= volumes.Length) break;
                        var volume = volumes[volumeIdx];
                        var volBox = volume.query;
                        CleaverEditorHelpers.DrawObbWithLabel(volBox, $"V{j}", Color.yellow);
                    }
                }

                // Draw label
                CleaverEditorHelpers.DrawLabel($"S{i}", section.bounds.Center + new float3(0, 1, 0), Color.hotPink);

                // Draw region reference
                if (section.regionIndex >= 0 && section.regionIndex < environment.proxyGroups.Length) {
                    var regionData = environment.proxyGroups[section.regionIndex];
                    Handles.color = new Color(1, 0.5f, 1, 0.3f);
                    Handles.DrawLine(section.bounds.Center, regionData.bounds.Center);
                }
            }
        }

        private static void DrawPortals(CleaverEnvironment environment, Plane[] frustumPlanes) {
            var portals = environment.portals;
            var sections = environment.sections;

            for (var i = 0; i < portals.Length; i++) {
                var portal = portals[i];

                if (portal.fromIndex < 0 || portal.fromIndex >= sections.Length) continue;
                if (portal.toIndex < 0 || portal.toIndex >= sections.Length) continue;

                var fromSection = sections[portal.fromIndex];
                var toSection = sections[portal.toIndex];

                // Frustum culling - check if either section center is visible
                if (frustumPlanes != null) {
                    var fromBounds = new Bounds(fromSection.bounds.Center, Vector3.one * 0.1f);
                    var toBounds = new Bounds(toSection.bounds.Center, Vector3.one * 0.1f);
                    if (!GeometryUtility.TestPlanesAABB(frustumPlanes, fromBounds) &&
                        !GeometryUtility.TestPlanesAABB(frustumPlanes, toBounds))
                        continue;
                }

                // Draw portal line
                var portalColor = portal.open ? Color.green : Color.red;
                Handles.color = portalColor;
                Handles.DrawLine(fromSection.bounds.Center, toSection.bounds.Center);
            }
        }

    }
}