using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;

namespace Spookline.SPC.Actor.FirstPerson {
    public partial class CharacterFirstPersonController {

        [Required, TabGroup("Camera")]
        public new CinemachineCamera camera;

        [TabGroup("Camera")]
        public float sensitivity = 0.05f;
        [TabGroup("Camera")]
        public Vector2 pitchLimits = new(-70, 70);

        private float _yaw;
        private float _pitch;


        private void HandleCameraRotationInput() {
            if (!camera) return;
            var input = lookInput.action.ReadValue<Vector2>();
            _yaw += input.x * sensitivity;
            _pitch -= input.y * sensitivity;
            _pitch = NormalizeAngle(_pitch);
            _pitch = Mathf.Clamp(_pitch, pitchLimits.x, pitchLimits.y);
            _movementStateAttachmentAccessor.Value.LookDirection = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void UpdateCamera() {
            if (!camera) return;
            var rotation = Quaternion.Euler(_pitch, _yaw, 0);
            Possessed.eyeTransform.rotation = rotation;
        }

        private static float NormalizeAngle(float angle) {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

    }
}