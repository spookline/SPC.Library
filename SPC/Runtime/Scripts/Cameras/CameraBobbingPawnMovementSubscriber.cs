using Spookline.SPC.Actor;
using Spookline.SPC.Ext;

namespace Spookline.SPC.Cameras {
    public class CameraBobbingPawnMovementSubscriber : SpookBehaviour<CameraBobbingPawnMovementSubscriber> {

        public CameraBobbingPreset idle;
        public CameraBobbingPreset walk;
        public CameraBobbingPreset sprint;
        public CameraBobbingPreset crouch;

        private PawnMovementStateSubscriber<CameraBobbingPawnMovementSubscriber> _subscriber;

        private void Awake() {
            _subscriber = this.SubscribeToPawnMovementState();
        }


        private void LateUpdate() {
            if (!_subscriber.IsStateAvailable || !CameraBobbing.HasInstance) return;
            var state = _subscriber.State;
            var cameraBobbing = CameraBobbing.Instance;
            if (!state.IsMoving) {
                if (idle) cameraBobbing.SetState(idle);
                return;
            }

            if (state.IsCrouching && crouch) {
                cameraBobbing.SetState(crouch);
            } else if (state.IsSprinting && sprint) {
                cameraBobbing.SetState(sprint);
            } else if (walk) {
                cameraBobbing.SetState(walk);
            }
        }

    }
}