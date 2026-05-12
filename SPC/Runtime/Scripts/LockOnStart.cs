using UnityEngine;

namespace Spookline.SPC {
    public class LockOnStart : MonoBehaviour {

        private void Start() {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

    }
}