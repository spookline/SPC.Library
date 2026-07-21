using Spookline.SPC.Actor.Attachments;
using Spookline.SPC.Ext;
using UnityEngine;

namespace Spookline.SPC.Actor.FirstPerson {
    public partial class CharacterFirstPersonController {

        public Pawn Possessed { get; private set; }

        private bool IsGrounded {
            get => _movementStateAttachmentAccessor.Value.IsGrounded;
            set => _movementStateAttachmentAccessor.Value.IsGrounded = value;
        }

        private bool IsCrouching {
            get => _movementStateAttachmentAccessor.Value.IsCrouching;
            set => _movementStateAttachmentAccessor.Value.IsCrouching = value;
        }

        private Vector2 Input {
            get => _movementStateAttachmentAccessor.Value.Input;
            set => _movementStateAttachmentAccessor.Value.Input = value;
        }

        private bool IsMoving {
            get => _movementStateAttachmentAccessor.Value.IsMoving;
            set => _movementStateAttachmentAccessor.Value.IsMoving = value;
        }

        private float CurrentSpeed {
            get => _movementStateAttachmentAccessor.Value.CurrentSpeed;
            set => _movementStateAttachmentAccessor.Value.CurrentSpeed = value;
        }

        private Vector3 Velocity {
            get => _movementStateAttachmentAccessor.Value.Velocity;
            set => _movementStateAttachmentAccessor.Value.Velocity = value;
        }

        private bool IsSprinting {
            get => _movementStateAttachmentAccessor.Value.IsSprinting;
            set => _movementStateAttachmentAccessor.Value.IsSprinting = value;
        }

        private AttachmentAccessor<CharacterAttachment> _characterAttachmentAccessor;
        private AttachmentAccessor<MovementAttachment> _movementAttachmentAccessor;
        private AttachmentAccessor<IMovementStateAttachment> _movementStateAttachmentAccessor;
        private AttachmentAccessor<StaminaAttachment> _staminaAttachmentAccessor;

        public void Possess(Pawn pawnToPossess) {
            _characterAttachmentAccessor = pawnToPossess.GetAccessor<CharacterAttachment>();
            _movementAttachmentAccessor = pawnToPossess.GetAccessor<MovementAttachment>();
            _movementStateAttachmentAccessor = pawnToPossess.GetAccessor<IMovementStateAttachment>();
            _staminaAttachmentAccessor = pawnToPossess.GetAccessor<StaminaAttachment>();
            Possessed = pawnToPossess;
            camera.Follow = Possessed.eyeTransform;
            if (_movementAttachmentAccessor.Value.fovEnabled) {
                _fovSource = Ext.AddFovSource(new FovSource(() => IsSprinting && IsMoving,
                    _movementAttachmentAccessor.Value.sprintFovMultiplier,
                    speed: _movementAttachmentAccessor.Value.sprintFovChangeSpeed, mode: FovValueMode.Multiplier));
            }
            _initialEyesPosition = Possessed.eyeTransform.localPosition;
            _initialCharacterControllerCenter = _characterAttachmentAccessor.Value.controller.center;
            _initialCharacterControllerHeight = _characterAttachmentAccessor.Value.controller.height;
            Possessed.OnPossessed(this);
        }

        public void Exorcise() {
            Possessed?.OnExorcised();
            Possessed = null;
        }

    }
}