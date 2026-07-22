using Spookline.SPC.Events;
using UnityEngine;

namespace Spookline.SPC.Interaction {
    public class InteractableHoldPreProcessor : IInteractablePreProcessor {

        private readonly bool _useUnscaledTime;

        public bool IsCompleted { get; private set; }
        public float HoldDuration { get; private set; }

        public float ElapsedTime { get; private set; }

        public float Progress => HoldDuration <= 0f ? 1f : Mathf.Clamp01(ElapsedTime / HoldDuration);

        public InteractableHoldPreProcessor(float holdDuration, bool useUnscaledTime = false) {
            HoldDuration = holdDuration;
            _useUnscaledTime = useUnscaledTime;
        }

        public override void Begin(InteractionContext context) {
            ElapsedTime = 0f;
            IsCompleted = false;
            new InteractionHoldProgressChangedEvt { Progress = 0f }.Raise();
        }

        public override InteractionProcessResult Process(InteractionContext context) {
            if (IsCompleted) return InteractionProcessResult.Completed;
            if (!context.IsInputHeld) return InteractionProcessResult.Rejected;
            var frameTime = _useUnscaledTime ? context.UnscaledDeltaTime : context.DeltaTime;
            ElapsedTime += Mathf.Max(0f, frameTime);
            new InteractionHoldProgressChangedEvt { Progress = Progress }.Raise();
            if (ElapsedTime < HoldDuration) return InteractionProcessResult.Running;

            ElapsedTime = HoldDuration;
            IsCompleted = true;
            new InteractionHoldCompletedEvt().Raise();
            return InteractionProcessResult.Completed;
        }

        public override void Cancel(InteractionContext context) {
            if (!IsCompleted && ElapsedTime > 0f) {
                new InteractionHoldCancelledEvt().Raise();
            }
            Reset();
            new InteractionHoldProgressChangedEvt { Progress = 0f }.Raise();
        }

        public override void Reset() {
            ElapsedTime = 0f;
            IsCompleted = false;
        }
    }

    public struct InteractionHoldProgressChangedEvt : Evt<InteractionHoldProgressChangedEvt> {

        public float Progress { get; set; }

    }
    
    public struct InteractionHoldCompletedEvt : Evt<InteractionHoldCompletedEvt> {
        

    }
    
    public struct InteractionHoldCancelledEvt : Evt<InteractionHoldCancelledEvt> {
        

    }
}