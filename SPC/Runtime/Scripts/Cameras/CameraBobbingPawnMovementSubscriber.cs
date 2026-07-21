using Spookline.SPC.Actor;

namespace Spookline.SPC.Cameras {
    public class CameraBobbingPawnMovementSubscriber : PawnMovementSubscriber<CameraBobbingPawnMovementSubscriber> {

        public CameraBobbingPreset idle;
        public CameraBobbingPreset walk;
        public CameraBobbingPreset sprint;
        public CameraBobbingPreset crouch;


        private void LateUpdate() {
            if (!IsStateAvailable || !CameraBobbing.HasInstance) return;
            var cameraBobbing = CameraBobbing.Instance;
            if (!State.IsMoving) {
                if (idle) cameraBobbing.SetState(idle);
                return;
            }

            if (State.IsCrouching && crouch) {
                cameraBobbing.SetState(crouch);
            } else if (State.IsSprinting && sprint) {
                cameraBobbing.SetState(sprint);
            } else if (walk) {
                cameraBobbing.SetState(walk);
            }
        }

    }
}