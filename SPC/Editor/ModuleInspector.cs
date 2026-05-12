using Spookline.SPC.Ext;
using UnityEditor;

namespace Spookline.SPC.Editor {
    [CustomEditor(typeof(IModule), true)]
    public class ModuleInspector : UnityEditor.Editor {

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            var module = (IModule)target;
        }

    }
}