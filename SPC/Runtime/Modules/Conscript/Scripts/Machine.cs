using Sirenix.OdinInspector;
using UnityEngine;

namespace Spookline.SPC.Conscript {
    public class ConscriptMachine {

        public ConscriptHierarchy Hierarchy { get; }

        public ConscriptMachine(ConscriptHierarchy hierarchy) {
            Hierarchy = hierarchy;
        }

        [ShowInInspector, TextArea(10, 9999)]
        public string State => Hierarchy?.ToStringDeep() ?? "No machine";

        [Button]
        public void Tick() {
            Hierarchy?.Tick();
        }
    }
}