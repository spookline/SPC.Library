using Spookline.SPC.Geometry;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Spookline.SPC.Editor {
    [CustomPropertyDrawer(typeof(OrientedBox))]
    public class OrientedBoxDrawer : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var centerProp = property.FindPropertyRelative("center");
            var halfExtentProp = property.FindPropertyRelative("halfExtent");
            var rotationProp = property.FindPropertyRelative("rotation");

            EditorGUILayout.PropertyField(centerProp, new GUIContent("Center"));

            // Rotation as Euler
            EditorGUI.BeginChangeCheck();
            var size = (Vector3)(float3)halfExtentProp.boxedValue * 2f;
            var newSize = EditorGUILayout.Vector3Field("Size", size);

            var q = (Quaternion)(quaternion)rotationProp.boxedValue;
            var euler = q.eulerAngles;
            var newEuler = EditorGUILayout.Vector3Field("Rotation", euler);
            if (EditorGUI.EndChangeCheck()) {
                rotationProp.boxedValue = (quaternion)Quaternion.Euler(newEuler);
                halfExtentProp.boxedValue = (float3)(newSize / 2f);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return 0f;
        }

    }
}