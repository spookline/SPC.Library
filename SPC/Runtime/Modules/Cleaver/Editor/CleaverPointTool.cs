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

        private int _selectedPointIndex = -1;

        public override GUIContent toolbarIcon =>
            new(EditorGUIUtility.IconContent("d_PreMatSphere").image, "Edit Cleaver Points");

        public override void OnToolGUI(EditorWindow window) {
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

            // Draw all points
            for (var i = 0; i < section.points.Count; i++) {
                var point = section.points[i];
                if (DrawPointLabel(affine, point, i, i == _selectedPointIndex)) { _selectedPointIndex = i; }
                if (i == _selectedPointIndex) { DrawPointHandles(section, affine, point); }
            }
        }

        private static bool DrawPointLabel(AffineTransform transform, EditablePoint point, int index, bool selected) {
            var worldPos = math.transform(transform, point.position);

            Handles.BeginGUI();
            var screenPos = HandleUtility.WorldToGUIPoint(worldPos);
            var bgWidth = 30f;
            var bgHeight = 16f;
            var bgStartX = screenPos.x - bgWidth / 2f;
            var bgRect = new Rect(bgStartX, screenPos.y, bgWidth, bgHeight);
            var buttonRect = new Rect(bgStartX, screenPos.y, 30f, bgHeight);

            GUI.backgroundColor = Color.black;
            GUI.Box(bgRect, "");
            GUI.backgroundColor = selected ? Color.yellow : Color.gray;

            var clicked = GUI.Button(buttonRect, index.ToString());

            GUI.backgroundColor = Color.white;
            Handles.EndGUI();

            return clicked;
        }

        private void DrawPointHandles(CleaverSection section, AffineTransform transform, EditablePoint point) {
            EditorGUI.BeginChangeCheck();

            var draw = Drawing.Handles;
            point.DrawHandles(transform, draw);

            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(section, "Edit Cleaver Point");
                EditorUtility.SetDirty(section);
                EditorSceneManager.MarkSceneDirty(section.gameObject.scene);
            }
        }

    }
}