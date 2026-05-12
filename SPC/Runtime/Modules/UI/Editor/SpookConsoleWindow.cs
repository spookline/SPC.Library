using HELIX.Widgets;
using Spookline.SPC.UI;
using UnityEditor;

namespace Spookline.SPC.Editor {
    public class SpookConsoleWindow : EditorWindow {

        [MenuItem("Window/Spook Console")]
        public static void ShowWindow() {
            GetWindow<SpookConsoleWindow>("Spook Console");
        }

        private void CreateGUI() {
            rootVisualElement.Clear();
            rootVisualElement.Add(new WidgetHostElement {
                Buildable = new SpookConsoleView(isEditor: true).ToBuildable()
            });
        }

    }
}