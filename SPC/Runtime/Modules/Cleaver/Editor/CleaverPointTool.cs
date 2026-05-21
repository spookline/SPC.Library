using System.Collections.Generic;
using HELIX.Coloring;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Spookline.SPC.Cleaver.Points;
using Spookline.SPC.Draw;
using Spookline.SPC.Geometry;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Spookline.SPC.Cleaver.Editor {
    [EditorTool(
        "Cleaver Points Tool",
        typeof(CleaverSection),
        toolPriority = 2000
    )]
    public sealed class CleaverPointTool : EditorTool {

        public static int SelectedPointIndex {
            get => _selectedPointIndex;
            set => _selectedPointIndex = value;
        }

        [SerializeField]
        private static int _selectedPointIndex = -1;

        public override GUIContent toolbarIcon {
            get {
                return new GUIContent(
                    SdfIcons.CreateTransparentIconTexture(SdfIconType.Record2, Colors.Hex("#ffb13d"), 64, 64, 0),
                    "Edit Cleaver Points"
                );
            }
        }

        public override void OnToolGUI(EditorWindow window) {
            var section = Selection.activeTransform
                ? Selection.activeTransform.GetComponentInParent<CleaverSection>()
                : null;
            if (section?.points == null || section.points.Count == 0) return;

            var sceneView = window as SceneView;
            if (sceneView == null) return;

            var affine = section.transform.Affine();

            // Perform selection
            for (var i = 0; i < section.points.Count; i++) {
                var point = section.points[i];
                if (point == null) continue;

                if (!string.IsNullOrEmpty(EditablePoint.Filter)) {
                    if (!point.TypeName.Contains(EditablePoint.Filter)) continue;
                }

                var isSelected = i == _selectedPointIndex;
                if (!isSelected && CleaverPointOverlay.HideNonSelected) {
                    point.editorHidden = true;
                    continue;
                }
                point.editorHidden = false;

                if (DrawPointLabel(affine, point, i, isSelected)) {
                    _selectedPointIndex = i;
                    break;
                }
            }

            // Draw all points
            if (!CleaverPointOverlay.HideHandles) for (var i = 0; i < section.points.Count; i++) {
                var point = section.points[i];
                if (i == _selectedPointIndex) { DrawPointHandles(section, affine, point); }
            }
        }

        private static bool DrawPointLabel(AffineTransform transform, EditablePoint point, int index, bool selected) {
            var worldPos = math.transform(transform, point.position);

            Handles.BeginGUI();
            var screenPos = HandleUtility.WorldToGUIPoint(worldPos);
            var typeName = point.TypeName;
            var indexStr = index.ToString();

            var labelStyle = EditorStyles.miniLabel;
            var typeNameWidth = labelStyle.CalcSize(new GUIContent(typeName)).x + 4f;

            var bgWidth = 30f + typeNameWidth;
            var bgHeight = 16f;
            var bgStartX = screenPos.x - bgWidth / 2f;
            var bgRect = new Rect(bgStartX, screenPos.y, bgWidth, bgHeight);
            var buttonRect = new Rect(bgStartX, screenPos.y, 30f, bgHeight);
            var labelRect = new Rect(bgStartX + 30f, screenPos.y, typeNameWidth, bgHeight);

            GUI.backgroundColor = Color.black;
            GUI.Box(bgRect, "");
            GUI.backgroundColor = selected ? Color.yellow : Color.gray;

            var clicked = GUI.Button(buttonRect, indexStr);

            GUI.Label(labelRect, typeName, labelStyle);

            GUI.backgroundColor = Color.white;
            Handles.EndGUI();

            return clicked;
        }


        private static int _currentGroup = -1;
        private static int _active = -1;
        private void DrawPointHandles(CleaverSection section, AffineTransform transform, EditablePoint point) {
            point.editorData ??= new Dictionary<string, object>();
            var clone = point.Clone();
            clone.editorData = point.editorData;

            CleaverEditorHelpers.GuardedUndo(
                section,
                () => {
                    try {
                        EditablePoint.CurrentSection = section;
                        clone.DrawHandles(transform);
                    } finally {
                        EditablePoint.CurrentSection = null;
                    }
                },
                () => point.CopyFrom(clone),
                $"Edit Cleaver Point {SelectedPointIndex} from Handles",
                ref _active,
                ref _currentGroup
            );
        }

    }
}