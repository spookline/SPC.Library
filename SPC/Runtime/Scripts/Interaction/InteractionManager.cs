using System;
using System.Collections.Generic;
using Spookline.SPC.Events;
using Spookline.SPC.Ext;
using Spookline.SPC.Interaction;
using UnityEngine;

namespace Spookline.SPC.Interaction {
    public sealed class InteractionManager : SpookManagerBehaviour<InteractionManager> {

        public Camera interactionCamera;

        public float lookDistance = 3f;
        public float proximityRadius = 4f;

        public LayerMask interactionLayers;

        public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

        private readonly List<Interactable> _interactables = new();
        private readonly Dictionary<Collider, Interactable> _colliderLookup = new();

        private InteractionContext _activeContext;
        private int _activeProcessorIndex;
        private bool _interactionInputHeld;
        private readonly HashSet<Interactable> _checkedInteractables = new();
        private readonly Collider[] _proximityColliders = new Collider[32];
        private readonly List<VisibilityRay> _visibilityRays = new();

        public struct VisibilityRay {
            public Vector3 origin;
            public Vector3 target;
            public bool isVisible;
        }

        public Interactable CurrentInteractable { get; private set; }

        public Interactable ActiveInteractable { get; private set; }

        public bool IsProcessingInteraction => ActiveInteractable != null;

        public IReadOnlyList<Interactable> RegisteredInteractables => _interactables;

        public IReadOnlyList<VisibilityRay> VisibilityRays => _visibilityRays;

        private void Update() {
            if (!interactionCamera) return;
            UpdateCurrentTarget();
            UpdateActiveInteraction();
        }

        protected override void OnDisable() {
            base.OnDisable();
            _interactionInputHeld = false;
            CancelInteraction();
        }

        public bool RegisterInteractable(Interactable interactable) {
            if (interactable is not { IsValid: true } || _interactables.Contains(interactable)) return false;
            var registeredAtLeastOneCollider = false;
            foreach (var collider in interactable.colliders) {
                if (!collider) continue;
                if (!_colliderLookup.TryAdd(collider, interactable)) continue;
                registeredAtLeastOneCollider = true;
            }

            if (!registeredAtLeastOneCollider) return false;
            _interactables.Add(interactable);
            return true;
        }

        public bool UnregisterInteractable(Interactable interactable) {
            if (interactable == null) return false;
            if (!_interactables.Remove(interactable)) return false;
            if (ActiveInteractable == interactable) {
                CancelInteraction();
            }

            if (interactable.colliders != null) {
                foreach (var collider in interactable.colliders) {
                    if (!collider) continue;
                    _colliderLookup.Remove(collider);
                }
            }

            if (CurrentInteractable == interactable) {
                SetCurrentInteractable(null);
            }

            return true;
        }

        public void Clear() {
            _interactionInputHeld = false;
            CancelInteraction();

            _interactables.Clear();
            _colliderLookup.Clear();
            SetCurrentInteractable(null);
        }

        private Interactable FindBestInteractable() {
            var lookInteractable = FindLookInteractable();
            return lookInteractable ?? FindClosestProximityInteractable();
        }

        private void UpdateCurrentTarget() {
            var newTarget = FindBestInteractable();
            if (newTarget == CurrentInteractable) return;
            SetCurrentInteractable(newTarget);
            if (IsProcessingInteraction && ActiveInteractable != CurrentInteractable) {
                CancelInteraction();
            }
        }

        private void ClearActiveInteraction() {
            ActiveInteractable = null;
            _activeContext = null;
            _activeProcessorIndex = 0;
        }

        public void SetCurrentInteractable(Interactable interactable) {
            if (CurrentInteractable == interactable) return;
            CurrentInteractable = interactable;
            new InteractionTargetChangedEvt { Interactable = interactable }.Raise();
        }

        public void NotifyInteractionPressed() {
            if (_interactionInputHeld) return;
            _interactionInputHeld = true;
            BeginInteraction();
        }

        public void NotifyInteractionReleased() {
            if (!_interactionInputHeld) return;
            _interactionInputHeld = false;
            if (IsProcessingInteraction) {
                CancelInteraction();
            }
        }

