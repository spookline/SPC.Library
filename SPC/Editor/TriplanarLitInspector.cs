using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

public class TriplanarLitInspector : ShaderGUI {

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {
        // 1. Fetch the properties from your Shader Graph by their reference names
        MaterialProperty baseMap = FindProperty("_BaseMap", properties);
        MaterialProperty baseColor = FindProperty("_BaseColor", properties);
        MaterialProperty normalMap = FindProperty("_NormalMap", properties);
        MaterialProperty normalStrength = FindProperty("_NormalStrength", properties);
        MaterialProperty tilingProp = FindProperty("_Tiling", properties);
        MaterialProperty sharpness = FindProperty("_Sharpness", properties);
        MaterialProperty remapProp = FindProperty("_Remap", properties);


        isSurfaceFoldoutOpen = CoreEditorUtils.DrawHeaderFoldout("Surface Inputs", isSurfaceFoldoutOpen);

        if (isSurfaceFoldoutOpen) {
            // This helper method draws a Texture slot and a Color picker on the SAME line
            materialEditor.TexturePropertySingleLine(new GUIContent("Albedo (RGB)"), baseMap, baseColor);
            materialEditor.TexturePropertySingleLine(new GUIContent("Normal Map"), normalMap, normalStrength);

            // Add some spacing
            EditorGUILayout.Space();
            materialEditor.RangeProperty(sharpness, "Sharpness");
            DrawUniformVector3(tilingProp, "Tiling");
            //DrawRemapSlider(remapProp, "Remap Range", 0f, 1f);
            EditorGUILayout.Space(15);
        }

        isAdvancedFoldoutOpen = CoreEditorUtils.DrawHeaderFoldout("Advanced Options", isAdvancedFoldoutOpen);
        if (isAdvancedFoldoutOpen) {
            base.OnGUI(materialEditor, properties);
        }
    }

    // Helper to make bold headers like the built-in materials
    private void Label(string text) {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
        EditorGUILayout.Space(2);
    }


    // Tracks whether the X, Y, and Z axes are locked together
    private bool lockScaleUniformly = false;
    private bool isAdvancedFoldoutOpen = false;
    private bool isSurfaceFoldoutOpen = true;

    private void DrawUniformVector3(MaterialProperty property, string label) {
        Vector3 vecValue = property.vectorValue;

        // 1. Begin a horizontal layout to sit the label, fields, and lock button side-by-side
        EditorGUILayout.BeginHorizontal();

        // Respect standard prefix label widths
        EditorGUILayout.PrefixLabel(label);

        // 2. Read the current values to look for manual changes
        EditorGUI.BeginChangeCheck();

        // We use EditorGUIUtility.labelWidth = 13 to draw tiny, native 'X', 'Y', 'Z' tags
        float originalLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 13f;

        // Draw the X field
        float x = EditorGUILayout.FloatField("X", vecValue.x);
        // Draw the Y field
        float y = EditorGUILayout.FloatField("Y", vecValue.y);
        // Draw the Z field
        float z = EditorGUILayout.FloatField("Z", vecValue.z);

        // Restore the original label width styling
        EditorGUIUtility.labelWidth = originalLabelWidth;

        // 3. Handle the Uniform Scale logic if something changed
        if (EditorGUI.EndChangeCheck()) {
            if (lockScaleUniformly) {
                // Determine which axis changed and apply the ratio uniformly to the others
                if (x != vecValue.x) {
                    y = x;
                    z = x;
                } else if (y != vecValue.y) {
                    x = y;
                    z = y;
                } else if (z != vecValue.z) {
                    x = z;
                    y = z;
                }
            }

            // Save the updated vector back to the material property
            property.vectorValue = new Vector4(x, y, z, property.vectorValue.w);
        }

        // 4. Draw the native "Link" button at the very end of the row
        // We use Unity's built-in icon styling for a perfectly seamless look
        GUIStyle lockStyle = new GUIStyle("Button");
        GUIContent lockIcon = lockScaleUniformly
            ? EditorGUIUtility.IconContent("LockIcon-On")
            : EditorGUIUtility.IconContent("LockIcon");

        // Force the button to stay small and square
        if (GUILayout.Button(lockIcon, lockStyle, GUILayout.Width(22), GUILayout.Height(18))) {
            lockScaleUniformly = !lockScaleUniformly;

            // Optional: When clicking 'Lock', instantly snap Y and Z to match X
            if (lockScaleUniformly) {
                property.vectorValue = new Vector4(vecValue.x, vecValue.x, vecValue.x, property.vectorValue.w);
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawRemapSlider(MaterialProperty property, string label, float minLimit, float maxLimit)
    {
        // Pull the current Min (X) and Max (Y) from the material's Vector2 property
        Vector4 currentVector = property.vectorValue;
        float minVal = currentVector.x;
        float maxVal = currentVector.y;

        // 1. Begin a horizontal layout to hold the label and the slider controls together
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);

        // 2. Track changes so Unity knows when to save/register an Undo command
        EditorGUI.BeginChangeCheck();
        //
        // // Optional: Draw a tiny read-out float field for the Min value on the left
        // minVal = EditorGUILayout.FloatField(minVal, GUILayout.Width(45));
        // EditorGUILayout.Space(4);

        // 3. Draw the actual native dual-slider bar
        // Arguments: (currentMin, currentMax, totalSliderMinBound, totalSliderMaxBound)
        EditorGUILayout.MinMaxSlider(ref minVal, ref maxVal, minLimit, maxLimit);
        // EditorGUILayout.Space(4);
        //
        // // Optional: Draw a tiny read-out float field for the Max value on the right
        // maxVal = EditorGUILayout.FloatField(maxVal, GUILayout.Width(45));

        if (EditorGUI.EndChangeCheck())
        {
            // Clamp values so min never crosses max
            if (minVal > maxVal) minVal = maxVal;

            // Save the adjusted float range back into the Vector2 (X and Y slots)
            property.vectorValue = new Vector4(minVal, maxVal, currentVector.z, currentVector.w);
        }

        EditorGUILayout.EndHorizontal();
    }

}