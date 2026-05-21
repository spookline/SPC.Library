using System.Collections.Generic;
using HELIX.Widgets.Diagnostics;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Spookline.SPC.Cleaver.Points;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.Cleaver.Editor {
    [Overlay(typeof(SceneView), "Points", true)]
    internal sealed class CleaverPointOverlay : Overlay, ITransientOverlay {

        public bool visible => ToolManager.activeToolType == typeof(CleaverPointTool);

        public override VisualElement CreatePanelContent() {
            var container = new IMGUIContainer(DrawOverlayGUI);
            container.style.width = 350f;
            return container;
        }

        private static int _currentGroup = -1;
        private static int _active = -1;
        private static int _selectBuffer = 0;

        private static bool _isOptions = false;
        public static bool HideHandles = false;
        public static bool HideNonSelected = false;

        private Texture2D _optionsIcon;

        public Texture2D GetOptionsIcon() {
            if (_optionsIcon) return _optionsIcon;
            _optionsIcon = SdfIcons.CreateTransparentIconTexture(SdfIconType.FunnelFill, Color.white, 15, 15, 0);
            _optionsIcon.hideFlags = HideFlags.HideAndDontSave;
            return _optionsIcon;
        }


        private void DrawOverlayGUI() {
            var section = CleaverSectionOverlayState.GetSelectedSection();
            if (section == null) {
                EditorGUILayout.LabelField("Select a CleaverSection");
                return;
            }

            var index = CleaverPointTool.SelectedPointIndex;
            if (section.points == null || index < 0 || index >= section.points.Count) {
                if (_isOptions) {
                    ViewOptionsGUI();
                    EditorGUILayout.Space();
                } else {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("No point selected", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                _selectBuffer = EditorGUILayout.IntField(GUIContent.none, _selectBuffer, GUILayout.Width(50f));
                if (GUILayout.Button("Select Point") && section.points != null) {
                    if (_selectBuffer >= 0 && _selectBuffer < section.points.Count) {
                        CleaverPointTool.SelectedPointIndex = _selectBuffer;
                    }
                }

                _isOptions = GUILayout.Toggle(
                    _isOptions,
                    new GUIContent(GetOptionsIcon()),
                    "Button"
                );
                EditorGUILayout.EndHorizontal();
            } else {
                var point = section.points[index];
                var clone = point.Clone();
                clone.editorData = point.editorData;

                if (_isOptions) { ViewOptionsGUI(); } else {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"#{index}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(clone.TypeName.ToStringNullable());
                    EditorGUILayout.EndHorizontal();

                    CleaverEditorHelpers.GuardedUndo(
                        section,
                        () => {
                            try {
                                EditablePoint.CurrentSection = section;
                                clone.DrawOverlayGUI();
                            } finally { EditablePoint.CurrentSection = null; }
                        },
                        () => point.CopyFrom(clone),
                        $"Edit Cleaver Point {index} from GUI",
                        ref _active,
                        ref _currentGroup
                    );
                }

                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Remove")) {
                    CleaverEditorHelpers.BeginFlushedUndo(section, $"Remove Cleaver Point {index}");
                    section.points.RemoveAt(index);
                    CleaverEditorHelpers.EndFlushedUndo(section);
                }

                if (GUILayout.Button("Duplicate")) {
                    CleaverEditorHelpers.BeginFlushedUndo(section, $"Duplicate Cleaver Point {index}");
                    section.points.Insert(index, clone);
                    CleaverEditorHelpers.EndFlushedUndo(section);
                    CleaverPointTool.SelectedPointIndex = index + 1;
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Deselect")) { CleaverPointTool.SelectedPointIndex = -1; }
                _isOptions = GUILayout.Toggle(
                    _isOptions,
                    new GUIContent(GetOptionsIcon()),
                    "Button"
                );
                GUILayout.EndHorizontal();
            }
        }

        private void ViewOptionsGUI() {
            EditablePoint.Filter = EditorGUILayout.TextField("Type Filter", EditablePoint.Filter);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Hide");
            EditorGUILayout.Space(0f, false);
            HideHandles = GUILayout.Toggle(HideHandles, "Handles", "Button");
            HideNonSelected = GUILayout.Toggle(HideNonSelected, "Not Selected", "Button");
            EditorGUILayout.EndHorizontal();
        }

    }
}