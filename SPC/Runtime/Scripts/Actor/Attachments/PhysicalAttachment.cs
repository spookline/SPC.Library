using System;
using UnityEngine;

namespace Spookline.SPC.Actor.Attachments {
    [Serializable]
    public class PhysicalAttachment : IPawnAttachment {

        public Rigidbody rigidbody;
        public CapsuleCollider collider;

    }
}