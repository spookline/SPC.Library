using System.Collections.Generic;
using System.Linq;
using HELIX.Widgets;
using HELIX.Widgets.Signals;
using HELIX.Widgets.Universal;
using HELIX.Widgets.Universal.Controllers;
using HELIX.Widgets.Utilities;
using Spookline.SPC;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.UI {
    public class SpookConsoleView : StatefulWidget<SpookConsoleView> {

        public readonly bool isEditor;

        public SpookConsoleView(
            bool isEditor = false,
            Key key = default,
            object[] constants = null,
            IReadOnlyCollection<Modifier> modifiers = null
        ) : base(key, constants, modifiers) {
            this.isEditor = isEditor;
        }

        public override State<SpookConsoleView> CreateState() => new State();

        private class State : State<SpookConsoleView> {

            public TextEditingController controller;
            public GlobalKey consoleKey = new();
            public CommandSystem system;
            public string completionText;


            public override void InitState() {
                controller = AddDisposable(new TextEditingController());
                controller.onChanged += OnChanged;

                system = new CommandSystem();
                system.Register(new SpawnCommand());
            }

            private void OnChanged(string obj) {
                if (obj.Contains("\t")) {
                    var updated = obj.Replace("\t", "");
                    controller.SetValue(updated);

                    completionText = "";
                    var result = system.Complete(updated);
                    if (result.CompletionItems.Count == 1) {
                        ReplaceCurrentToken(result.CompletionItems[0]);
                    } else { completionText = string.Join(" ", result.CompletionItems.ToArray()); }

                    var info = result.RichInfoText ?? result.InfoText;
                    if (info != null) {
                        completionText = completionText.TrimEnd() + "\n" + info;
                    }

                    completionText = completionText.Trim();
                    SetState();

                    var unityField = consoleKey.Target.Element.Q<TextField>();
                    unityField.textSelection.cursorIndex = controller.Value.Length;
                    unityField.textSelection.selectIndex = controller.Value.Length;

                    Debug.Log("[SpookConsole] Complete Tab");
                    return;
                }

                if (obj.Contains("\n")) {
                    OnSubmitted(obj.Replace("\n", "").Trim());
                    controller.SetValue("");
                    return;
                }

                var lateResult = system.Complete(obj);
                completionText = lateResult.RichInfoText ?? lateResult.InfoText;
                SetState();
            }

            public void ReplaceCurrentToken(string completion) {
                var input = controller.Value;
                var lastSpace = input.LastIndexOf(' ');

                if (lastSpace < 0) controller.SetValue(completion + " ");
                else controller.SetValue(input[..(lastSpace + 1)] + completion + " ");

                // var unityField = consoleKey.Target.Element.Q<TextField>();
                // unityField.textSelection.cursorIndex = controller.Value.Length;
                // unityField.textSelection.selectIndex = controller.Value.Length;
            }


            private void OnSubmitted(string obj) {
                if (string.IsNullOrWhiteSpace(obj)) return;
                Debug.Log($"[SpookConsole] >{obj}");
                var result = system.Execute(obj);
                if (result.Success) {
                    Debug.Log($"[SpookConsole] Success!");
                } else {
                    Debug.Log($"[SpookConsole] Failed: {result.Error}");
                }

                // controller.SetValue("");
                // mount.Element.schedule.Execute(() => {
                //     consoleKey.Focus();
                // }).ExecuteLater(10);
            }


            public override Widget Build(BuildContext context) {
                return new HColumn(crossAxisAlign: Align.Stretch) {
                    new HText(completionText, enableRichText: true),
                    new HTextField(key: consoleKey, controller: controller, multiline: true)
                };
            }

        }

    }
}