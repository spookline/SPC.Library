using System;
using UnityEngine.InputSystem;

namespace Spookline.SPC.Focus {
    [Serializable]
    public class InputActionDisableFocusListener : IFocusListener {

        public int flagBit = 1;
        public InputActionReference[] references = Array.Empty<InputActionReference>();

        public void OnFocusChanged(FocusChangedEvt evt) {
            var flagMask = (1 << flagBit);
            if (evt.HasFlag(flagMask)) {
                foreach (var reference in references) {
                    reference.action?.Disable();
                }
            } else {
                foreach (var reference in references) {
                    reference.action?.Enable();
                }
            }
        }

    }
}