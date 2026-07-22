using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spookline.SPC.Interaction {
    public sealed class InteractableBuilder {

        private InteractionType _interactionType =
            InteractionType.LookAt;

        private readonly List<Collider> _colliders = new();

        private readonly List<IInteractablePreProcessor> _preProcessors = new();
        private Dictionary<string, object> _data = new();

        private Func<bool> _isActive = () => true;
        private Interactable.InteractAction _interactAction;

        private InteractableBuilder() { }

        public static InteractableBuilder Create() {
            return new InteractableBuilder();
        }
        
        public InteractableBuilder WithIsActive(Func<bool> active) {
            _isActive = active;
            return this;
        }
        
        public InteractableBuilder WithData(Dictionary<string, object> data) {
            _data = data;
            return this;
        }
        
        public InteractableBuilder WithData(string key, object value) {
            if (string.IsNullOrEmpty(key)) return this;
            _data[key] = value;
            return this;
        }

        public InteractableBuilder WithInteractionType(
            InteractionType type) {
            _interactionType = type;
            return this;
        }

        public InteractableBuilder WithCollider(Collider targetCollider) {
            if (targetCollider &&
                !_colliders.Contains(targetCollider)) {
                _colliders.Add(targetCollider);
            }

            return this;
        }

        public InteractableBuilder WithColliders(
            params Collider[] targetColliders) {
            if (targetColliders == null) return this;
            foreach (var targetCollider in targetColliders)
                WithCollider(targetCollider);
            return this;
        }

        public InteractableBuilder WithCollidersFrom(
            GameObject target,
            bool includeChildren = true) {
            var foundColliders = includeChildren
                ? target.GetComponentsInChildren<Collider>(true)
                : target.GetComponents<Collider>();
            return WithColliders(foundColliders);
        }

        public InteractableBuilder WithPreProcessor(
            IInteractablePreProcessor preProcessor) {
            if (preProcessor == null) {
                throw new ArgumentNullException(
                    nameof(preProcessor));
            }
            _preProcessors.Add(preProcessor);
            return this;
        }

        public InteractableBuilder RequireHold(float duration, bool useUnscaledTime = false) {
            return WithPreProcessor(new InteractableHoldPreProcessor(() => duration, useUnscaledTime));
        }
        
        public InteractableBuilder RequireHold(Func<float> duration, bool useUnscaledTime = false) {
            return WithPreProcessor(new InteractableHoldPreProcessor(duration, useUnscaledTime));
        }


        /// <summary>
        /// Return value of the delegate determines whether the interactable should be 
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public InteractableBuilder OnInteract(Interactable.InteractAction action) {
            _interactAction = action;
            return this;
        }
        
        public Interactable Register() {
            var interactable = new Interactable {
                type = _interactionType,
                colliders = _colliders.ToArray(),
                preProcessors = _preProcessors.ToArray(),
                interactAction = _interactAction,
                data = _data,
                isActive = _isActive
            };
            if (InteractionManager.Instance.RegisterInteractable(interactable)) return interactable;
            Debug.LogError("The interactable could not be registered.");
            return null;
        }

    }
}