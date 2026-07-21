using Sirenix.OdinInspector;
using Spookline.SPC.Ext;
using Unity.Cinemachine;
using UnityEngine;

namespace Spookline.SPC {
    public class CameraDutch : SpookManagerBehaviour<CameraDutch> {

        [Required]
        public CinemachineCamera cinemachineCamera;
        
        [SerializeField]
        private AnimationCurve dutchEasing = AnimationCurve.EaseInOut(
            0f, 0f,
            1f, 1f
        );

        private float _startDutch;
        private float _targetDutch;
        private float _transitionDuration;
        private float _transitionTime;
        private bool _isTransitioning;

        private void Update() {
            if (!cinemachineCamera || !_isTransitioning) return;
            _transitionTime += Time.deltaTime;
            var normalizedTime = Mathf.Clamp01(_transitionTime / _transitionDuration);
            var easedTime = dutchEasing.Evaluate(normalizedTime);
            cinemachineCamera.Lens.Dutch = Mathf.LerpUnclamped(
                _startDutch,
                _targetDutch,
                easedTime
            );
            if (!(normalizedTime >= 1f)) return;
            
            cinemachineCamera.Lens.Dutch = _targetDutch;
            _isTransitioning = false;
        }

        public void Dutch(float amount, float duration) {
            if (!cinemachineCamera) return;
            if(_isTransitioning && Mathf.Approximately(_targetDutch, amount)) return;
            _startDutch = cinemachineCamera.Lens.Dutch;
            _targetDutch = amount;
            _transitionDuration = Mathf.Max(duration, 0.001f);
            _transitionTime = 0f;
            _isTransitioning = true;
        }

    }
}