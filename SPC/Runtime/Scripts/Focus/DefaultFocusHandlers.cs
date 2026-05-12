using UnityEngine;

namespace Spookline.SPC.Focus {
    public enum DefaultFocusFlags : short {

        None = 0,
        EnableCursor = 1 << 0,

    }

    public class CursorFocusHandler : IFocusHandler {

        public void OnFocusGained(FocusContext context) {
            UpdateState(context);
        }

        public void OnFocusLost(FocusContext context) {
            UpdateState(context);
        }

        private static void UpdateState(FocusContext context) {
            if (context.HasFlag((short)DefaultFocusFlags.EnableCursor)) {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            } else {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

    }
}