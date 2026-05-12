using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Spookline.SPC.Cleaver.Editor {
    [CustomPropertyDrawer(typeof(NavmeshAreaAttribute))]
    public class NavmeshAreaPropertyDrawer : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var areaNames = NavMesh.GetAreaNames();
            property.intValue = EditorGUILayout.Popup(property.displayName, property.intValue, areaNames);
        }

    }

    [CustomPropertyDrawer(typeof(NavmeshAgentTypeAttribute))]
    public class NavmeshAgentTypePropertyDrawer : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            NavMeshComponentsGUIUtility.AgentTypePopup(property.displayName, property);
        }

    }
}