        public bool BeginInteraction() {
            if (IsProcessingInteraction) {
                CancelInteraction();
            }

            var target = CurrentInteractable ?? FindBestInteractable();
            if (target == null) return false;

            // Interaction started evt
            if (!target.HasPreProcessors) {
                return InvokeInteraction(target);
            }

            ActiveInteractable = target;
            _activeContext = new InteractionContext(this, target);
            _activeProcessorIndex = 0;
            return BeginCurrentPreProcessor();
        }

        public void CancelInteraction() {
            if (!IsProcessingInteraction) return;
            var cancelledInteractable = ActiveInteractable;
            var currentPreProcessor = GetCurrentPreProcessor();
            if (currentPreProcessor != null) {
                try {
                    currentPreProcessor.Cancel(_activeContext);
                } catch (Exception e) {
                    Debug.LogException(e, this);
                }
            }

            ResetProProcessors(cancelledInteractable);
            ClearActiveInteraction();

            new InteractionCancelledEvt { Interactable = cancelledInteractable }.Raise();
        }

        private void UpdateActiveInteraction() {
            if (!IsProcessingInteraction) return;
            if (!_interactionInputHeld) {
                CancelInteraction();
                return;
            }

            if (CurrentInteractable != ActiveInteractable) {
                CancelInteraction();
                return;
            }

            ProcessCurrentPreProcessor(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void ProcessCurrentPreProcessor(float deltaTime, float unscaledDeltaTime) {
            var currentPreProcessor = GetCurrentPreProcessor();
            if (currentPreProcessor == null) {
                AdvanceToNextPreProcessor();
                return;
            }

            _activeContext.SetFrameData(deltaTime, unscaledDeltaTime, _interactionInputHeld);
            InteractionProcessResult result;
            try {
                result = currentPreProcessor.Process(_activeContext);
            } catch (Exception exception) {
                Debug.LogException(exception, this);
                CancelInteraction();
                return;
            }

            switch (result) {
                case InteractionProcessResult.Running:
                    break;
                case InteractionProcessResult.Completed:
                    currentPreProcessor.Reset();
                    AdvanceToNextPreProcessor();
                    break;
                case InteractionProcessResult.Rejected:
                    CancelInteraction();
                    break;
                default:
                    CancelInteraction();
                    break;
            }
        }

        private bool BeginCurrentPreProcessor() {
            while (IsProcessingInteraction) {
                var preProcessor = GetCurrentPreProcessor();
                if (preProcessor == null) {
                    _activeProcessorIndex++;
                    continue;
                }

                preProcessor.Reset();
                try {
                    if (!preProcessor.CanBegin(_activeContext)) {
                        CancelInteraction();
                        return false;
                    }

                    preProcessor.Begin(_activeContext);
                    return true;
                } catch (Exception exception) {
                    Debug.LogException(exception, this);
                    CancelInteraction();
                    return false;
                }
            }

            return false;
        }

        private void AdvanceToNextPreProcessor() {
            if (!IsProcessingInteraction) return;
            _activeProcessorIndex++;
            if (_activeProcessorIndex < ActiveInteractable.preProcessors.Length) {
                BeginCurrentPreProcessor();
                return;
            }

            CompleteActiveInteraction();
        }

        private void CompleteActiveInteraction() {
            var completedInteractable = ActiveInteractable;

            ResetProProcessors(completedInteractable);
            ClearActiveInteraction();

            InvokeInteraction(completedInteractable);
        }

        private bool InvokeInteraction(Interactable interactable) {
            if (interactable == null) return false;
            try {
                interactable.Interact(this);
                new InteractEvt { Interactable = interactable }.Raise();
                return true;
            } catch (Exception ex) {
                Debug.LogException(ex, this);
                return false;
            }
        }

        public void MarkDirty() {
            if (CurrentInteractable == null) return;
            new InteractionTargetChangedEvt {
                Interactable = CurrentInteractable
            }.Raise();
        }

        private IInteractablePreProcessor GetCurrentPreProcessor() {
            if (ActiveInteractable?.preProcessors == null) return null;
            if (_activeProcessorIndex < 0 || _activeProcessorIndex >= ActiveInteractable.preProcessors.Length)
                return null;
            return ActiveInteractable.preProcessors[_activeProcessorIndex];
        }

        private void ResetProProcessors(Interactable interactable) {
            if (interactable?.preProcessors == null) return;
            foreach (var preProcessor in interactable.preProcessors) {
                preProcessor?.Reset();
            }
        }

        private Interactable FindLookInteractable() {
            var cameraTransform = interactionCamera.transform;
            var ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (!Physics.Raycast(ray, out var hit, lookDistance, interactionLayers, triggerInteraction)) return null;
            if (!_colliderLookup.TryGetValue(hit.collider, out var interactable)) return null;
            return interactable.type == InteractionType.LookAt && interactable.isActive() ? interactable : null;
        }

        private Interactable FindClosestProximityInteractable() {
            var origin = interactionCamera.transform.position;
            _visibilityRays.Clear();

            var hits = Physics.OverlapSphereNonAlloc(origin, proximityRadius, _proximityColliders, interactionLayers,
                triggerInteraction);
            _checkedInteractables.Clear();

            Interactable closestInteractable = null;
            var closestDistanceSquared = float.MaxValue;
            for (var i = 0; i < hits; i++) {
                var nearbyCollider = _proximityColliders[i];
                if (!nearbyCollider) continue;
                if (!_colliderLookup.TryGetValue(nearbyCollider, out var interactable)) continue;
                if (interactable.type != InteractionType.Proximity) continue;
                if (interactable.isActive == null || !interactable.isActive()) continue;
                if (!_checkedInteractables.Add(interactable)) continue;
                if (!HasVisibilityTo(interactable, origin)) continue;
                var interactableDistanceSquared = GetClosestDistanceSquared(interactable, origin);
                if (interactableDistanceSquared >= closestDistanceSquared) continue;
                closestDistanceSquared = interactableDistanceSquared;
                closestInteractable = interactable;
            }
            return closestInteractable;
        }

        private bool HasVisibilityTo(Interactable interactable, Vector3 origin) {
            if (interactable?.colliders == null) return false;

            const float raycastPadding = 0.01f;

            foreach (var collider in interactable.colliders) {
                if (!collider || !collider.enabled || !collider.gameObject.activeInHierarchy) continue;

                var closestPoint = collider.ClosestPoint(origin);
                var direction = closestPoint - origin;
                var distance = direction.magnitude;
                if (distance <= Mathf.Epsilon) {
                    _visibilityRays.Add(new VisibilityRay { origin = origin, target = closestPoint, isVisible = true });
                    return true;
                }

                var isVisible = Physics.Raycast(origin, direction / distance, out var hit, distance + raycastPadding,
                    interactionLayers, triggerInteraction) && interactable.ContainsCollider(hit.collider);
                _visibilityRays.Add(new VisibilityRay { origin = origin, target = closestPoint, isVisible = isVisible });
                if (isVisible) return true;
            }

            return false;
        }

        private static float GetClosestDistanceSquared(Interactable interactable, Vector3 origin) {
            if (interactable?.colliders == null) return float.MaxValue;

            var closestDistanceSquared = float.MaxValue;

            foreach (var collider in interactable.colliders) {
                if (!collider || !collider.enabled || !collider.gameObject.activeInHierarchy) continue;
                var closestPoint = collider.ClosestPoint(origin);
                var distanceSquared = (closestPoint - origin).sqrMagnitude;
                if (distanceSquared < closestDistanceSquared) {
                    closestDistanceSquared = distanceSquared;
                }
            }

            return closestDistanceSquared;
        }

    }
}

public struct InteractEvt : Evt<InteractEvt> {

    public Interactable Interactable { get; set; }

}

public struct InteractionTargetChangedEvt : Evt<InteractionTargetChangedEvt> {

    public Interactable Interactable { get; set; }

}

public struct InteractionCancelledEvt : Evt<InteractionCancelledEvt> {

    public Interactable Interactable { get; set; }

}