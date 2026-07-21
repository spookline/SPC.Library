using Spookline.SPC.Actor;
using Spookline.SPC.Ext;

namespace Spookline.SPC.Cameras {
    public class CameraBobbingPawnMovementSubscriber : SpookBehaviour<CameraBobbingPawnMovementSubscriber> {

        public CameraBobbingPreset idle;
        public CameraBobbingPreset walk;
        public CameraBobbingPreset sprint;
        public CameraBobbingPreset crouch;

        private AttachmentAccessor<IMovementStateAttachment> _movementStateAttachment;

        private void Awake() {
            On<PawnPossessedEvt>().Do(evt => {
                _movementStateAttachment = evt.Pawn.GetAccessor<IMovementStateAttachment>();
            });
            On<PawnExorcisedEvt>().Do(evt => {
                if (evt.Pawn.HasAttachment<IMovementStateAttachment>()) {
                    _movementStateAttachment = null;
                }
            });
        }

        private void LateUpdate() {
            if (_movementStateAttachment is not { HasValue: true }) return;
            var state = _movementStateAttachment.Value;
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