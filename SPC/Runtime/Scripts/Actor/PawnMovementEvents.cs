using UnityEngine;

namespace Spookline.SPC.Actor {

    public class PawnSprintingChangedEvt : PawnEvt<PawnSprintingChangedEvt> {

        public bool IsCancelled { get; set; } = false;
        public bool IsSprinting { get; set; }
        
    }
    
    public class PawnGroundedChangedEvt : PawnEvt<PawnGroundedChangedEvt> {
        
        public bool IsGrounded { get; set; }
        public RaycastHit GroundHit { get; set; }
        
    }
    
    public class PawnJumpEvt : PawnEvt<PawnJumpEvt> {
        
    }

    public class PawnLandedEvt : PawnEvt<PawnLandedEvt> {
        
        public RaycastHit GroundHit { get; set; }

    }
    
    public class PawnCrouchedChangedEvt : PawnEvt<PawnCrouchedChangedEvt> {
        
        public bool IsCrouched { get; set; }
        public bool IsCancelled { get; set; } = false;
        
    }
    
    public class PawnStaminaOutOfBreathEvt : PawnEvt<PawnStaminaOutOfBreathEvt> {
        
        
    }
}