using System.Collections.Generic;
using Sirenix.Serialization;
using Spookline.SPC.Common;
using Spookline.SPC.Ext;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Spookline.SPC.Focus {
    [CreateAssetMenu(fileName = "SpookFocusModule", menuName = "Modules/SpookFocusModule")]
    public class FocusModule : Module<FocusModule> {

        [OdinSerialize]
        public List<IFocusListener> listeners = new();

        public InputActionReference cancelAction;
        public InputActionReference menuAction;
        public InputActionReference escapeAction;

        public override void Load() {
            base.Load();

            var globalsObject = Globals.Instance.gameObject;
            var manager = globalsObject.GetOrAddComponent<FocusManager>();

            if (escapeAction) manager.PerformedInput(escapeAction, _ => manager.SendAmbiguousEscapeInput());
            if (menuAction) manager.PerformedInput(menuAction, ctx => manager.SendMenuInput());
            if (cancelAction) manager.PerformedInput(cancelAction, ctx => manager.SendCancelInput());

            On<FocusChangedEvt>().Do(DefaultOnFocusChanged);
        }

        private void DefaultOnFocusChanged(ref FocusChangedEvt evt) {
            if (evt.HasFlag((int)DefaultFocusFlags.EnableCursor)) {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            } else {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            foreach (var listener in listeners) listener.OnFocusChanged(evt);
        }

    }
}