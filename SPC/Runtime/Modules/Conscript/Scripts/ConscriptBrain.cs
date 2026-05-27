using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Spookline.SPC.Conscript.Nodes;
using Spookline.SPC.Ext;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Spookline.SPC.Conscript {
    public class ConscriptBrain : SpookBehaviour<ConscriptBrain> {

        [OdinSerialize]
        public IConscriptTree tree;

        [ShowIf("@machine != null"), NonSerialized, ShowInInspector]
        public ConscriptMachine machine;

        [Button, ShowIf("@machine == null")]
        public void Instantiate() {
            try {
                var root = tree.Clone().Tree(this);
                var hierarchy = new ConscriptHierarchy(root);
                machine = new ConscriptMachine(hierarchy);
            } catch (Exception e) { Debug.LogException(e); }
        }

        [Button]
        public void Reset() {
            machine = null;
        }

    }

    public interface IConscriptTree {

        public ConscriptNode Tree(ConscriptBrain brain);

        public IConscriptTree Clone();

    }


    public abstract class ConscriptTree<T> : FlowScope, IConscriptTree where T : ConscriptTree<T> {

        public abstract ConscriptNode Tree(ConscriptBrain brain);

        public virtual IConscriptTree Clone() => (T)MemberwiseClone();

    }

    [Serializable]
    public class ExampleTree : ConscriptTree<ExampleTree> {

        public override ConscriptNode Tree(ConscriptBrain brain) {
            var test = "Hello World!";
            return new Select {
                new Condition {
                    Observers = {
                        [Observe.Guard | Observe.Interrupt] = new SimpleCondition(() => Random.value > 0.5f)
                    },
                    Child = new SimpleCondition(true)
                },
                new Sequence {
                    new WaitNode(4f),
                }
            };
        }

    }
}