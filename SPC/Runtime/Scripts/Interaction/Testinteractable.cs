using Spookline.SPC.Ext;
using UnityEngine;

namespace Spookline.SPC.Interaction {
    public class Testinteractable : SpookBehaviour<Testinteractable> {

        protected override void Start() {
            base.Start();
            InteractableBuilder.Create()
                .WithInteractionType(InteractionType.LookAt)
                .OnInteract(() => Debug.Log("Interacted"))
                .WithCollidersFrom(gameObject)
                .RequireHold(3f)
                .Register();
        }

    }
}