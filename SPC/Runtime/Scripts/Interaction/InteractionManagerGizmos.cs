using System;
using UnityEngine;

namespace Spookline.SPC.Interaction {
    public sealed class InteractionManagerGizmos : MonoBehaviour {

        private InteractionManager _interactionManager;

        private void Awake() {
            _interactionManager = GetComponent<InteractionManager>();
        }

        private void OnDrawGizmosSelected() {
            if (!_interactionManager || !_interactionManager.interactionCamera) return;

            var cameraTransform = _interactionManager.interactionCamera.transform;

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(cameraTransform.position, cameraTransform.forward * _interactionManager.lookDistance);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(cameraTransform.position, _interactionManager.proximityRadius);

            if (_interactionManager.CurrentInteractable != null) {
                Gizmos.color = _interactionManager.IsProcessingInteraction ? Color.red : Color.green;
                DrawInteractableGizmos(_interactionManager.CurrentInteractable);
            }

            if (_interactionManager.IsProcessingInteraction &&
                _interactionManager.ActiveInteractable != _interactionManager.CurrentInteractable) {
                Gizmos.color = Color.red;
                DrawInteractableGizmos(_interactionManager.ActiveInteractable);
            }

            foreach (var visibilityRay in _interactionManager.VisibilityRays) {
                Gizmos.color = visibilityRay.isVisible ? Color.green : Color.red;
                Gizmos.DrawLine(visibilityRay.origin, visibilityRay.target);
                Gizmos.DrawSphere(visibilityRay.target, 0.04f);
            }
        }

        private static void DrawInteractableGizmos(Interactable interactable) {
            if (interactable?.colliders == null) return;
            foreach (var collider in interactable.colliders) {
                if (!collider || !collider.enabled || !collider.gameObject.activeInHierarchy) continue;
                Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
            }
        }
    }
}