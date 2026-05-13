using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using HELIX.Coloring;
using HELIX.Coloring.Material;
using HELIX.Types;
using HELIX.Widgets;
using HELIX.Widgets.Modifiers;
using HELIX.Widgets.Navigation;
using HELIX.Widgets.Signals;
using HELIX.Widgets.Theming;
using HELIX.Widgets.Universal;
using HELIX.Widgets.Universal.Controllers;
using HELIX.Widgets.Universal.Styles;
using HELIX.Widgets.Universal.Theme;
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

            public override Widget Build(BuildContext context) {
                var colors = PrimitiveColorScheme.From(MaterialColors.Blue, Brightness.Dark);
                var spacing = new PrimitiveSpacingScheme() { factor = 1f };
                return new HThemeProvider(
                        new List<ThemeComponent> {
                            new PrimitiveBaseThemeComponent {
                                colors = colors,
                                spacing = spacing,
                                typography = new PrimitiveTypographyScheme() { factor = 1f },
                                radius = new PrimitiveRadiusScheme() { factor = 1f }
                            }
                        }
                    ) {
                        new SpookConsoleCommandLine()
                    }
                    .Padding(spacing.Space3)
                    .WithModifier(new BackgroundStyleModifier(colors.surface.main))
                    .WithModifier(
                        new TextStyleModifier(
                            new TextStyle() {
                                color = colors.surface.onMain
                            }
                        )
                    );
            }

        }

    }

    public class SpookConsoleCommandLine : StatefulWidget<SpookConsoleCommandLine> {

        public override State<SpookConsoleCommandLine> CreateState() => new State();

        private class State : State<SpookConsoleCommandLine> {

            public TextEditingController controller;
            public GlobalKey consoleKey = new();
            public CommandSystem system;
            public string completionText;
            public string infoText;


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
                    if (result.completionItems.Count == 1) { ReplaceCurrentToken(result.completionItems[0]); } else {
                        completionText = string.Join(" ", result.completionItems.ToArray());
                    }

                    infoText = result.richInfoText ?? result.infoText;
                    completionText = completionText.Trim();
                    SetState();

                    var unityField = consoleKey.Target.Element.Q<TextField>();
                    unityField.textSelection.cursorIndex = controller.Value.Length;
                    unityField.textSelection.selectIndex = controller.Value.Length;
                    return;
                }

                if (obj.Contains("\n")) {
                    var updated = obj.Replace("\n", "").Trim();
                    controller.SetValue(updated);
                    OnSubmitted(updated);
                    return;
                }

                if (obj.EndsWith("?")) {
                    infoText = system.GetHelp(obj.TrimEnd('?').Trim());
                    completionText = "";
                    SetState();
                    return;
                }

                var lateResult = system.Complete(obj);
                infoText = lateResult.richInfoText ?? lateResult.infoText;
                completionText = "";
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
                Debug.Log($"[SpookConsole] >{obj}");
                if (string.IsNullOrWhiteSpace(obj)) return;
                var result = system.Execute(obj);
                if (result.success) { Debug.Log($"[SpookConsole] Success!"); } else {
                    Debug.Log($"[SpookConsole] Failed: {result.error}");
                }

                controller.SetValue("");
                infoText = "";
                completionText = "";
                SetState();
            }


            public override Widget Build(BuildContext context) {
                ModificationBarrier.AddPostFrameCallback(() => {
                        consoleKey.Target.Element.Q<TextElement>().style.unityTextAlign =
                            TextAnchor.MiddleLeft;
                    }
                );

                var style = HTextFieldStyle.DefaultStyleOf(context);
                var typo = PrimitiveBaseTheme.Typography.Get(context);
                var colors = PrimitiveBaseTheme.Colors.Get(context);
                var radius = PrimitiveBaseTheme.Radius.Get(context);
                var spacing = PrimitiveBaseTheme.Spacing.Get(context);
                return new HColumn(crossAxisAlign: Align.Stretch, mainAxisAlign: Justify.FlexEnd) {
                    new HBox(
                        background: new BackgroundStyle {
                            color = colors.surface.container
                        },
                        key: "InfoBox",
                        borderRadius: BorderRadius.Only(topLeft: radius.Radius3, topRight: radius.Radius3)
                    ) {
                        new HColumn(crossAxisAlign: Align.FlexStart) {
                            new HText(infoText, enableRichText: true, key: "InfoText").Caption(context, 2),
                            new HText(completionText, enableRichText: true, key: "CompletionText").Caption(context),
                        }
                    }
                        .Padding(spacing.Space2)
                        .Display(!(string.IsNullOrEmpty(infoText) && string.IsNullOrEmpty(completionText))),
                    new HTextField(key: consoleKey, controller: controller, multiline: true, style: style),
                };
            }

        }

    }
}