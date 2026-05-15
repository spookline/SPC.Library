using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using HELIX.Types;
using HELIX.Widgets;
using HELIX.Widgets.Diagnostics;
using HELIX.Widgets.Modifiers;
using HELIX.Widgets.Universal;
using HELIX.Widgets.Universal.Controllers;
using HELIX.Widgets.Universal.Styles;
using HELIX.Widgets.Universal.Theme;
using Spookline.SPC.Console;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.UI {
  public class SpookConsoleCommandLine : StatefulWidget<SpookConsoleCommandLine> {

    public CommandInfoRichTextStyle style;
    public GlobalKey cmdTextKey;

    public SpookConsoleCommandLine(
      GlobalKey cmdTextKey,
      CommandInfoRichTextStyle style,
      Key key = default,
      object[] constants = null,
      IReadOnlyCollection<Modifier> modifiers = null
    ) : base(key, constants, modifiers) {
      this.style = style;
      this.cmdTextKey = cmdTextKey;
    }


    public override State<SpookConsoleCommandLine> CreateState() => new State();

    private class State : State<SpookConsoleCommandLine> {

      public TextEditingController controller;
      public string completionText;
      public string infoText;
      public bool isExecuting;
      public int historyIndex = -1;


      public override void InitState() {
        controller = AddDisposable(new TextEditingController());
        controller.onChanged += OnChanged;
        controller.onBeginEditing += () => { CommandSystem.Instance.Refresh(); };

        mount.Element.RegisterCallback<KeyDownEvent>(
          evt => {
            if (evt.keyCode == KeyCode.DownArrow) {
              var newIndex = Mathf.Max(historyIndex - 1, -1);
              if (newIndex == historyIndex || historyIndex < 0) return;
              historyIndex = newIndex;
              HistoryChanged();
              evt.StopPropagation();
            } else if (evt.keyCode == KeyCode.UpArrow) {
              if (controller.Value.Length > 0 && historyIndex < 0) return;
              historyIndex = Mathf.Min(historyIndex + 1, SpookConsoleHistoryObserver.Instance.history.Count - 1);
              HistoryChanged();
              evt.StopPropagation();
            }
          },
          TrickleDown.TrickleDown
        );

        CommandSystem.Instance.Refresh(); // Force refresh of the command system.
      }

      public void HistoryChanged() {
        if (historyIndex < 0) { controller.SetValue(""); } else {
          var history = SpookConsoleHistoryObserver.Instance.history;
          historyIndex = Mathf.Min(historyIndex, history.Count - 1);
          controller.SetValue(historyIndex < 0 ? "" : history[historyIndex]);
        }

        SelectEnd();
      }

      public void SelectEnd() {
        var unityField = widget.cmdTextKey.Target.Element.Q<TextField>();
        unityField.textSelection.cursorIndex = controller.Value.Length;
        unityField.textSelection.selectIndex = controller.Value.Length;
      }

      private void OnChanged(string obj) {
        if (isExecuting) return;
        var system = CommandSystem.Instance;
        if (obj.Contains("\t")) {
          var updated = obj.Replace("\t", "");
          controller.SetValue(updated);

          completionText = "";
          var result = system.Complete(updated, widget.style);
          if (result.completionItems.Count == 1) { ReplaceCurrentToken(result.completionItems[0]); } else {
            completionText = string.Join(" ", result.completionItems.ToArray());
          }

          infoText = result.richInfoText ?? "";
          completionText = completionText.Trim();
          SetState();
          SelectEnd();
          return;
        }

        if (obj.Contains("\n")) {
          var updated = obj.Replace("\n", "").Trim();
          controller.SetValue(updated);
          OnSubmitted(updated).Forget();
          return;
        }

        if (obj.EndsWith("?")) {
          infoText = system.GetHelp(obj.TrimEnd('?').Trim());
          completionText = "";
          SetState();
          return;
        }

        var lateResult = system.Complete(obj, widget.style);
        infoText = lateResult.richInfoText ?? "";
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


      private async UniTaskVoid OnSubmitted(string obj) {
        if (isExecuting || string.IsNullOrWhiteSpace(obj)) return;
        isExecuting = true;
        var system = CommandSystem.Instance;
        try {
          LogHistoryBuffer.Instance.Add(
            new ExtendedLogEntry {
              type = ExtLogType.Input,
              summary = obj,
              message = obj
            }
          );
          SpookConsoleHistoryObserver.Instance.AddToHistory(obj.Trim());

          controller.SetValue("");
          infoText = "";
          completionText = "";
          historyIndex = -1;
          SetState();

          CommandResult result;
          try {
            result = await system.Execute(obj).Timeout(TimeSpan.FromSeconds(5)); //
          } catch (TimeoutException) {
            result = CommandResult.Failed("Command timed out after 5 seconds."); //
          } catch (Exception e) {
            result = CommandResult.Failed(e); //
          }

          if (result.success) {
            if (result.hasMessage) {
              LogHistoryBuffer.Instance.Add(
                new ExtendedLogEntry(result.message.ToStringNullable()) {
                  type = ExtLogType.Log,
                }
              );
            }
          } else {
            LogHistoryBuffer.Instance.Add(
              new ExtendedLogEntry(result.message.ToStringNullable()) { type = ExtLogType.Error }
            );
          }
        } finally { isExecuting = false; }

        SetState();
      }


      public override Widget Build(BuildContext context) {
        ModificationBarrier.AddPostFrameCallback(() => {
            widget.cmdTextKey.Target.Element.Q<TextElement>().style.unityTextAlign =
              TextAnchor.MiddleLeft;
          }
        );

        var style = HTextFieldStyle.DefaultStyleOf(context);
        var typo = PrimitiveBaseTheme.Typography.Get(context);
        var colors = PrimitiveBaseTheme.Colors.Get(context);
        var radius = PrimitiveBaseTheme.Radius.Get(context);
        var spacing = PrimitiveBaseTheme.Spacing.Get(context);
        var textStyleInfo = new TextStyle {
          fontSize = typo.FontSize2,
          color = colors.surface.onMain,
          wrap = WhiteSpace.Normal,
          generator = TextGeneratorType.Standard,
        };

        return new HColumn(crossAxisAlign: Align.Stretch, mainAxisAlign: Justify.FlexEnd) {
          new HBox(
            key: "InfoBox",
            background: new BackgroundStyle { color = colors.surface.container },
            borderRadius: BorderRadius.Only(topLeft: radius.Radius3, topRight: radius.Radius3),
            modifiers: new Modifier[] {
              new PaddingModifier(spacing.Space2),
              new DisplayModifier(
                !(string.IsNullOrEmpty(infoText) && string.IsNullOrEmpty(completionText))
              )
            }
          ) {
            new HColumn(crossAxisAlign: Align.Stretch) {
              new HText(infoText, enableRichText: true, key: "InfoText", style: textStyleInfo),
              new HText(completionText, enableRichText: true, key: "CompletionText").Caption(context),
            }
          },
          new HTextField(
            key: "ConsoleInput",
            focusKey: widget.cmdTextKey,
            controller: controller,
            multiline: true,
            style: style
          )
        };
      }

    }

  }
}