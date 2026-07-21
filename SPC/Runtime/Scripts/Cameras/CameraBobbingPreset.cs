using Unity.Cinemachine;
using UnityEngine;

namespace Spookline.SPC.Cameras {
    [CreateAssetMenu(
        fileName = "Camera Bobbing Preset", menuName = "Camera/Camera Bobbing Preset")]
    public class CameraBobbingPreset : ScriptableObject {

        [Min(0f)]
        public float amplitude = 0.3f;

        [Min(1f)]
        public float frequency = 0.4f;

        [Min(0f)]
        public float transitionDuration = 0.2f;

        public bool useSmoothStep;

        public NoiseSettings noiseSettings;

    }
}