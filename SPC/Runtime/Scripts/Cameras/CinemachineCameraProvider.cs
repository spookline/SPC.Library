using System;
using Unity.Cinemachine;

namespace Spookline.SPC.Cameras {
    [Serializable]
    public class CinemachineCameraProvider : FovManager.ICameraProvider {

        public CinemachineCamera[] cameras;

        public float baseFov = 60f;

        public CinemachineCamera BaseCamera => cameras.Length > 0 ? cameras[0] : null;

        public float BaseFov => baseFov;

        public float Fov {
            get => BaseCamera != null ? BaseCamera.Lens.FieldOfView : baseFov;
            set {
                foreach (var cam in cameras) {
                    cam.Lens.FieldOfView = value;
                }
            }
        }

    }
}