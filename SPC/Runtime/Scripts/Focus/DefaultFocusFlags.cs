using System;
using UnityEngine;

namespace Spookline.SPC.Focus {
    // Flags are always organized so that flags=0 represents the default in-game behavior
    public static class DefaultFocusFlags {

        public const int None = 0;
        public const int EnableCursor = 1 << 0;
        public const int DisableMovement = 1 << 1;
        public const int DisableLook = 1 << 2;
        public const int BlockInputActions = 1 << 3;
        public const int EscapeCancelable = 1 << 4;

    }
}