using HELIX.Extensions;
using HELIX.Widgets;
using Spookline.SPC.UI;
using UnityEditor;

namespace Spookline.SPC.Editor {
    public class SpookConsoleWindow : EditorWindow {

        private void CreateGUI() {
            rootVisualElement.Clear();
            var spcDefault = new SpcDefaultTheme {
                Dark = true,
                IsEditor = true
            }.Stretched();

            spcDefault.Add(
                new WidgetHostElement {
                    Buildable = new SpookConsoleView(isEditor: true).ToBuildable()
                }.Stretched()
            );

            rootVisualElement.Add(spcDefault);
        }

        [MenuItem("Window/Spook Console")]
        public static void ShowWindow() {
            GetWindow<SpookConsoleWindow>("Spook Console");
        }

    }
}