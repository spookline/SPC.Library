using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Spookline.SPC.Actor.Attachments {
    [Serializable]
    public class MovementAttachment : IPawnAttachment {

        [FoldoutGroup("Speed")]
        public float moveSpeed = 5f;
        [FoldoutGroup("Speed")]
        public float speedChangeRate = 5f;

        [FoldoutGroup("Speed")]
        public bool sprintEnabled = true;
        [FoldoutGroup("Speed"), ShowIf("sprintEnabled")]
        public float sprintSpeedMultiplier = 1.5f;

        [ToggleGroup("crouchEnabled", groupTitle: "Crouch")]
        public bool crouchEnabled;
        [ToggleGroup("crouchEnabled")]
        public Vector3 crouchEyesOffset = new(0, -0.7f, 0);
        [ToggleGroup("crouchEnabled")]
        public float crouchColliderSizeReduction = 0.5f;
        [ToggleGroup("crouchEnabled"), Tooltip("Lerp speed for crouching eyes position and collider size")]
        public float crouchLerpSpeed = 5f;
        [ToggleGroup("crouchEnabled")]
        public float crouchSpeedMultiplier = 0.5f;

        [ToggleGroup("fovEnabled", groupTitle: "FOV")]
        public bool fovEnabled = true;
        [ToggleGroup("fovEnabled")]
        public float sprintFovMultiplier = 1.1f;
        [ToggleGroup("fovEnabled")]
        public float sprintFovChangeSpeed = 3f;

        [ToggleGroup("jumpEnabled", groupTitle: "Jump")]
        public bool jumpEnabled = true;
        [ToggleGroup("jumpEnabled")]
        public float jumpHeight = 1.2f;
        [ToggleGroup("jumpEnabled")]
        public float jumpGravityMultiplier = 1;

    }
}