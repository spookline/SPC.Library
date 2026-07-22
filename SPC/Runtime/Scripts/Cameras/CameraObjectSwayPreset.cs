using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Spookline.SPC.Cameras {
    [CreateAssetMenu(
        fileName = "Camera Object Sway Preset", menuName = "Camera/Camera Object Sway Preset")]
    public class CameraObjectSwayPreset : SerializedScriptableObject {
        
        public float rotationSwayAmount = 0.05f;
        public float maxSwayAmount = 0.1f;
        public float smoothSpeed = 8f;
        
        [ToggleGroup("useX", groupTitle: "X")]
        public bool useX = true;
        [ToggleGroup("useX"), InlineProperty, HideLabel]
        public CameraObjectSwayModifier x;
        [ToggleGroup("useY", groupTitle: "Y")]
        public bool useY;
        [ToggleGroup("useY"), InlineProperty, HideLabel]
        public CameraObjectSwayModifier y;

    }

    [Serializable]
    public class CameraObjectSwayModifier {
        
        public float amount = 0.02f;
        public float speed = 8f;

    }
}