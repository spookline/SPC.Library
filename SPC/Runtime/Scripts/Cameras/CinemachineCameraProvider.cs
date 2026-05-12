using System;
using Unity.Cinemachine;
using UnityEngine;

namespace Spookline.SPC.Scripts.Cameras
{
    [Serializable]
    public class CinemachineCameraProvider : FovManager.ICameraProvider {

        [Tooltip(
            "First camera in the array will be used as the base FOV provider. All cameras should ideally have the same base FOV for consistent results.")]
        public CinemachineCamera[] cameras;

        public float baseFov = 60f;

        public CinemachineCamera BaseCamera => cameras.Length > 0 ? cameras[0] : null;

        public float BaseFov => baseFov;

        public float Fov {
            get => BaseCamera != null ? BaseCamera.Lens.FieldOfView : 60f;
            set {
                foreach (var cam in cameras) {
                    cam.Lens.FieldOfView = value;
                }
            }
        }

    }
}