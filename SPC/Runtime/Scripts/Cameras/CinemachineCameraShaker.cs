using Unity.Cinemachine;
using UnityEngine;

namespace Spookline.SPC.Cameras {
    public class CinemachineCameraShaker : CinemachineExtension {

        public static CinemachineCameraShaker Instance { get; private set; }
        
        [SerializeField]
        private AnimationCurve falloff = AnimationCurve.EaseInOut(
            0f, 1f,
            1f, 0f
        );

        private float _shakeDuration;
        private float _shakeTimer;
        private float _positionStrength;
        private float _rotationStrength;
        private float _frequency;

        private float _noiseTime;
        private int _shakeSeed;

        protected override void Awake() {
            base.Awake();
            Instance = this;
        }

        public void Shake(
            float duration,
            float positionAmount,
            float rotationAmount,
            float shakeFrequency = 25f
        ) {
            _shakeDuration = Mathf.Max(0.01f, duration);
            _shakeTimer = _shakeDuration;

            _positionStrength = Mathf.Max(0f, positionAmount);
            _rotationStrength = Mathf.Max(0f, rotationAmount);
            _frequency = Mathf.Max(0.01f, shakeFrequency);

            _noiseTime = 0f;
            _shakeSeed = Random.Range(-10000, 10000);
        }

        public void StopShake() {
            _shakeTimer = 0f;
        }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase virtualCamera,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime
        ) {
            if (stage != CinemachineCore.Stage.Finalize)
                return;

            if (_shakeTimer <= 0f || deltaTime <= 0f)
                return;

            _shakeTimer -= deltaTime;
            _noiseTime += deltaTime * _frequency;

            var normalizedTime = 1f - Mathf.Clamp01(_shakeTimer / _shakeDuration);
            var intensity = falloff.Evaluate(normalizedTime);

            var positionNoise = new Vector3(
                SampleNoise(0),
                SampleNoise(1),
                SampleNoise(2)
            );

            var rotationNoise = new Vector3(
                SampleNoise(3),
                SampleNoise(4),
                SampleNoise(5)
            );

            state.PositionCorrection +=
                positionNoise * _positionStrength * intensity;

            state.OrientationCorrection *= Quaternion.Euler(
                rotationNoise * _rotationStrength * intensity
            );
        }

        private float SampleNoise(int channel) {
            var channelOffset = _shakeSeed + channel * 37.17f;
            return Mathf.PerlinNoise(channelOffset, _noiseTime) * 2f - 1f;
        }

    }
}