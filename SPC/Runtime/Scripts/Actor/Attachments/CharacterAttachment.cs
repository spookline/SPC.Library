using System;
using UnityEngine;

namespace Spookline.SPC.Actor.Attachments {
    [Serializable]
    public class CharacterAttachment : IPawnAttachment {

        public CharacterController controller;
        public float gravity = -9.81f;
        

    }
}