using System;
using System.Linq;
using System.Reflection;
using Sirenix.Utilities;
using Spookline.SPC.Ext;
using Spookline.SPC.Geometry;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assemblies;

namespace Spookline.SPC.Editor {
    [InitializeOnLoad]
    public class AutoHideSceneIcons {

        static AutoHideSceneIcons() {
            foreach (var assembly in CurrentAssemblies.GetLoadedAssemblies()) {
                foreach (var type in assembly.SafeGetTypes()) {
                    var interfaces = type.GetInterfaces();
                    if (interfaces.Contains(typeof(IHideScriptSceneIcon))) {
                        SetGizmoIconEnabled(type, false);
                    }
                }
            }
        }

        private static MethodInfo _setIconEnabled;

        private static MethodInfo SetIconEnabled =>
            _setIconEnabled ??= Assembly.GetAssembly(typeof(UnityEditor.Editor))
                ?.GetType("UnityEditor.AnnotationUtility")
                ?.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic);

        private static void SetGizmoIconEnabled(Type type, bool on) {
            const int monoBehaviorClassID = 114; // https://docs.unity3d.com/Manual/ClassIDReference.html
            if (SetIconEnabled == null) return;
            try {
                SetIconEnabled.Invoke(null, new object[] { monoBehaviorClassID, type.Name, on ? 1 : 0 });
                return;
            } catch (Exception) {
                /* Ignore */
            }

            try {
                SetIconEnabled.Invoke(null, new object[] { monoBehaviorClassID, type.FullName, on ? 0 : 1 }); //
            } catch (Exception e) {
                Debug.LogError($"Failed to set gizmo icon disabled for type {type.FullName}: {e}"); //
            }
        }

    }
}