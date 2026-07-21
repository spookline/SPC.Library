using Spookline.SPC.Events;
using UnityEngine;

namespace Spookline.SPC.Actor {
    public struct PawnSprintingChangedEvt : Evt<PawnSprintingChangedEvt>, IPawnEvt {

        public bool IsCancelled { get; set; }
        public bool IsSprinting { get; set; }

        public Pawn Pawn { get; set; }
        public IPossessor Possessor { get; set; }

    }

    public struct PawnGroundedChangedEvt : Evt<PawnGroundedChangedEvt>, IPawnEvt {

        public bool IsGrounded { get; set; }
        public RaycastHit GroundHit { get; set; }

        public Pawn Pawn { get; set; }
        public IPossessor Possessor { get; set; }

    }

    public struct PawnJumpEvt : Evt<PawnJumpEvt>, IPawnEvt {

        public Pawn Pawn { get; set; }
        public IPossessor Possessor { get; set; }

    }

    public struct PawnLandedEvt : Evt<PawnLandedEvt>, IPawnEvt {

        public RaycastHit GroundHit { get; set; }

        public Pawn Pawn { get; set; }
        public IPossessor Possessor { get; set; }

    }

    public struct PawnCrouchedChangedEvt : Evt<PawnCrouchedChangedEvt>, IPawnEvt {

        public bool IsCrouched { get; set; }
        public bool IsCancelled { get; set; }

        public Pawn Pawn { get; set; }
        public IPossessor Possessor { get; set; }

    }

    public struct PawnStaminaOutOfBreathEvt : Evt<PawnStaminaOutOfBreathEvt>, IPawnEvt {

        public Pawn Pawn { get; set; }
        public IPossessor Possessor { get; set; }

    }
}