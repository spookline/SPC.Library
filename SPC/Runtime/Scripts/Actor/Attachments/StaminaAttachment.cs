using System;
using UnityEngine;

namespace Spookline.SPC.Actor.Attachments {
    [Serializable]
    public class StaminaAttachment : IPawnAttachment {
        
        public int Stamina { get; set; }

        [Tooltip("Rate at which stamina regenerates when not sprinting")]
        public float staminaRegenRate = 0.05f;
        [Tooltip("Rate at which stamina decays while sprinting")]
        public float staminaDecayRate = 0.02f;
        [Tooltip("Time the player stays out of breath when stamina reaches zero")]
        public float outOfBreathTime = 1f;
        [Tooltip("Maximum stamina capacity")]
        public int maxStamina = 100;
        
    }
}