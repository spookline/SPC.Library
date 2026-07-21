using System;
using Sirenix.OdinInspector;
using Spookline.SPC.Debugging;
using Spookline.SPC.Events;
using Spookline.SPC.Ext;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Spookline.SPC.Actor.FirstPerson {
    [HideMonoScript]
    public partial class CharacterFirstPersonController : SpookBehaviour<CharacterFirstPersonController>, IPossessor,
        IHideScriptSceneIcon {

        [TabGroup("Ground")]
        public LayerMask groundLayer;
        [TabGroup("Ground")]
        public Vector3 groundCheckOffset;
        [TabGroup("Ground")]
        public float groundCheckRadius = 0.2f;
        [TabGroup("Ground")]
        public float groundCheckDistance = 0.3f;

        [TabGroup("Input")]
        public InputActionReference moveInput;
        [TabGroup("Input")]
        public InputActionReference jumpInput;
        [TabGroup("Input")]
        public InputActionReference crouchInput;
        [TabGroup("Input")]
        public InputActionReference sprintInput;
        [TabGroup("Input")]
        public InputActionReference lookInput;
        
        public bool rotateMainTransform = true;

        private float _verticalVelocity;
        private Vector3 _horizontalVelocity;
        private float _staminaTime;
        private float _outOfBreathTimer;
        private bool _hasLanded;
        private bool _lastSprint;
        private Vector3 _initialEyesPosition;
        private Vector3 _initialCharacterControllerCenter;
        private float _initialCharacterControllerHeight;

        private FovSource _fovSource;
        private bool _isOutOfBreath;
        private bool _lastCrouched;

        private void Awake() {
            On<GizmoEvt>().Do(OnGizmos);
            On<CollectDebugFlagsEvt>().Do(evt => evt.Add("movement"));
        }


        protected override void OnDisable() {
            base.OnDisable();
            _fovSource?.Dispose();
        }

        private void Update() {
            if (!Possessed) return;
            var mainTransform = Possessed.mainTransform;
            var controller = _characterAttachmentAccessor.Value.controller;

            Input = moveInput.action.ReadValue<Vector2>();
            Debug.Log("Setting input to: " + Input);
            IsSprinting = sprintInput.action.IsPressed();
            IsCrouching = crouchInput.action.IsPressed();

            HandleCameraRotationInput();

            if (rotateMainTransform) {
                mainTransform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            }

            // Ground check
            var isGrounded = Physics.SphereCast(
                mainTransform.position + groundCheckOffset + new Vector3(0, groundCheckRadius, 0),
                groundCheckRadius,
                Vector3.down,
                out var groundHit,
                groundCheckDistance,
                groundLayer
            );
            if (isGrounded != IsGrounded) {
                IsGrounded = isGrounded;
                new PawnGroundedChangedEvt {
                    GroundHit = groundHit,
                    IsGrounded = isGrounded
                }.Raise();
            }

            if (IsGrounded && !_hasLanded) {
                _hasLanded = true;
                new PawnLandedEvt {
                    GroundHit = groundHit
                }.Raise();
            } else if (!isGrounded) {
                _hasLanded = false;
            }

            HandleStamina();
            if (_movementAttachmentAccessor.Value.crouchEnabled) {
                if (_lastCrouched != IsCrouching) {
                    var evt = new PawnCrouchedChangedEvt {
                        IsCrouched = IsCrouching
                    }.Raise();
                    if (evt.IsCancelled) {
                        IsCrouching = _lastCrouched;
                    }
                    _lastCrouched = IsCrouching;
                }
                HandleCrouch();
            }

            

            if (_lastSprint != IsSprinting) {
                var evt = new PawnSprintingChangedEvt {
                    IsSprinting = IsSprinting
                }.Raise();
                if (evt.IsCancelled) {
                    IsSprinting = _lastSprint;
                }

                _lastSprint = IsSprinting;
            }

            // Apply horizontal movement
            var forward = mainTransform.forward;
            var right = mainTransform.right;
            forward.y = 0;
            right.y = 0;

            forward.Normalize();
            right.Normalize();

            var direction = Vector3.ClampMagnitude(forward * Input.y + right * Input.x, 1f);

            var desiredSpeed = _movementAttachmentAccessor.Value.moveSpeed;
            if (IsSprinting && !IsCrouching && _movementAttachmentAccessor.Value.sprintEnabled) desiredSpeed *= _movementAttachmentAccessor.Value.sprintSpeedMultiplier;
            if (IsCrouching && _movementAttachmentAccessor.Value.crouchEnabled) desiredSpeed *= _movementAttachmentAccessor.Value.crouchSpeedMultiplier;
            
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, desiredSpeed, _movementAttachmentAccessor.Value.speedChangeRate * Time.deltaTime);
            
            var targetVelocity = direction * CurrentSpeed;

            IsMoving = targetVelocity.sqrMagnitude > 0.001f;

            var sharpness = IsMoving ? _movementAttachmentAccessor.Value.acceleration : _movementAttachmentAccessor.Value.deceleration;

            _horizontalVelocity = Vector3.MoveTowards(
                _horizontalVelocity,
                targetVelocity,
                sharpness * Time.deltaTime
            );

            // Apply gravity
            var jumpPressed = jumpInput.action.WasPerformedThisFrame();
            var gravity = _characterAttachmentAccessor.Value.gravity;

            if (IsGrounded && _verticalVelocity < 0) {
                _verticalVelocity = -2f;
            }

            if (IsGrounded && jumpPressed && _movementAttachmentAccessor.Value.jumpEnabled) {
                var absGravity = Mathf.Abs(gravity * _movementAttachmentAccessor.Value.jumpGravityMultiplier);
                _verticalVelocity = Mathf.Sqrt(2 * _movementAttachmentAccessor.Value.jumpHeight * absGravity);
                new PawnJumpEvt().Raise();
            }

            _verticalVelocity += gravity * Time.deltaTime;

            var velocity = _horizontalVelocity;
            velocity.y = _verticalVelocity;
            
            Velocity = velocity;

            if (!controller || !controller.enabled) return;

            controller.Move(velocity * Time.deltaTime);
        }


        private void HandleStamina() {
            if (!_staminaAttachmentAccessor.HasValue) return;
            var staminaAttachment = _staminaAttachmentAccessor.Value;
            _staminaTime += Time.deltaTime;
            if (_outOfBreathTimer > 0) _outOfBreathTimer -= Time.deltaTime;
            if (IsMoving && IsSprinting) {
                if (_staminaTime >= staminaAttachment.staminaDecayRate) {
                    staminaAttachment.Stamina = Mathf.Max(0, staminaAttachment.Stamina - 1);
                    _staminaTime = 0f;
                }
            } else if (_outOfBreathTimer <= 0) {
                if (_staminaTime >= staminaAttachment.staminaRegenRate) {
                    staminaAttachment.Stamina = Mathf.Min(staminaAttachment.maxStamina, staminaAttachment.Stamina + 1);
                    _staminaTime = 0f;
                }
            }

            if (staminaAttachment.Stamina > 0) {
                _isOutOfBreath = false;
                return;
            }

            IsSprinting = false;

            if (_isOutOfBreath) return;

            _isOutOfBreath = true;
            _outOfBreathTimer = staminaAttachment.outOfBreathTime;

            new PawnStaminaOutOfBreathEvt {
                Pawn = Possessed,
                Possessor = this
            }.Raise();
        }

        private void HandleCrouch() {
            var nextEyesPosition = IsCrouching ? _initialEyesPosition + _movementAttachmentAccessor.Value.crouchEyesOffset : _initialEyesPosition;
            Possessed.eyeTransform.localPosition = Vector3.Lerp(Possessed.eyeTransform.localPosition, nextEyesPosition, Time.deltaTime * _movementAttachmentAccessor.Value.crouchLerpSpeed);

            var targetHeight = !IsCrouching
                ? _initialCharacterControllerHeight
                : _initialCharacterControllerHeight * _movementAttachmentAccessor.Value.crouchColliderSizeReduction;
            var heightDiff = _initialCharacterControllerHeight - targetHeight;
            var targetCenter = IsCrouching ? _initialCharacterControllerCenter - Vector3.up * (heightDiff * 0.5f) : _initialCharacterControllerCenter;

            var controller = _characterAttachmentAccessor.Value.controller;
            controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * _movementAttachmentAccessor.Value.crouchLerpSpeed);
            controller.center = Vector3.Lerp(controller.center, targetCenter, Time.deltaTime * _movementAttachmentAccessor.Value.crouchLerpSpeed);
        }

        private void LateUpdate() {
            if (!Possessed) return;
            UpdateCamera();
        }

        private void OnDrawGizmos() {
            if (!Possessed) return;
            var position = Possessed.mainTransform.position + groundCheckOffset + new Vector3(0, groundCheckRadius, 0);
            Gizmos.color = !IsGrounded ? Color.red : Color.green;
            Gizmos.DrawWireSphere(position, groundCheckRadius);
        }

        private void OnGizmos(ref GizmoEvt args) {
            if (!args.HasFlag("movement") || !Possessed) return;
            if (args.ScreenOverlayPass(out var screen)) {
                screen.Global()
                    .Field("Velocity", Velocity)
                    .Field("Speed", CurrentSpeed)
                    .Field("-----", "-----")
                    .Field("IsGrounded", IsGrounded.ToString())
                    .Field("Input", Input);
            }
        }

    }
}