using System.Collections.Generic;
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

        public static int SelectedPointIndex { get; private set; } = -1;

        [SerializeField]
        private int _selectedPointIndex = -1;

        public override GUIContent toolbarIcon =>
            new(EditorGUIUtility.IconContent("d_PreMatSphere").image, "Edit Cleaver Points");

        public override void OnToolGUI(EditorWindow window) {
            SelectedPointIndex = _selectedPointIndex;
            var section = Selection.activeTransform
                ? Selection.activeTransform.GetComponentInParent<CleaverSection>()
                : null;
            if (section?.points == null || section.points.Count == 0) return;

            var sceneView = window as SceneView;
            if (sceneView == null) return;

            var virtualTransform = VirtualTransform.From(section.transform);
            var affine = new AffineTransform(
                virtualTransform.position,
                virtualTransform.rotation,
                virtualTransform.scale
            );

            // Perform selection
            for (var i = 0; i < section.points.Count; i++) {
                var point = section.points[i];
                if (DrawPointLabel(affine, point, i, i == _selectedPointIndex)) {
                    Undo.IncrementCurrentGroup();
                    Undo.SetCurrentGroupName($"Select Cleaver Point {i}");
                    Undo.RecordObject(this, $"Select Cleaver Point {i}");
                    _selectedPointIndex = i;
                    SelectedPointIndex = i;
                    Undo.FlushUndoRecordObjects();
                    break;
                }
            }

            // Draw all points
            for (var i = 0; i < section.points.Count; i++) {
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