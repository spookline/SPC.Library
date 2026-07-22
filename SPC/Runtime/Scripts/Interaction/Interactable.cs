using System;
using UnityEngine;

namespace Spookline.SPC.Interaction {
    public class Interactable {

        public InteractionType type;
        public Collider[] colliders;
        public IInteractablePreProcessor[] preProcessors;
        public Action interactAction;
        
        public bool IsValid => colliders is { Length: > 0 } && interactAction != null;
        
        public bool HasPreProcessors => preProcessors is { Length: > 0 };
        
        public void Interact() {
            interactAction?.Invoke();
        }

        public bool ContainsCollider(Collider collider) {
            if(!collider || colliders == null) return false;
            return Array.Exists(colliders, c => c == collider);
        }

    }

    
    
    public enum InteractionType {
        LookAt,
        Proximity
    }
    
}