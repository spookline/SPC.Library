using System;
using System.Collections.Generic;
using System.Linq;
using Spookline.SPC.Geometry;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.Cleaver.Editor {
    [Overlay(typeof(SceneView), "Cleaver Volumes", true)]
    internal sealed class CleaverSectionVolumeOverlay : Overlay, ITransientOverlay {

        private Vector2 _scroll;

        public bool visible => ToolManager.activeToolType == typeof(CleaverSectionVolumeTool);

        public override VisualElement CreatePanelContent() {
            var container = new IMGUIContainer(DrawOverlayGUI);
            container.style.minWidth = 340f;
            return container;
        }

        private void DrawOverlayGUI() {
            var section = CleaverSectionOverlayState.GetSelectedSection();
            if (section == null) {
                EditorGUILayout.LabelField("Select a CleaverSection");
                return;
            }

            if (section.volumes == null) section.volumes = Array.Empty<OrientedBox>();

            var selectedIndices = CleaverSectionOverlayState.GetSelectedIndices(section);
            var index = CleaverSectionOverlayState.GetLastEditedIndex(section);

            // If multiple volumes are selected
            if (selectedIndices.Count > 1) {
                var indicesStr = string.Join(", ", selectedIndices);
                EditorGUILayout.LabelField($"Volumes #{indicesStr}", EditorStyles.boldLabel);

                EditorGUILayout.Space();
                if (GUILayout.Button("Merge Volumes")) {
                    Undo.RecordObject(section, "Merge Cleaver Volumes");
                    var list = new List<OrientedBox>(section.volumes);

                    // Use the first selected volume as the base
                    var firstIdx = selectedIndices[0];
                    var mergedVolume = list[firstIdx];

                    // Encapsulate all other selected volumes into the base
                    for (var i = 1; i < selectedIndices.Count; i++)
                        mergedVolume = mergedVolume.Encapsulate(list[selectedIndices[i]]);

                    // Update the base volume
                    list[firstIdx] = mergedVolume;

                    // Remove all other volumes in reverse order to avoid index shifting
                    foreach (var idx in selectedIndices.Skip(1).OrderByDescending(x => x)) list.RemoveAt(idx);

                    section.volumes = list.ToArray();
                    CleaverSectionOverlayState.SetLastEditedIndex(section, firstIdx);
                    CleaverSectionOverlayState.ClearSelectedIndices(section);
                    CleaverSectionOverlayState.MarkDirty(section);
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Remove Volumes")) {
                    Undo.RecordObject(section, "Remove Cleaver Volumes");
                    var list = new List<OrientedBox>(section.volumes);
                    // Remove in reverse order to avoid index shifting
                    foreach (var idx in selectedIndices.OrderByDescending(x => x)) list.RemoveAt(idx);

                    section.volumes = list.ToArray();
                    CleaverSectionOverlayState.ClearSelectedIndices(section);
                    CleaverSectionOverlayState.ClearLastEditedIndex(section);
                    CleaverSectionOverlayState.MarkDirty(section);
                }

                if (GUILayout.Button("Deselect All")) CleaverSectionOverlayState.ClearSelectedIndices(section);

                EditorGUILayout.EndHorizontal();

                return;
            }

            // Single volume or no selection - show field editor
            if (section.volumes.Length == 0 || index < 0 || index >= section.volumes.Length)
                EditorGUILayout.LabelField("No volume selected", EditorStyles.boldLabel);
            else {
                var v = section.volumes[index];
                EditorGUILayout.LabelField($"Volume #{index}", EditorStyles.boldLabel);

                var center = new Vector3(v.center.x, v.center.y, v.center.z);
                var volumeSize = new Vector3(v.Size.x, v.Size.y, v.Size.z);
                var rotQuat = new Quaternion(
                    v.rotation.value.x,
                    v.rotation.value.y,
                    v.rotation.value.z,
                    v.rotation.value.w
                );
                var euler = rotQuat.eulerAngles;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Center (local)");
                center = EditorGUILayout.Vector3Field(GUIContent.none, center);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Size (local)");
                volumeSize = EditorGUILayout.Vector3Field(GUIContent.none, volumeSize);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Rotation (local, Euler)");
                euler = EditorGUILayout.Vector3Field(GUIContent.none, euler);
                EditorGUILayout.EndHorizontal();
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(section, "Modify Cleaver Volume");
                    var newRot = Quaternion.Euler(euler);
                    section.volumes[index] = new OrientedBox(
                        new float3(center.x, center.y, center.z),
                        new float3(volumeSize.x, volumeSize.y, volumeSize.z),
                        new quaternion(newRot.x, newRot.y, newRot.z, newRot.w)
                    );
                    CleaverSectionOverlayState.SetLastEditedIndex(section, index);
                    CleaverSectionOverlayState.MarkDirty(section);
                }

                if (GUILayout.Button("Convert Bounds")) {
                    Undo.RecordObject(section, "Convert Cleaver Volume to AABB");
                    section.volumes[index] = v.AABB();
                }

                if (GUILayout.Button("Remove Volume")) {
                    Undo.RecordObject(section, "Remove Cleaver Volume");
                    var list = new List<OrientedBox>(section.volumes);
                    list.RemoveAt(index);
                    section.volumes = list.ToArray();
                    if (section.volumes.Length == 0)
                        CleaverSectionOverlayState.ClearLastEditedIndex(section);
                    else {
                        CleaverSectionOverlayState.SetLastEditedIndex(
                            section,
                            Mathf.Clamp(index, 0, section.volumes.Length - 1)
                        );
                    }

                    CleaverSectionOverlayState.MarkDirty(section);
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Volume")) {
                Undo.RecordObject(section, "Add Cleaver Volume");
                var list = new List<OrientedBox>(section.volumes) {
                    new(new float3(0f, 0.5f, 0f), new float3(1f, 1f, 1f), new quaternion(0f, 0f, 0f, 1f))
                };
                section.volumes = list.ToArray();
                CleaverSectionOverlayState.SetLastEditedIndex(section, section.volumes.Length - 1);
                CleaverSectionOverlayState.MarkDirty(section);
            }

            if (GUILayout.Button("Autodetect")) {
                if (EditorUtility.DisplayDialog(
                        "Autodetect Volumes",
                        "Autodetect volumes from mesh?",
                        "Reset & Autodetect",
                        "Cancel"
                    )) {
                    Undo.RecordObject(section, "Autodetect Volumes");
                    section.LoadVolumesFromMeshes();
                    CleaverSectionOverlayState.ClearLastEditedIndex(section);
                    CleaverSectionOverlayState.ClearSelectedIndices(section);
                    CleaverSectionOverlayState.MarkDirty(section);
                }
            }

            if (GUILayout.Button("Reset Volumes")) {
                if (EditorUtility.DisplayDialog("Clear Volumes", "Reset volumes to empty?", "Reset", "Cancel")) {
                    Undo.RecordObject(section, "Clear Volumes");
                    section.volumes = Array.Empty<OrientedBox>();
                    CleaverSectionOverlayState.ClearLastEditedIndex(section);
                    CleaverSectionOverlayState.ClearSelectedIndices(section);
                    CleaverSectionOverlayState.MarkDirty(section);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

    }
}