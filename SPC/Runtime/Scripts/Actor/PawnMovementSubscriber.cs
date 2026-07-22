using Spookline.SPC.Ext;

namespace Spookline.SPC.Actor {
    public class PawnMovementStateSubscriber<T> where T : SpookBehaviour<T> {

        public bool IsStateAvailable => _movementStateAttachment is { HasValue: true };
        public IMovementStateAttachment State => _movementStateAttachment.Value;

        private AttachmentAccessor<IMovementStateAttachment> _movementStateAttachment;
        private readonly Condition _condition;
        private readonly SpookBehaviour<T> _behaviour;

        public PawnMovementStateSubscriber(SpookBehaviour<T> behaviour, Condition condition = null) {
            _behaviour = behaviour;
            _condition = condition;
        }

        public void Subscribe() {
            _behaviour.On<PawnPossessedEvt>().Do(evt => {
                var condition = _condition?.Invoke(evt.Pawn) ?? true;
                if (!condition) return;
                _movementStateAttachment = evt.Pawn.GetAccessor<IMovementStateAttachment>();
            });
            _behaviour.On<PawnExorcisedEvt>().Do(evt => {
                var condition = _condition?.Invoke(evt.Pawn) ?? true;
                if (evt.Pawn.HasAttachment<IMovementStateAttachment>() && condition) {
                    _movementStateAttachment = null;
                }
            });
        }

        public delegate bool Condition(Pawn pawn);

    }

    public static class PawnMovementSubscriberExtensions {

        public static PawnMovementStateSubscriber<T> SubscribeToPawnMovementState<T>(this SpookBehaviour<T> behaviour, PawnMovementStateSubscriber<T>.Condition condition = null)
            where T : SpookBehaviour<T> {
            var subscriber = new PawnMovementStateSubscriber<T>(behaviour, condition);
            subscriber.Subscribe();
            return subscriber;
        }
    }
}