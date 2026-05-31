using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Spookline.SPC.Actor;
using Spookline.SPC.Conscript.Nodes;
using Spookline.SPC.Conscript.UI;
using Spookline.SPC.Ext;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Spookline.SPC.Conscript {
    [RequireComponent(typeof(Pawn))]
    public class ConscriptBrain : SpookBehaviour<ConscriptBrain>, IPawnAttachment {

        [NonSerialized, OdinSerialize]
        public IConscriptTree tree;

        [ShowIf("@machine != null"), NonSerialized, ShowInInspector]
        public ConscriptMachine machine;

        [NonSerialized]
        public Pawn pawn;

        private void Awake() {
            pawn = GetComponent<Pawn>();
        }

        [Button, ShowIf("@machine == null")]
        public void Instantiate() {
            try {
                pawn = GetComponent<Pawn>();
                tree.Declare(this);
                var root = tree.Tree(this);
                var hierarchy = new ConscriptMachine(root);
                machine = hierarchy;
            } catch (Exception e) { Debug.LogException(e); }
        }

        [Button]
        public void Reset() {
            machine = null;
        }

        public GraphBlackboard BuildBlackboard() {
            return new GraphBlackboard(this);
        }

    }

    public interface IConscriptTree {

        public void Declare(ConscriptBrain brain);

        public ConscriptNode Tree(ConscriptBrain brain);

    }


    public abstract class ConscriptTree<T> : FlowScope, IConscriptTree where T : ConscriptTree<T> {

        public virtual void Declare(ConscriptBrain brain) { }

        public abstract ConscriptNode Tree(ConscriptBrain brain);

    }
}