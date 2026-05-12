using Spookline.SPC.Geometry;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Spookline.SPC.Cleaver.Editor {
    [EditorTool("Cleaver Volume Tool", typeof(CleaverSection))]
    public sealed class CleaverSectionVolumeTool : EditorTool {

        private readonly BoxBoundsHandle _boundsHandle = new();

        public override GUIContent toolbarIcon =>
            new(EditorGUIUtility.IconContent("EditCollider").image, "Edit Cleaver Section Volumes");


        public override void OnToolGUI(EditorWindow window) {
            var section = Selection.activeTransform
                ? Selection.activeTransform.GetComponentInParent<CleaverSection>()
                : null;
            if (section == null || section.volumes == null) return;

            var vt = VirtualTransform.From(section.transform);
            var selectedIndex = CleaverSectionOverlayState.GetLastEditedIndex(section);
            var selectedIndices = CleaverSectionOverlayState.GetSelectedIndices(section);

            for (var i = 0; i < section.volumes.Length; i++) {
                var local = section.volumes[i];
                var world = vt.Transform(local);

                var worldCenter = new Vector3(world.center.x, world.center.y, world.center.z);
                var worldSize = new Vector3(world.Size.x, world.Size.y, world.Size.z);
                var worldRot = new Quaternion(
                    world.rotation.value.x,
                    world.rotation.value.y,
                    world.rotation.value.z,
                    world.rotation.value.w
                );

                // Draw volume index label with click detection
                Handles.color = i == selectedIndex ? Color.yellow : Color.white;
                var labelOffset = new Vector3(0f, 1f, 0f);
                var labelWorldPos = worldCenter + labelOffset;

                // Draw in screen space to avoid overlap
                Handles.BeginGUI();
                var screenPos = HandleUtility.WorldToGUIPoint(labelWorldPos);
                var bgWidth = 32f;
                var bgHeight = 16f;
                var bgStartX = screenPos.x - bgWidth / 2f; // Center horizontally
                var bgRect = new Rect(bgStartX, screenPos.y, bgWidth, bgHeight);
                var buttonRect = new Rect(bgStartX, screenPos.y, 32f, bgHeight);

                // Draw black background
                GUI.backgroundColor = Color.black;
                GUI.Box(bgRect, "");
                GUI.backgroundColor = Color.white;

                // Determine button color
                Color buttonColor;
                if (selectedIndices.Contains(i)) {
                    // Multi-selected: slightly highlighted (not full yellow)
                    buttonColor = new Color(0.7f, 0.7f, 0.3f, 1f); // Olive/tan color
                } else if (i == selectedIndex) {
                    // Single selection: fully highlighted
                    buttonColor = Color.yellow;
                } else
                    buttonColor = Color.gray;

                // Draw clickable button
                GUI.backgroundColor = buttonColor;
                if (GUI.Button(buttonRect, i.ToString())) {
                    if (Event.current.shift) {
                        if (selectedIndex != i && !selectedIndices.Contains(selectedIndex)) {
                            selectedIndices.Add(selectedIndex);
                            CleaverSectionOverlayState.ClearLastEditedIndex(section);
                        }

                        // Multi-selection with shift
                        if (selectedIndices.Contains(i))
                            selectedIndices.Remove(i);
                        else
                            selectedIndices.Add(i);
                        CleaverSectionOverlayState.SetSelectedIndices(section, selectedIndices);
                    } else {
                        // Single selection
                        selectedIndices.Clear();
                        CleaverSectionOverlayState.ClearSelectedIndices(section);
                        CleaverSectionOverlayState.SetLastEditedIndex(section, i);
                    }
                }

                GUI.backgroundColor = Color.white;
                Handles.EndGUI();

                if (i != selectedIndex) continue;

                using (new Handles.DrawingScope(Matrix4x4.TRS(worldCenter, worldRot, Vector3.one))) {
                    _boundsHandle.center = Vector3.zero;
                    _boundsHandle.size = worldSize;

                    Handles.color = Color.cyan;
                    EditorGUI.BeginChangeCheck();
                    _boundsHandle.DrawHandle();
                    var boundsChanged = EditorGUI.EndChangeCheck();

                    if (boundsChanged) {
                        Undo.RecordObject(section, "Edit Cleaver Volume");

                        var newWorldCenter = Matrix4x4.TRS(worldCenter, worldRot, Vector3.one)
                            .MultiplyPoint3x4(_boundsHandle.center);
                        var newWorldSize = _boundsHandle.size;
                        var newWorldRotation = worldRot;

                        var newWorldBox = new OrientedBox(
                            new float3(newWorldCenter.x, newWorldCenter.y, newWorldCenter.z),
                            new float3(newWorldSize.x, newWorldSize.y, newWorldSize.z),
                            new quaternion(
                                newWorldRotation.x,
                                newWorldRotation.y,
                                newWorldRotation.z,
                                newWorldRotation.w
                            )
                        );

                        section.volumes[i] = vt.InverseTransform(newWorldBox);
                        CleaverSectionOverlayState.SetLastEditedIndex(section, i);
                        EditorUtility.SetDirty(section);
                        EditorSceneManager.MarkSceneDirty(section.gameObject.scene);
                    }
                }

                // Then draw a single transform handle at the box center for moving/rotating the whole box
                EditorGUI.BeginChangeCheck();
                var newCenter = worldCenter;
                var newRot = worldRot;
                var newSize = worldSize;
                Handles.TransformHandle(ref newCenter, ref newRot, ref newSize);
                var transformChanged = EditorGUI.EndChangeCheck();

                if (transformChanged) {
                    Undo.RecordObject(section, "Edit Cleaver Volume");
                    var newWorldBox = new OrientedBox(
                        new float3(newCenter.x, newCenter.y, newCenter.z),
                        new float3(newSize.x, newSize.y, newSize.z),
                        new quaternion(newRot.x, newRot.y, newRot.z, newRot.w)
                    );
                    section.volumes[i] = vt.InverseTransform(newWorldBox);
                    CleaverSectionOverlayState.SetLastEditedIndex(section, i);
                    EditorUtility.SetDirty(section);
                    EditorSceneManager.MarkSceneDirty(section.gameObject.scene);
                }
            }
        }

    }
}