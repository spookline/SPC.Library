using Spookline.SPC.Events;
using Spookline.SPC.Ext;
using UnityEngine;

namespace Spookline.SPC.Actor {
    
    public sealed class Pawn : AttachmentSpookBehaviour<Pawn, IPawnAttachment> {
        
        public Transform eyeTransform;
        public Transform mainTransform;

        public IPossessor Possessor { get; private set; }

        public void OnPossessed(IPossessor owner) {
            Possessor = owner;
            var evt = new PawnPossessedEvt {
                Pawn = this,
                Possessor = owner
            };
            evt.Raise();
        }

        public void OnExorcised() {
            new PawnExorcisedEvt {
                Pawn = this,
                Possessor = Possessor
            }.Raise();
            Possessor = null;
        }

    }
    
    public interface IPawnAttachment : IAttachment {
        
        
    }

    public interface IPossessor {

        public Pawn Possessed { get; }
        public void Possess(Pawn pawnToPossess);
        public void Exorcise();

    }

    public abstract class PawnEvt<T> : Evt<T> where T : PawnEvt<T> {

        public Pawn Pawn { get; set; }
        public IPossessor Possessor { get; set; }

    }

    public class PawnPossessedEvt : PawnEvt<PawnPossessedEvt> {

        public bool IsCancelled { get; set; } = false;

    }

    public class PawnExorcisedEvt : PawnEvt<PawnExorcisedEvt> { }
}