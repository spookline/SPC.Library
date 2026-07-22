using Spookline.SPC.Actor;
using Spookline.SPC.Ext;

namespace Spookline.SPC.Cameras {
    public class CameraDutchPawnMovementSubscriber : SpookBehaviour<CameraDutchPawnMovementSubscriber> {

        public float dutchAmount = 1f;
        public float dutchTransitionDuration = 0.3f;

        private PawnMovementStateSubscriber<CameraDutchPawnMovementSubscriber> _subscriber;

        private void Awake() {
            _subscriber = this.SubscribeToPawnMovementState();
        }

        private void LateUpdate() {
            if (!_subscriber.IsStateAvailable || !CameraDutch.HasInstance) return;

            var x = _subscriber.State.Input.x;
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