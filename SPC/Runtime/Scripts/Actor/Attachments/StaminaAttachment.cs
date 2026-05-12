using System;

namespace Spookline.SPC.Actor.Attachments {
    [Serializable]
    public class StaminaAttachment : IPawnAttachment {

        public int Stamina { get; set; }

        public float staminaRegenRate = 0.05f;
        public float staminaDecayRate = 0.02f;
        public float outOfBreathTime = 1f;
        public int maxStamina = 100;
        
    }
}