using Sirenix.OdinInspector;
using Spookline.SPC.Ext;
using UnityEngine;

namespace Spookline.SPC.Cameras {
    [DefaultExecutionOrder(1000)]
    public class CameraObjectSway : SpookManagerBehaviour<CameraObjectSway> {

        [Required]
        public new Transform camera;

        public CameraObjectSwayPreset preset;

        private Vector3 _initialPosition;
        private Vector3 _currentPosition;
        private Vector3 _lastCameraEuler;

        private Vector3 _targetPosition;

        protected override void Awake() {
            base.Awake();
            _initialPosition = transform.localPosition;
            _currentPosition = _initialPosition;
        }

        private void LateUpdate() {
            if (!camera) return;
            var cameraEuler = camera.localEulerAngles;
            var deltaX = Mathf.DeltaAngle(_lastCameraEuler.x, cameraEuler.x);
            var deltaY = Mathf.DeltaAngle(_lastCameraEuler.y, cameraEuler.y);
            var movement = new Vector3(-deltaY, -deltaX, 0) * preset.rotationSwayAmount;

            var swayX = preset.useX ? Mathf.Sin(Time.time * preset.x.speed) * preset.x.amount : 0f;
            var swayY = preset.useY ? Mathf.Abs(Mathf.Sin(Time.time * preset.y.speed)) * preset.y.amount : 0f;
            movement.x += swayX;
            movement.y += swayY;

            movement = Vector3.ClampMagnitude(movement, preset.maxSwayAmount);
            _targetPosition = _initialPosition + movement;
            
            var time =  1f - Mathf.Exp(-preset.smoothSpeed * Time.deltaTime);
            _currentPosition = Vector3.Lerp(_currentPosition, _targetPosition, time);
            transform.localPosition = _currentPosition;
            _lastCameraEuler = cameraEuler;
        }

    }
}