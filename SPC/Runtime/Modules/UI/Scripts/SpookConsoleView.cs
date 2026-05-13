using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using HELIX.Coloring;
using HELIX.Coloring.Material;
using HELIX.Extensions;
using HELIX.Types;
using HELIX.Widgets;
using HELIX.Widgets.Diagnostics;
using HELIX.Widgets.Modifiers;
using HELIX.Widgets.Scrolling;
using HELIX.Widgets.Signals;
using HELIX.Widgets.Theming;
using HELIX.Widgets.Universal;
using HELIX.Widgets.Universal.Controllers;
using HELIX.Widgets.Universal.Styles;
using HELIX.Widgets.Universal.Theme;
using Spookline.SPC.Console;
using Spookline.SPC.Events;
using UnityEngine;
using UnityEngine.UI;
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

      public ValueSignal<ConsoleLogEntry?> selectedEntry;

      public override void InitState() {
        selectedEntry = AddDisposable(new ValueSignal<ConsoleLogEntry?>());
      }


      public PrimitiveBaseThemeComponent GetComponent(BuildContext context) {
        if (!widget.isEditor)
          return new PrimitiveBaseThemeComponent() {
            colors = PrimitiveBaseTheme.Colors.Get(context),
            spacing = PrimitiveBaseTheme.Spacing.Get(context),
            typography = PrimitiveBaseTheme.Typography.Get(context),
            radius = PrimitiveBaseTheme.Radius.Get(context),
          };

        var colors = PrimitiveColorScheme.From(MaterialColors.Blue, Brightness.Dark);
        var spacing = new PrimitiveSpacingScheme { factor = 1f };

        return new PrimitiveBaseThemeComponent {
          colors = colors,
          spacing = spacing,
          typography = new PrimitiveTypographyScheme { factor = 1f },
          radius = new PrimitiveRadiusScheme { factor = 1f }
        };
      }

      public override Widget Build(BuildContext context) {
        var component = GetComponent(context);

        var primary = component.colors.value.primary.main;
        var infoColor = Colors.Blue.Harmonize(primary);
        var warningColor = Colors.Yellow.Harmonize(primary);
        var errorColor = Colors.Red.Harmonize(primary);
        var successColor = Colors.Green.Harmonize(primary);
        var weakColor = component.colors.value.surface.onMain.WithOpacity(0.5f);
        var activeColor = component.colors.value.primary.main;

        var consoleStyle = new CommandInfoRichTextStyle {
          weak = weakColor.ToHex(),
          active = activeColor.ToHex(),
          valid = successColor.ToHex(),
          error = errorColor.ToHex(),
        };

        return new HThemeProvider(
          new List<ThemeComponent> { component },
          modifiers: new Modifier[] {
            new PaddingModifier(component.spacing.value.Space3),
            new BackgroundStyleModifier(component.colors.value.surface.main),
            new TextStyleModifier(
              new TextStyle {
                color = component.colors.value.surface.onMain,
                generator = TextGeneratorType.Standard
              }
            )
          }
        ) {
          new HStack {
            new HRow {
              new SpookConsoleHistory(
                infoColor: infoColor,
                successColor: successColor,
                warningColor: warningColor,
                errorColor: errorColor,
                selectedEntry: selectedEntry
              ).Fill(),
              new LogMessageViewer(
                selectedEntry.Value ?? default,
                () => { selectedEntry.Value = null; },
                modifiers: new Modifier[] {
                  new MarginModifier(EdgeInsets.Only(left: 8f)),
                  new DisplayModifier(selectedEntry.Value.HasValue),
                  new SizeModifier(BoxConstraints.Tight(50.Percent(), 100.Percent()))
                }
              )
            }.Positioned(EdgeInsets.Only(0, 0, 0, 32)),
            new SpookConsoleCommandLine(consoleStyle)
              .Positioned(EdgeInsets.Only(bottom: 0, left: 0, right: 0))
          }
        };
      }

    }

  }

  public class SpookConsoleCommandLine : StatefulWidget<SpookConsoleCommandLine> {

    public CommandInfoRichTextStyle style;

    public SpookConsoleCommandLine(
      CommandInfoRichTextStyle style,
      Key key = default,
      object[] constants = null,
      IReadOnlyCollection<Modifier> modifiers = null
    ) : base(key, constants, modifiers) {
      this.style = style;
    }


    public override State<SpookConsoleCommandLine> CreateState() => new State();

    private class State : State<SpookConsoleCommandLine> {

      public TextEditingController controller;
      public GlobalKey consoleKey = new();
      public string completionText;
      public string infoText;
      public bool isExecuting;
      public int historyIndex = -1;


      public override void InitState() {
        controller = AddDisposable(new TextEditingController());
        controller.onChanged += OnChanged;

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
              historyIndex = Mathf.Min(historyIndex + 1, ConsoleHistorySignalObserver.Instance.history.Count - 1);
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
          var history = ConsoleHistorySignalObserver.Instance.history;
          historyIndex = Mathf.Min(historyIndex, history.Count - 1);
          controller.SetValue(historyIndex < 0 ? "" : history[historyIndex]);
        }

        SelectEnd();
      }

      public void SelectEnd() {
        var unityField = consoleKey.Target.Element.Q<TextField>();
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
          ConsoleHistoryBuffer.Instance.Add(
            new ConsoleLogEntry {
              type = ExtLogType.Input,
              summary = obj,
              message = obj
            }
          );
          ConsoleHistorySignalObserver.Instance.AddToHistory(obj.Trim());

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
              ConsoleHistoryBuffer.Instance.Add(
                new ConsoleLogEntry(result.message.ToStringNullable()) {
                  type = ExtLogType.Log,
                }
              );
            }
          } else {
            ConsoleHistoryBuffer.Instance.Add(
              new ConsoleLogEntry(result.message.ToStringNullable()) { type = ExtLogType.Error }
            );
            Debug.LogError($"[SpookConsole] Failed: {result.message.ToStringNullable()}");
          }
        } finally { isExecuting = false; }

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
            key: consoleKey,
            controller: controller,
            multiline: true,
            style: style
          )
        };
      }

    }

  }

  public class SpookConsoleHistory : StatefulWidget<SpookConsoleHistory> {

    public Color infoColor;
    public Color successColor;
    public Color warningColor;
    public Color errorColor;
    public ValueSignal<ConsoleLogEntry?> selectedEntry;

    public SpookConsoleHistory(
      Color infoColor,
      Color successColor,
      Color warningColor,
      Color errorColor,
      ValueSignal<ConsoleLogEntry?> selectedEntry,
      Key key = default,
      object[] constants = null,
      IReadOnlyCollection<Modifier> modifiers = null
    ) : base(key, constants, modifiers) {
      this.infoColor = infoColor;
      this.successColor = successColor;
      this.warningColor = warningColor;
      this.errorColor = errorColor;
      this.selectedEntry = selectedEntry;
    }


    public override State<SpookConsoleHistory> CreateState() => new State();

    private class State : State<SpookConsoleHistory>, ISignalObserver {

      public ScrollController scrollController;

      public override void InitState() {
        scrollController = AddDisposable(new ScrollController());

        var observer = ConsoleHistorySignalObserver.Instance;
        observer.Resubscribe();
        observer.AddObserver(new WeakSignalObserver(this));
      }

      public override void Dispose() {
        ConsoleHistorySignalObserver.Instance.RemoveObserver(this);
      }

      public void OnSignalChanged(Signal signal) {
        SetState();

        mount.Element.schedule
          .Execute(() => scrollController.JumpTo(scrollController.MaxOffset))
          .ExecuteLater(10);
      }

      public override Widget Build(BuildContext context) {
        return new HListView(
          BuildLogMessageEntry,
          ConsoleHistoryBuffer.Instance.messages.Count,
          scrollController: scrollController
        );
      }

      private Widget BuildLogMessageEntry(BuildContext ctx, int i) {
        var entry = ConsoleHistoryBuffer.Instance.messages[i];
        var icon = FaSolidIcons.Info;
        var color = widget.infoColor;
        switch (entry.type) {
          case ExtLogType.Log: break;
          case ExtLogType.Warning:
            icon = FaSolidIcons.TriangleExclamation;
            color = widget.warningColor;
            break;
          case ExtLogType.Error:
            icon = FaSolidIcons.CircleExclamation;
            color = widget.errorColor;
            break;
          case ExtLogType.Assert:
            icon = FaSolidIcons.CircleExclamation;
            color = widget.errorColor;
            break;
          case ExtLogType.Exception:
            icon = FaSolidIcons.Bug;
            color = widget.errorColor;
            break;
          case ExtLogType.Input:
            icon = FaSolidIcons.Terminal;
            color = widget.successColor;
            break;

          default: throw new ArgumentOutOfRangeException();
        }

        return new HRow(
          gap: 8f,
          modifiers: new Modifier[] {
            new TextStyleModifier(
              new TextStyle { generator = TextGeneratorType.Standard, color = color }
            ),
            new ManipulatorModifier(BuildLogClickManipulator(entry)),
            new PaddingModifier(EdgeInsets.Symmetric(4f, 2f)),
          }
        ) {
          new HBox(alignment: Alignment.Center) {
            new HIcon(icon, FaSolidIcons.FontDefinition, color: color).Tight()
          }.Size(16f),
          new HText(entry.summary).Expand().Caption(ctx)
        };
      }

      private Clickable BuildLogClickManipulator(ConsoleLogEntry entry) {
        var manipulator = new Clickable(evt => {
            if (evt is IPointerEvent { ctrlKey: true }) {
              GUIUtility.systemCopyBuffer = entry.GetFullText().Trim();
              return;
            }

            widget.selectedEntry.Value = entry;
          }
        ) {
          activators = {
            new ManipulatorActivationFilter { button = MouseButton.LeftMouse },
            new ManipulatorActivationFilter {
              button = MouseButton.LeftMouse,
              modifiers = EventModifiers.Control
            }
          }
        };
        return manipulator;
      }

    }

  }

  public class LogMessageViewer : StatefulWidget<LogMessageViewer> {

    public ConsoleLogEntry entry;
    public Action onClose;

    public LogMessageViewer(
      ConsoleLogEntry entry,
      Action onClose,
      Key key = default,
      object[] constants = null,
      IReadOnlyCollection<Modifier> modifiers = null
    ) : base(key, constants, modifiers) {
      this.entry = entry;
      this.onClose = onClose;
    }

    public override State<LogMessageViewer> CreateState() => new State();

    private class State : State<LogMessageViewer> {

      public bool wrapLines = false;

      public override Widget Build(BuildContext context) {
        var colors = PrimitiveBaseTheme.Colors.Get(context);
        var radius = PrimitiveBaseTheme.Radius.Get(context);
        var typography = PrimitiveBaseTheme.Typography.Get(context);

        var general = new TextStyle {
          wrap = WhiteSpace.Normal,
          generator = TextGeneratorType.Standard,
        };

        var message = new TextStyle {
          wrap = WhiteSpace.Normal,
          generator = TextGeneratorType.Standard,
          fontSize = typography.FontSize1
        };

        var wrapped = new TextStyle {
          wrap = wrapLines ? WhiteSpace.Normal : WhiteSpace.NoWrap,
          generator = TextGeneratorType.Standard,
          fontSize = typography.FontSize1
        };

        return new HColumn(
          crossAxisAlign: Align.Stretch,
          modifiers: new Modifier[] {
            new TextStyleModifier(general),
            new PaddingModifier(8f),
            new BackgroundStyleModifier(colors.surface.container),
            new BorderModifier(Border.None, BorderRadius.All(radius.Radius3))
          }
        ) {
          new HRow(crossAxisAlign: Align.FlexStart) {
            new HText("Log Message").Body(context),
            new HGap().Expand(),
            new HButton(
              HButtonVariant.Ghost,
              size: HButtonSize.Small,
              selected: wrapLines,
              onClick: SetState(() => wrapLines = !wrapLines)
            ) {
              new HRow(gap: 4f) {
                new HIcon(
                  FaSolidIcons.TextWidth,
                  FaSolidIcons.FontDefinition
                ),
                new HText(
                  "Wrap",
                  style: new TextStyle() {
                    style = FontStyle.Bold
                  }
                )
              }
            },
            new HButton(
              HButtonVariant.Ghost,
              size: HButtonSize.Small,
              onClick: () => { GUIUtility.systemCopyBuffer = widget.entry.GetFullText().Trim(); }
            ) {
              new HIcon(FaSolidIcons.Copy, FaSolidIcons.FontDefinition)
            },
            new HButton(HButtonVariant.Ghost, onClick: widget.onClose, size: HButtonSize.Small) {
              new HIcon(FaSolidIcons.Xmark, FaSolidIcons.FontDefinition)
            }
          },
          new HScrollView {
            new HText(widget.entry.message ?? "", style: message, selectable: true),
            new HGap(),
            new HText(widget.entry.stackTrace ?? "", style: wrapped, selectable: true)
          }.Fill()
        };
      }

    }

  }

  public class ConsoleHistorySignalObserver : Signal {

    public static ConsoleHistorySignalObserver Instance { get; } = new();

    private IDisposable _subscription;

    public List<string> history = new();

    public void Resubscribe() {
      _subscription?.Dispose();
      _subscription = Evt<LogMessageReceivedEvt>.Subscribe(OnLogMessageReceivedEvent);
    }

    public void AddToHistory(string message) {
      if (history.Contains(message)) history.Remove(message);
      if (history.Count >= 50) history.RemoveAt(history.Count - 1);
      history.Insert(0, message);
    }

    private void OnLogMessageReceivedEvent(ref LogMessageReceivedEvt args) {
      NotifyDirty();
      NotifyObservers();
    }

  }
}