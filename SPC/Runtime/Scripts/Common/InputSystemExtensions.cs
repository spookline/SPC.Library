using System;
using Spookline.SPC.Ext;
using UnityEngine.InputSystem;

namespace Spookline.SPC.Common {
    public static class InputSystemExtensions {

        public static void PerformedInput(this ISpookBehaviour behaviour, InputActionReference reference,
            Action<InputAction.CallbackContext> action) {
            var inputAction = reference.ToInputAction();
            inputAction.performed += action;
            behaviour.DisposeOnDestroy(new DisposableInputActionPerformedSubscription(inputAction, action));
        }

        public static void PerformedCanceledInput(this ISpookBehaviour behaviour, InputActionReference reference,
            Action<InputAction.CallbackContext> performed, Action<InputAction.CallbackContext> canceled) {
            var inputAction = reference.ToInputAction();
            inputAction.performed += performed;
            inputAction.canceled += canceled;
            behaviour.DisposeOnDestroy(
                new DisposableInputActionPerformedCanceledSubscription(inputAction, performed, canceled));
        }


        private class DisposableInputActionPerformedSubscription : IDisposable {

            private readonly Action<InputAction.CallbackContext> _action;
            private readonly InputAction _reference;

            public DisposableInputActionPerformedSubscription(InputAction reference,
                Action<InputAction.CallbackContext> action) {
                _reference = reference;
                _action = action;
            }

            public void Dispose() {
                _reference.performed -= _action;
            }

        }

        private class DisposableInputActionPerformedCanceledSubscription : IDisposable {

            private readonly Action<InputAction.CallbackContext> _canceled;
            private readonly Action<InputAction.CallbackContext> _performed;
            private readonly InputAction _reference;

            public DisposableInputActionPerformedCanceledSubscription(InputAction reference,
                Action<InputAction.CallbackContext> performed, Action<InputAction.CallbackContext> canceled) {
                _reference = reference;
                _performed = performed;
                _canceled = canceled;
            }

            public void Dispose() {
                _reference.performed -= _performed;
                _reference.canceled -= _canceled;
            }

        }

    }
}