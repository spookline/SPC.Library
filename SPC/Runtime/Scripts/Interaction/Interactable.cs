using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spookline.SPC.Interaction {
    public class Interactable {

        public InteractionType type;
        public Collider[] colliders;
        public IInteractablePreProcessor[] preProcessors;
        public InteractAction interactAction;
        public Dictionary<string, object> data = new();
        public Func<bool> isActive = () => true;

        public bool IsValid => colliders is { Length: > 0 } && interactAction != null;

        public bool HasPreProcessors => preProcessors is { Length: > 0 };

        public void Interact(InteractionManager manager) {
            interactAction?.Invoke(manager);
        }

        public bool ContainsCollider(Collider collider) {
            if (!collider || colliders == null) return false;
            return Array.Exists(colliders, c => c == collider);
        }

        public bool HasData(string key) => data.ContainsKey(key);

        public string GetData(string key) => data[key] as string;

        public Func<T> GetDataAsFunc<T>(string key) => data[key] as Func<T>;

        public delegate void InteractAction(InteractionManager manager);

    }


    public enum InteractionType {

        LookAt,
        Proximity

    }
}