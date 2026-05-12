using System;
using UnityEngine;

namespace Spookline.SPC.Actor.Attachments {
    
    [Serializable]
    
    public class PhysicalAttachment : IPawnAttachment {

        public new Rigidbody rigidbody;
        public new CapsuleCollider collider;

    }
}