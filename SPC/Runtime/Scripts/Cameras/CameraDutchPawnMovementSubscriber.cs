using Spookline.SPC.Actor;
using UnityEngine;

namespace Spookline.SPC.Cameras {
    public class CameraDutchPawnMovementSubscriber : PawnMovementSubscriber<CameraDutchPawnMovementSubscriber> {

        public float dutchAmount = 1f;
        public float dutchTransitionDuration = 0.3f;

        private void LateUpdate() {
            if (!IsStateAvailable || !CameraDutch.HasInstance) return;

            var x = State.Input.x;
            if (x >= 0.4) {
                CameraDutch.Instance.Dutch(-dutchAmount, dutchTransitionDuration);
            } else if (x <= -0.4) {
                CameraDutch.Instance.Dutch(dutchAmount, dutchTransitionDuration);
            } else {
                CameraDutch.Instance.Dutch(0, dutchTransitionDuration);
            }
        }

    }
}