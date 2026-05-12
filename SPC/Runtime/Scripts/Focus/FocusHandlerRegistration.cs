using Sirenix.Serialization;
using Spookline.SPC.Ext;
using UnityEngine;

namespace Spookline.SPC.Focus {
    public class FocusHandlerRegistration : SpookBehaviour<FocusHandlerRegistration> {

        [SerializeField, OdinSerialize]
        private IFocusHandler[] handlers;

        [SerializeField]
        private bool dontDestroyOnLoad;

        private void Awake() {
            if (dontDestroyOnLoad) {
                DontDestroyOnLoad(gameObject);
                foreach (var focusHandler in handlers) {
                    FocusManager.Instance.Register(focusHandler);
                }
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            if (dontDestroyOnLoad) return;
            foreach (var focusHandler in handlers) {
                FocusManager.Instance.Register(focusHandler);
            }
        }

        protected override void OnDisable() {
            base.OnDisable();
            if (dontDestroyOnLoad) return;
            foreach (var focusHandler in handlers) {
                FocusManager.Instance.Unregister(focusHandler);
            }
        }

    }
}