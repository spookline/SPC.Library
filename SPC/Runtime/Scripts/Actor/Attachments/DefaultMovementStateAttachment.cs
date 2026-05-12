using System;
using UnityEngine;

namespace Spookline.SPC.Actor.Attachments {
    [Serializable]
    public class DefaultMovementStateAttachment : IMovementStateAttachment {

        public bool IsGrounded { get; set; }

        public bool IsSprinting { get; set; }

        public bool IsCrouching { get; set; }

        public bool IsMoving { get; set; }

        public float CurrentSpeed { get; set; }

        public Quaternion LookDirection { get; set; }

        public Vector2 Input { get; set; }

    }
}