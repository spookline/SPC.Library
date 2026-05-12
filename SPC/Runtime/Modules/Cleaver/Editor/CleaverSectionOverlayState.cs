using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Spookline.SPC.Cleaver.Editor {
    internal static class CleaverSectionOverlayState {

        private static string PrefKey(CleaverSection section) {
            var id = GlobalObjectId.GetGlobalObjectIdSlow(section).ToString();
            return $"CleaverSection_LastEdited_{id}";
        }

        private static string MultiSelectPrefKey(CleaverSection section) {
            var id = GlobalObjectId.GetGlobalObjectIdSlow(section).ToString();
            return $"CleaverSection_MultiSelect_{id}";
        }

        public static CleaverSection GetSelectedSection() {
            return Selection.activeTransform ? Selection.activeTransform.GetComponentInParent<CleaverSection>() : null;
        }

        public static int GetLastEditedIndex(CleaverSection section) {
            if (section == null || section.volumes == null || section.volumes.Length == 0) return -1;
            var idx = EditorPrefs.GetInt(PrefKey(section), section.volumes.Length - 1);
            return Mathf.Clamp(idx, 0, section.volumes.Length - 1);
        }

        public static void SetLastEditedIndex(CleaverSection section, int index) {
            if (section == null) return;
            EditorPrefs.SetInt(PrefKey(section), index);
        }

        public static void ClearLastEditedIndex(CleaverSection section) {
            if (section == null) return;
            EditorPrefs.DeleteKey(PrefKey(section));
        }

        public static List<int> GetSelectedIndices(CleaverSection section) {
            var selectedSet = new List<int>();
            if (section == null || section.volumes == null) return selectedSet;

            var prefKey = MultiSelectPrefKey(section);
            var selectedString = EditorPrefs.GetString(prefKey, "");

            if (string.IsNullOrEmpty(selectedString)) return selectedSet;

            var indices = selectedString.Split(',');
            foreach (var indexStr in indices) {
                if (int.TryParse(indexStr.Trim(), out var idx)) {
                    // Only add if within bounds
                    if (idx >= 0 && idx < section.volumes.Length) selectedSet.Add(idx);
                }
            }

            return selectedSet;
        }

        public static void SetSelectedIndices(CleaverSection section, List<int> indices) {
            if (section == null) return;

            if (indices == null || indices.Count == 0) {
                EditorPrefs.DeleteKey(MultiSelectPrefKey(section));
                return;
            }

            // Filter out of bounds indices
            var validIndices = new List<int>(indices);
            validIndices.RemoveAll(i => i < 0 || i >= section.volumes.Length);

            if (validIndices.Count == 0) {
                EditorPrefs.DeleteKey(MultiSelectPrefKey(section));
                return;
            }

            var selectedString = string.Join(",", validIndices);
            EditorPrefs.SetString(MultiSelectPrefKey(section), selectedString);
        }

        public static void ClearSelectedIndices(CleaverSection section) {
            if (section == null) return;
            EditorPrefs.DeleteKey(MultiSelectPrefKey(section));
        }

        public static void MarkDirty(CleaverSection section) {
            EditorUtility.SetDirty(section);
            EditorSceneManager.MarkSceneDirty(section.gameObject.scene);
        }

    }
}