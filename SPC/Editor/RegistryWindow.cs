using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Spookline.SPC.Registry;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Spookline.SPC.Editor {
    public class RegistryWindow : OdinMenuEditorWindow {

        [MenuItem("Window/Asset Management/Spookline Registries")]
        private static void ShowWindow() {
            var window = GetWindow<RegistryWindow>();
            window.titleContent = new GUIContent("Spookline Registries");
            window.Show();
        }

        public static IReadOnlyList<Type> FindAllRegistryTypes() {
            var result = new List<Type>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
                if (asm.IsDynamic) continue;
                foreach (var t in asm.SafeGetTypes()) {
                    try {
                        if (t == null) continue;
                        if (!t.IsClass || t.IsAbstract) continue;
                        if (typeof(IObjectRegistry).IsAssignableFrom(t)) result.Add(t);
                    } catch (TypeLoadException) {
                        // Ignored
                    } catch (ReflectionTypeLoadException) {
                        // Ignored
                    }
                }
            }

            return result.Distinct().OrderBy(t => t.FullName).ToList();
        }

        protected override void DrawEditor(int index) {
            if (index == 0) {
                var edited = GetTargets().FirstOrDefault();
                if (edited is RegistryObject obj) {
                    EditorGUILayout.TextField("Guid", obj.assetGuid);
                    EditorGUILayout.TextField("Name", obj.name);
                    EditorGUILayout.Space();
                }
            }

            base.DrawEditor(index);
        }

        protected override OdinMenuTree BuildMenuTree() {
            var tree = new OdinMenuTree(false, new OdinMenuTreeDrawingConfig { AutoFocusSearchBar = false });

            var registries = FindAllRegistryTypes();
            foreach (var registry in registries) {
                var instance = Activator.CreateInstance(registry) as IObjectRegistry;
                if (instance == null) continue;
                var objType = instance.GetObjectType();
                var objectRegistryEntry = new ObjectRegistryEntry {
                    objectType = objType.FullName,
                    addressableLabel = instance.AddressableLabel,
                    entryCount = 0
                };
                tree.Add(registry.Name, objectRegistryEntry);
                foreach (var entry in GetEntriesWithLabel(instance.AddressableLabel)) {
                    try {
                        var asset = AssetDatabase.LoadAssetAtPath<RegistryObject>(entry.AssetPath);
                        if (!asset) continue;
                        if (asset.GetType() != objType) continue;
                        var item = tree.Add($"{registry.Name}/{asset.name}", asset).LastOrDefault();
                        item!.SearchString = $"{asset.name}, {asset.assetGuid}";
                        item.AddThumbnailIcon(true);
                        objectRegistryEntry.entryCount++;
                    } catch (Exception e) {
                        // ignored
                    }
                }
            }

            return tree;
        }

        private static List<AddressableAssetEntry> GetEntriesWithLabel(string label) {
            var result = new List<AddressableAssetEntry>();

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) {
                Debug.LogWarning("Addressable Asset Settings not found. Make sure you have Addressables configured.");
                return result;
            }

            foreach (var group in settings.groups) {
                if (group == null) continue;
                foreach (var entry in group.entries) {
                    if (entry.labels.Contains(label)) result.Add(entry);
                }
            }

            return result;
        }

    }

    [Serializable]
    public class ObjectRegistryEntry {

        [ReadOnly]
        public string objectType;
        [ReadOnly]
        public string addressableLabel;
        [ReadOnly]
        public int entryCount;

    }
}