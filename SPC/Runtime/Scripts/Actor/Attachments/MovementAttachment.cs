using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Spookline.SPC.Actor.Attachments {
    [Serializable]
    public class MovementAttachment : IPawnAttachment {

        [FoldoutGroup("Speed"), Tooltip("Base movement speed")]
        public float moveSpeed = 5f;
        [FoldoutGroup("Speed"), Tooltip("Rate at which current speed changes towards target speed")]
        public float speedChangeRate = 5f;

        [FoldoutGroup("Speed"), Tooltip("Enable or disable sprinting")]
        public bool sprintEnabled = true;
        [FoldoutGroup("Speed"), ShowIf("sprintEnabled"), Tooltip("Multiplier applied to move speed when sprinting")]
        public float sprintSpeedMultiplier = 1.5f;

        [ToggleGroup("crouchEnabled", groupTitle: "Crouch")]
        public bool crouchEnabled;
        [ToggleGroup("crouchEnabled"), Tooltip("Offset applied to eye position when crouching")]
        public Vector3 crouchEyesOffset = new(0, -0.7f, 0);
        [ToggleGroup("crouchEnabled"), Tooltip("Multiplier applied to CharacterController height when crouching")]
        public float crouchColliderSizeReduction = 0.5f;
        [ToggleGroup("crouchEnabled"), Tooltip("Lerp speed for crouching eyes position and collider size")]
        public float crouchLerpSpeed = 5f;
        [ToggleGroup("crouchEnabled"), Tooltip("Multiplier applied to move speed when crouching")]
        public float crouchSpeedMultiplier = 0.5f;

        [ToggleGroup("fovEnabled", groupTitle: "FOV")]
        public bool fovEnabled = true;
        [ToggleGroup("fovEnabled"), Tooltip("FOV multiplier applied when sprinting")]
        public float sprintFovMultiplier = 1.1f;
        [ToggleGroup("fovEnabled"), Tooltip("Speed at which FOV changes when sprinting starts/stops")]
        public float sprintFovChangeSpeed = 3f;

        [ToggleGroup("jumpEnabled", groupTitle: "Jump")]
        public bool jumpEnabled = true;
        [ToggleGroup("jumpEnabled"), Tooltip("Height of the jump")]
        public float jumpHeight = 1.2f;
        [ToggleGroup("jumpEnabled"), Tooltip("Gravity multiplier applied specifically to jumping")]
        public float jumpGravityMultiplier = 1;

    }
}