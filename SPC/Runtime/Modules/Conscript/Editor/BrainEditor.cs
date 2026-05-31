using HELIX.Coloring;
using HELIX.Extensions;
using HELIX.Widgets;
using Sirenix.OdinInspector.Editor;
using Spookline.SPC.Conscript;
using Spookline.SPC.Conscript.UI;
using Spookline.SPC.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Conscript.Editor {
    [CustomEditor(typeof(ConscriptBrain))]
    public class BrainEditor : OdinEditor {

        public override VisualElement CreateInspectorGUI() {
            var brain = (ConscriptBrain)target;

            var list = new VisualElement();

            var odinGeneratedContainer = new VisualElement();
            odinGeneratedContainer.Add(new IMGUIContainer(() => { Tree.Draw(false); }));

            list.Add(odinGeneratedContainer);
            Debug.Log("Created base property GUI");

            if (!target) return list;
            var widget = brain.BuildBlackboard();
            list.Add(
                new SpcDefaultTheme {
                    IsEditor = true,
                    SeedColor = Colors.Green
                }.WithAdded(
                    new WidgetHostElement {
                        Buildable = widget.ToBuildable()
                    }
                )
            );
            Debug.Log("Added widget");
            return list;
        }

    }
}