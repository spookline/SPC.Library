using Spookline.SPC.Cleaver.Points;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.Cleaver.Editor {
    [Overlay(typeof(SceneView), "Cleaver Points", true)]
    internal sealed class CleaverPointOverlay : Overlay, ITransientOverlay {

        public bool visible => ToolManager.activeToolType == typeof(CleaverPointTool);

        public override VisualElement CreatePanelContent() {
            var container = new IMGUIContainer(DrawOverlayGUI);
            container.style.minWidth = 300f;
            return container;
        }

        private static int _currentGroup = -1;
        private static int _active = -1;

        private void DrawOverlayGUI() {
            var section = CleaverSectionOverlayState.GetSelectedSection();
            if (section == null) {
                EditorGUILayout.LabelField("Select a CleaverSection");
                return;
            }

            var index = CleaverPointTool.SelectedPointIndex;
            if (section.points == null || index < 0 || index >= section.points.Count) {
                EditorGUILayout.LabelField("No point selected", EditorStyles.boldLabel);
            } else {
                var point = section.points[index];
                var clone = point.Clone();
                clone.editorData = point.editorData;

                EditorGUILayout.LabelField($"{clone.TypeName} #{index}", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                CleaverEditorHelpers.GuardedUndo(
                    section,
                    () => {
                        try {
                            EditablePoint.CurrentSection = section;
                            clone.DrawOverlayGUI();
                        } finally {
                            EditablePoint.CurrentSection = null;
                        }
                    },
                    () => point.CopyFrom(clone),
                    $"Edit Cleaver Point {index} from GUI",
                    ref _active,
                    ref _currentGroup
                );

                EditorGUILayout.Space();
                if (GUILayout.Button("Remove Point")) {
                    CleaverEditorHelpers.BeginFlushedUndo(section, $"Remove Cleaver Point {index}");
                    section.points.RemoveAt(index);
                    CleaverEditorHelpers.EndFlushedUndo(section);
                }
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Add Point")) {
                CleaverEditorHelpers.BeginFlushedUndo(section, $"Add Cleaver Point");
                section.points?.Add(new EditableTransformPoint {
                    position = Vector3.zero
                });
                CleaverEditorHelpers.EndFlushedUndo(section);
            }
        }
    }
}
