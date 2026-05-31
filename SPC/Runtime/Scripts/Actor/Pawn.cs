using System;
using Sirenix.OdinInspector;
using Spookline.SPC.Common;
using Spookline.SPC.Events;
using Spookline.SPC.Ext;
using UnityEngine;

namespace Spookline.SPC.Actor {

    [HideMonoScript]
    [AddComponentMenu("AI/Pawn")]
    public sealed class Pawn : AttachmentSpookBehaviour<Pawn, IPawnAttachment> {
        
        public Transform eyeTransform;
        public Transform mainTransform;

        /// <summary>
        /// Instance locale identifier of the current pawn.
        /// This will not change throughout the lifetime of the pawn.
        /// </summary>
        [NonSerialized]
        public ulong pawnId;

        public IPossessor Possessor { get; private set; }

        protected override void Awake() {
            base.Awake();
            pawnId = IdGenerator.NextId();
        }

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