using System;
using System.Collections.Generic;
using HELIX.Widgets;
using HELIX.Widgets.Modifiers;
using HELIX.Widgets.Scrolling;
using HELIX.Widgets.Signals;
using HELIX.Widgets.Universal;
using HELIX.Widgets.Universal.Styles;
using Spookline.SPC.Debugging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.UI {
  public class SpookConsoleHistory : StatefulWidget<SpookConsoleHistory> {

    public bool refreshing;

    public ValueSignal<ExtendedLogEntry?> selectedEntry;

    public SpookConsoleHistory(
      ValueSignal<ExtendedLogEntry?> selectedEntry,
      bool refreshing,
      Key key = default,
      object[] constants = null,
      IReadOnlyCollection<Modifier> modifiers = null
    ) : base(key, constants, modifiers) {
      this.selectedEntry = selectedEntry;
      this.refreshing = refreshing;
    }


    public override State<SpookConsoleHistory> CreateState() {
      return new State();
    }

    private class State : State<SpookConsoleHistory>, ISignalObserver {

      public ScrollController scrollController;

      public override void InitState() {
        scrollController = AddDisposable(new ScrollController());

        var observer = SpookConsoleHistoryObserver.Instance;
        observer.Resubscribe();
        dependencyTracker.DependOnExplicit(observer, true);
        dependencyTracker.OnDependenciesChanged += OnSignalDependenciesChanged;

        if (widget.refreshing) mount.Element.schedule.Execute(OnTimerUpdate).Every(1000);
      }

      private void OnTimerUpdate(TimerState obj) {
        var observer = SpookConsoleHistoryObserver.Instance;
        if (observer.hasUnhandledUpdate) observer.Refresh();
      }

      private void OnSignalDependenciesChanged(Signal obj) {
        mount.Element.schedule
          .Execute(() => scrollController.JumpTo(scrollController.MaxOffset))
          .ExecuteLater(10);
      }

      public override Widget Build(BuildContext context) {
        var style = SpookTheme.Console.Get(context);

        return new HListView(
          (ctx, i) => BuildLogMessageEntry(style, ctx, i),
          SpookConsoleHistoryObserver.Instance.messages.Count,
          scrollController: scrollController
        );
      }

      private Widget BuildLogMessageEntry(SpookConsoleStyle style, BuildContext ctx, int i) {
        var entry = SpookConsoleHistoryObserver.Instance.messages[i];
        var icon = FaSolidIcons.Info;
        var color = style.colors.typeInfo;
        switch (entry.type) {
          case ExtLogType.Log: break;
          case ExtLogType.Warning:
            icon = FaSolidIcons.TriangleExclamation;
            color = style.colors.typeWarning;
            break;
          case ExtLogType.Error:
            icon = FaSolidIcons.CircleExclamation;
            color = style.colors.typeError;
            break;
          case ExtLogType.Assert:
            icon = FaSolidIcons.CircleExclamation;
            color = style.colors.typeError;
            break;
          case ExtLogType.Exception:
            icon = FaSolidIcons.Bug;
            color = style.colors.typeBug;
            break;
          case ExtLogType.Input:
            icon = FaSolidIcons.Terminal;
            color = style.colors.typeInput;
            break;

          default: throw new ArgumentOutOfRangeException();
        }

        var text = entry.summary;
        if (entry.repeatCount > 0) text = $"{entry.repeatCount + 1}x {text}";

        return new HRow(
          gap: style.historyIconSize.value.value * 0.5f,
          modifiers: new Modifier[] {
            new TextStyleModifier(
              new TextStyle { generator = TextGeneratorType.Standard, color = color }
            ),
            new ManipulatorModifier(BuildLogClickManipulator(entry)),
            new PaddingModifier(style.historyEntryPadding)
          }
        ) {
          new HColumn(crossAxisAlign: Align.Center, mainAxisAlign: Justify.Center) {
            new HIcon(icon, FaSolidIcons.FontDefinition, color: color).Tight()
          }.Size(style.historyIconSize),
          new HText(text).Expand().Caption(ctx)
        };
      }

      private Clickable BuildLogClickManipulator(ExtendedLogEntry entry) {
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
}