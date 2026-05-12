using System;

namespace Spookline.SPC.Scripts.Cameras
{
    [Serializable]
    public class UnityCameraProvider : FovManager.ICameraProvider {

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