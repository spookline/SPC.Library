using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.Cleaver.Editor {
    [Overlay(typeof(SceneView), "Cleaver Sections", true)]
    internal sealed class CleaverEnvironmentSectionsDebugOverlay : Overlay, ITransientOverlay {

        private CleaverSectionsDebugState DebugState => CleaverSectionsDebugState.Instance;
        public bool visible => ToolManager.activeToolType == typeof(CleaverEnvironmentSectionsDebugTool);

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
            var sectionCount = environment.sections.IsCreated ? environment.sections.Length : 0;
            var portalCount = environment.portals.IsCreated ? environment.portals.Length : 0;
            EditorGUILayout.LabelField($"Sections: {sectionCount} Portals: {portalCount}");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Display Options", EditorStyles.boldLabel);

            // Toggle visibility options
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            DebugState.showSections = GUILayout.Toggle(DebugState.showSections, "Sections", "Button");
            DebugState.showPortals = GUILayout.Toggle(DebugState.showPortals, "Portals", "Button");
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);

            // Selection management
            if (environment.sections is { IsCreated: true, Length: > 0 }) {
                EditorGUI.BeginChangeCheck();
                DebugState.selectedSection = EditorGUILayout.IntSlider(
                    "Selected Section",
                    DebugState.selectedSection,
                    -1,
                    environment.sections.Length - 1
                );
                if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();

                if (DebugState.selectedSection >= 0) DisplaySectionInfo(environment, DebugState.selectedSection);
            } else
                DebugState.selectedSection = -1;
        }

        private void DisplaySectionInfo(CleaverEnvironment environment, int sectionIndex) {
            var section = environment.sections[sectionIndex];
            EditorGUILayout.LabelField(
                $"Section #{sectionIndex} Volumes: {section.volumeIndex}[{section.volumeCount}] Portals: {section.portalIndex}[{section.portalCount}]",
                EditorStyles.miniLabel
            );
            EditorGUILayout.LabelField(
                $"Masks: {section.mask} Closed: {section.closed}",
                EditorStyles.miniLabel
            );
        }

    }
}