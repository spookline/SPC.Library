using Spookline.SPC.Ext;

namespace Spookline.SPC.Actor {
    public abstract class PawnMovementSubscriber<T> : SpookBehaviour<T> where T : SpookBehaviour<T> {
        
        protected bool IsStateAvailable => _movementStateAttachment is { HasValue: true };
        
        protected IMovementStateAttachment State => _movementStateAttachment.Value;

        private AttachmentAccessor<IMovementStateAttachment> _movementStateAttachment;

        protected void Awake() {
            On<PawnPossessedEvt>().Do(evt => {
                if(!Condition(evt.Pawn)) return;
                _movementStateAttachment = evt.Pawn.GetAccessor<IMovementStateAttachment>();
            });
            On<PawnExorcisedEvt>().Do(evt => {
                if (evt.Pawn.HasAttachment<IMovementStateAttachment>() && Condition(evt.Pawn)) {
                    _movementStateAttachment = null;
                }
            });
        }

        protected virtual bool Condition(Pawn pawn) => true;

    }
}