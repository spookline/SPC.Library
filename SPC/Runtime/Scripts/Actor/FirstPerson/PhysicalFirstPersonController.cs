using Sirenix.OdinInspector;
using Spookline.SPC.Actor.Attachments;
using Spookline.SPC.Events;
using Spookline.SPC.Ext;
using Spookline.SPC.Input;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Spookline.SPC.Actor.FirstPerson {
    public class PhysicalFirstPersonController : SpookBehaviour<PhysicalFirstPersonController>, IPossessor {

        public new CinemachineCamera camera;

        public Vector2 Input => _currentInput;

        [TabGroup("Slope & Ground")]
        public LayerMask groundLayer;
        [TabGroup("Slope & Ground")]
        public Vector3 groundCheckOffset;
        [TabGroup("Slope & Ground")]
        public float groundCheckDistance = 0.2f;
        [TabGroup("Slope & Ground")]
        public float maxStepHeight = 0.3f;
        [TabGroup("Slope & Ground")]
        public float stepCooldown = 0.01f;


        [TabGroup("Input")]
        public InputActionReference moveInput;

        [TabGroup("Input")]
        public InputActionReference sprintInput;

        [TabGroup("Input")]
        public InputActionReference crouchInput;

        [TabGroup("Input")]
        public InputActionReference jumpInput;

        [TabGroup("Synchronization")]
        public bool syncMainTransform = true;
        [TabGroup("Synchronization")]
        public bool syncEyeTransform = true;

        private AttachmentAccessor<PhysicalAttachment> _physicalAttachmentAccessor;
        private AttachmentAccessor<MovementAttachment> _movementAttachmentAccessor;
        private AttachmentAccessor<IMovementStateAttachment> _movementStateAttachmentAccessor;
        private AttachmentAccessor<StaminaAttachment> _staminaAttachmentAccessor;

        private Vector2 _currentInput;
        private RaycastHit _groundHit;
        private float _stepCooldown;
        private bool _jumpQueued;
        private Vector3 _currentHorizontalVel;
        private float _staminaTime;
        private bool _hasLanded;
        private Transform _cameraTransform;

        public bool IsGrounded { get; private set; }

        public bool IsSprinting { get; private set; }

        public bool IsCrouching { get; private set; }

        public bool IsMoving => _currentInput.sqrMagnitude > 0.01f;

        public float CurrentSpeed => _physicalAttachmentAccessor?.Value.rigidbody.linearVelocity.magnitude ?? 0f;

        private void Awake() {
            this.PerformedInput(jumpInput, _ => {
                if (Possessed == null || !_movementAttachmentAccessor.Value.jumpEnabled || !IsGrounded) return;
                _jumpQueued = true;
            });
            _cameraTransform = camera != null ? camera.transform : null;
        }

        private void Update() {
            if (!Possessed) return;
            _currentInput = moveInput.action.ReadValue<Vector2>();
            CheckGround();

            var isSprinting = sprintInput.action.IsPressed();

            // Stamina check
            if (_staminaAttachmentAccessor is { HasValue: true })
                if (!HandleStamina(isSprinting) && isSprinting)
                    isSprinting = false;

            // Raise sprinting changed event if sprinting state changed
            if (isSprinting != IsSprinting) {
                var evt = new PawnSprintingChangedEvt {
                    Pawn = Possessed,
                    IsSprinting = isSprinting
                };
                evt.Raise();
                if (!evt.IsCancelled) IsSprinting = isSprinting;
            }

            // Handle crouch input and raise event if crouching state changed
            var isCrouching = crouchInput.action.IsPressed();
            if (isCrouching != IsCrouching) {
                var evt = new PawnCrouchedChangedEvt {
                    Pawn = Possessed,
                    IsCrouched = isCrouching
                };
                evt.Raise();
                if (!evt.IsCancelled) IsCrouching = isCrouching;
            }

            if (!_movementStateAttachmentAccessor.HasValue) return;
            var state = _movementStateAttachmentAccessor.Value;
            state.IsMoving = IsMoving;
            state.IsGrounded = IsGrounded;
            state.CurrentSpeed = CurrentSpeed;
            state.Input = Input;
            state.IsSprinting = IsSprinting;
            state.IsCrouching = IsCrouching;
        }

        private void FixedUpdate() {
            if (!Possessed) return;

            var physicalAttachment = _physicalAttachmentAccessor.Value;
            var movementAttachment = _movementAttachmentAccessor.Value;
            var rb = physicalAttachment.rigidbody;

            // Input direction
            var dir = _cameraTransform.forward * _currentInput.y + _cameraTransform.right * _currentInput.x;
            dir.y = 0f;
            var moveDir = dir.normalized;
            if (IsGrounded) moveDir = Vector3.ProjectOnPlane(moveDir, _groundHit.normal);

            // Desired velocity
            var targetSpeed = movementAttachment.moveSpeed;
            if (IsCrouching) targetSpeed *= movementAttachment.crouchSpeedMultiplier;
            else if (IsSprinting) targetSpeed *= movementAttachment.sprintSpeedMultiplier;

            var desiredVelocity = moveDir * targetSpeed;

            // Apply horizontal move
            var horizontalVel = new Vector3(rb.linearVelocity.x, 0f,
                rb.linearVelocity.z);

            horizontalVel = Vector3.SmoothDamp(
                horizontalVel,
                desiredVelocity,
                ref _currentHorizontalVel,
                1f / movementAttachment.speedChangeRate,
                movementAttachment.moveSpeed * 10f,
                Time.fixedDeltaTime
            );
            rb.linearVelocity =
                new Vector3(horizontalVel.x, rb.linearVelocity.y, horizontalVel.z);

            // Apply manual ground drag
            if (IsGrounded && _currentInput.sqrMagnitude < 0.01f) {
                horizontalVel = new Vector3(rb.linearVelocity.x, 0f,
                    rb.linearVelocity.z);
                horizontalVel = Vector3.MoveTowards(horizontalVel, Vector3.zero,
                    movementAttachment.speedChangeRate * 2f * Time.fixedDeltaTime);
                rb.linearVelocity =
                    new Vector3(horizontalVel.x, rb.linearVelocity.y, horizontalVel.z);
            }

            // Apply slope gravity to counter sliding down slopes
            if (IsGrounded && _groundHit.normal.y > 0.01f) {
                var gravity = Physics.gravity;
                var slopeGravity = Vector3.ProjectOnPlane(gravity, _groundHit.normal);
                rb.AddForce(-slopeGravity, ForceMode.Acceleration);
            }

            HandleSteps(moveDir);
            HandleJump();
        }

        private void LateUpdate() {
            UpdatePawnTransforms();
        }

        private void OnDrawGizmos() {
            if (Possessed == null) return;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(Possessed.mainTransform.position + groundCheckOffset, 0.1f);
        }

        public Pawn Possessed { get; private set; }

        public void Possess(Pawn pawnToPossess) {
            Possessed = pawnToPossess;
            _physicalAttachmentAccessor = pawnToPossess.GetAccessor<PhysicalAttachment>();
            if (!_physicalAttachmentAccessor.HasValue) {
                Debug.LogError("Possessed pawn does not have a PhysicalAttachment.");
                return;
            }

            _movementAttachmentAccessor = pawnToPossess.GetAccessor<MovementAttachment>();
            if (!_movementAttachmentAccessor.HasValue) {
                Debug.LogError("Possessed pawn does not have a MovementAttachment.");
                return;
            }

            _movementStateAttachmentAccessor = pawnToPossess.GetAccessor<IMovementStateAttachment>();
            if (!_movementStateAttachmentAccessor.HasValue) {
                Debug.LogError("Possessed pawn does not have a MovementStateAttachment.");
                return;
            }

            _staminaAttachmentAccessor = pawnToPossess.GetAccessor<StaminaAttachment>();
            camera.Follow = pawnToPossess.eyeTransform;
            pawnToPossess.OnPossessed(this);
            Debug.Log("Possessing " + pawnToPossess.name);
        }

        public void Exorcise() {
            Possessed?.OnExorcised();
            camera.Follow = null;
            Possessed = null;
            Debug.Log("Exorcised " + Possessed?.name);
        }

        /// <summary>
        ///     Updates the pawn's main transform rotation to match the camera's horizontal rotation (yaw) and the eye transform
        ///     rotation to match the camera's full rotation.
        /// </summary>
        private void UpdatePawnTransforms() {
            var rotation = _cameraTransform.rotation;
            var headEuler = rotation.eulerAngles;
            var bodyRotation = Quaternion.Euler(0f, headEuler.y, 0f);
            if (syncMainTransform) {
                Possessed.mainTransform.rotation = bodyRotation;
            }

            if (syncEyeTransform) {
                Possessed.eyeTransform.rotation = rotation;
            }
        }

        /// <summary>
        ///     Checks if the pawn is grounded by performing a capsule cast downwards from the player's position.
        ///     It uses the collider dimensions from the PhysicalAttachment to determine the capsule's start and end points.
        ///     If the capsule cast hits the ground within the specified distance, it updates the IsGrounded property and raises a
        ///     PawnGroundedChangedEvt.
        ///     It also handles landing events by raising a PawnLandedEvt when the pawn transitions from not grounded to grounded.
        /// </summary>
        private void CheckGround() {
            var coll = _physicalAttachmentAccessor.Value.collider;
            var capsuleBottom = Possessed.mainTransform.position + Vector3.up * (coll.radius + 0.01f);
            var capsuleTop = capsuleBottom + Vector3.up * (coll.height - 2f * coll.radius);
            var radius = coll.radius;
            var isGrounded = Physics.CapsuleCast(
                capsuleTop,
                capsuleBottom,
                radius,
                Vector3.down,
                out _groundHit,
                groundCheckDistance,
                groundLayer
            );
            // Raise grounded changed event if grounded state changed
            if (isGrounded != IsGrounded) {
                IsGrounded = isGrounded;
                new PawnGroundedChangedEvt {
                    Pawn = Possessed,
                    IsGrounded = IsGrounded,
                    GroundHit = _groundHit
                }.Raise();
            }

            // Handle landing event
            if (IsGrounded && !_hasLanded) {
                _hasLanded = true;
                new PawnLandedEvt {
                    Pawn = Possessed,
                    GroundHit = _groundHit
                }.Raise();
            } else if (!IsGrounded) {
                _hasLanded = false;
            }
        }

        /// <summary>
        ///     Handles stamina decay and regeneration based on sprinting state and movement input.
        /// </summary>
        /// <returns> True if the pawn can sprint, false if stamina is depleted.</returns>
        private bool HandleStamina(bool wantsToSprint) {
            var staminaAttachment = _staminaAttachmentAccessor.Value;
            _staminaTime += Time.deltaTime;
            if (wantsToSprint && _currentInput.sqrMagnitude > 0.01f) {
                if (_staminaTime >= staminaAttachment.staminaDecayRate) {
                    staminaAttachment.Stamina = Mathf.Max(0, staminaAttachment.Stamina - 1);
                    _staminaTime = 0f;
                }
            } else {
                if (_staminaTime >= staminaAttachment.staminaRegenRate) {
                    staminaAttachment.Stamina = Mathf.Min(staminaAttachment.maxStamina, staminaAttachment.Stamina + 1);
                    _staminaTime = 0f;
                }
            }

            return staminaAttachment.Stamina > 0;
        }

        /// <summary>
        ///     Handles stepping up small obstacles by performing raycasts to detect step height and adjusting the player's
        ///     position accordingly.
        /// </summary>
        /// <param name="moveDir"> The current movement direction based on player input and camera orientation.</param>
        private void HandleSteps(Vector3 moveDir) {
            if (_stepCooldown > 0f) {
                _stepCooldown -= Time.fixedDeltaTime;
                return;
            }

            var physicalAttachment = _physicalAttachmentAccessor.Value;
            var rb = _physicalAttachmentAccessor.Value.rigidbody;

            if (moveDir.sqrMagnitude < 0.01f) return;
            var coll = physicalAttachment.collider;
            var origin = Possessed.mainTransform.position;
            var stepCheckDistance = coll.radius + 0.05f;
            if (!Physics.Raycast(origin + Vector3.up * 0.05f, moveDir,
                    out var lowerHit, stepCheckDistance, groundLayer))
                return;

            if (Physics.Raycast(origin + Vector3.up * maxStepHeight,
                    moveDir, stepCheckDistance, groundLayer))
                return;

            var stepTopOrigin = lowerHit.point + Vector3.up * maxStepHeight;

            if (!Physics.Raycast(stepTopOrigin, Vector3.down,
                    out var stepTopHit, maxStepHeight + 0.1f, groundLayer))
                return;

            var stepHeight = stepTopHit.point.y - origin.y;

            if (stepHeight <= 0f || stepHeight > maxStepHeight) return;

            var targetPos = rb.position;
            targetPos.y += stepHeight;

            const float maxStepUpSpeed = 4f;
            var allowedStep = maxStepUpSpeed * Time.fixedDeltaTime;

            var actualStep = Mathf.Min(stepHeight, allowedStep);

            targetPos = new Vector3(
                targetPos.x,
                rb.position.y + actualStep,
                targetPos.z
            );

            rb.MovePosition(targetPos);

            _stepCooldown = stepCooldown;
        }

        /// <summary>
        ///     Handles jumping by applying an upward velocity to the player's rigidbody based on the jump height defined in the
        ///     MovementAttachment and the gravity multiplier.
        ///     It also ensures that the player can only jump when grounded and resets the jump queue after performing a jump.
        /// </summary>
        private void HandleJump() {
            if (!_jumpQueued || !IsGrounded) return;
            var physicalAttachment = _physicalAttachmentAccessor.Value;
            var movementAttachment = _movementAttachmentAccessor.Value;
            var velocity = physicalAttachment.rigidbody.linearVelocity;
            velocity.y = 0f;

            var gravity = Mathf.Abs(Physics.gravity.y * movementAttachment.jumpGravityMultiplier);
            var jumpVelocity = Mathf.Sqrt(2f * gravity * movementAttachment.jumpHeight);
            velocity.y = jumpVelocity;
            physicalAttachment.rigidbody.linearVelocity = velocity;
            IsGrounded = false;
            _jumpQueued = false;
        }

    }
}