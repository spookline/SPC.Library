using System;
using Spookline.SPC.Ext;
using Unity.Cinemachine;
using UnityEngine;

namespace Spookline.SPC.Cameras {
    public class CameraBobbing : SpookManagerBehaviour<CameraBobbing> {

        public CameraBobbingPreset CurrentState { get; private set; }

        [SerializeField]
        private CinemachineBasicMultiChannelPerlin noise;

        [SerializeField]
        private CameraBobbingPreset initialState;

        protected override void Start() {
            base.Start();

            if (initialState) {
                SetState(initialState, true);
            }
        }

        private void Update() {
            if (!CurrentState || !noise) {
                return;
            }

            var duration = CurrentState.transitionDuration;

            if (duration <= 0f) {
                ApplyState(CurrentState);
                return;
            }

            var time = Mathf.Clamp01(Time.deltaTime / duration);

            if (CurrentState.useSmoothStep) {
                time = Mathf.SmoothStep(0f, 1f, time);
            }

            noise.AmplitudeGain = Mathf.Lerp(
                noise.AmplitudeGain,
                CurrentState.amplitude,
                time
            );

            noise.FrequencyGain = Mathf.Lerp(
                noise.FrequencyGain,
                CurrentState.frequency,
                time
            );
        }

        public void SetState(CameraBobbingPreset preset, bool immediate = false) {
            if (!preset) return;
            if (!noise) {
                Debug.LogError("CinemachineBasicMultiChannelPerlin is null", this);
                return;
            }
            CurrentState = preset;
            if (preset.noiseSettings) {
                noise.NoiseProfile = preset.noiseSettings;
            }
            if (immediate) {
                ApplyState(preset);
            }
        }

        private void ApplyState(CameraBobbingPreset preset) {
            noise.AmplitudeGain = preset.amplitude;
            noise.FrequencyGain = preset.frequency;
        }

    }

    [Serializable]
    public class CameraBobbingSettings {

        public float amplitude = 0.3f;
        public float frequency = 0.4f;

    }
}