using System;

namespace Spookline.SPC.Cameras
{
    [Serializable]
    public class FovUnityCameraProvider : FovManager.ICameraProvider {

        public UnityEngine.Camera camera;

        public float BaseFov {
            get {
                if (_baseFov < 0f) _baseFov = camera.fieldOfView;
                return _baseFov;
            }
        }

        public float Fov {
            get => camera.fieldOfView;
            set => camera.fieldOfView = value;
        }

        private float _baseFov = -1f;

    }
}