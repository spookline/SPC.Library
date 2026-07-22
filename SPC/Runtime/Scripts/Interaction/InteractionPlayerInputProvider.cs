using Spookline.SPC.Common;
using Spookline.SPC.Ext;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Spookline.SPC.Interaction {
    [RequireComponent(typeof(InteractionManager))]
    public class InteractionPlayerInputProvider : SpookBehaviour<InteractionPlayerInputProvider> {
        
        [SerializeField]
        private InputActionReference input;

        private InteractionManager _interactionManager;

        private void Awake() {
            _interactionManager = GetComponent<InteractionManager>();
            Ext.PerformedCanceledInput(input, _ => _interactionManager.NotifyInteractionPressed(), _ => _interactionManager.NotifyInteractionReleased());
        }

    }
}