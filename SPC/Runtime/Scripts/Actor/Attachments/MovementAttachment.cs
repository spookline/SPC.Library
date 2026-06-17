using System;

namespace Spookline.SPC.Actor.Attachments {
    [Serializable]
    public class MovementAttachment : IPawnAttachment {

        public float moveSpeed = 5f;
        public float crouchSpeedMultiplier = 0.5f;
        public float sprintSpeedMultiplier = 1.5f;
        public float sprintFovMultiplier = 1.1f;
        public float sprintFovChangeSpeed = 3f;
        public float speedChangeRate = 5f;
        public bool jumpEnabled = true;
        public float jumpHeight = 1.2f;
        public float jumpGravityMultiplier = 1;

    }
}