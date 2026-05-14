using System;
using System.Collections.Generic;
using HELIX.Types;
using HELIX.Widgets;
using HELIX.Widgets.Modifiers;
using HELIX.Widgets.Scrolling;
using HELIX.Widgets.Signals;
using HELIX.Widgets.Universal;
using HELIX.Widgets.Universal.Styles;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.UI {
  public class SpookConsoleHistory : StatefulWidget<SpookConsoleHistory> {

    public Color infoColor;
    public Color successColor;
    public Color warningColor;
    public Color errorColor;
    public ValueSignal<ExtendedLogEntry?> selectedEntry;
    public bool refreshing;

    public SpookConsoleHistory(
      Color infoColor,
      Color successColor,
      Color warningColor,
      Color errorColor,
      ValueSignal<ExtendedLogEntry?> selectedEntry,
      bool refreshing,
      Key key = default,
      object[] constants = null,
      IReadOnlyCollection<Modifier> modifiers = null
    ) : base(key, constants, modifiers) {
      this.infoColor = infoColor;
      this.successColor = successColor;
      this.warningColor = warningColor;
      this.errorColor = errorColor;
      this.selectedEntry = selectedEntry;
      this.refreshing = refreshing;
    }


    public override State<SpookConsoleHistory> CreateState() => new State();

    private class State : State<SpookConsoleHistory>, ISignalObserver {

      public ScrollController scrollController;

      public override void InitState() {
        scrollController = AddDisposable(new ScrollController());

        var observer = SpookConsoleHistoryObserver.Instance;
        observer.Resubscribe();
        dependencyTracker.DependOnExplicit(observer, weak: true);
        dependencyTracker.OnDependenciesChanged += OnSignalDependenciesChanged;

        if (widget.refreshing) mount.Element.schedule.Execute(OnTimerUpdate).Every(1000);
      }

      private void OnTimerUpdate(TimerState obj) {
        var observer = SpookConsoleHistoryObserver.Instance;
        if (observer.hasUnhandledUpdate) {
          observer.Refresh();
        }
      }

      private void OnSignalDependenciesChanged(Signal obj) {
        mount.Element.schedule
          .Execute(() => scrollController.JumpTo(scrollController.MaxOffset))
          .ExecuteLater(10);
      }

      public override Widget Build(BuildContext context) {
        return new HListView(
          BuildLogMessageEntry,
          SpookConsoleHistoryObserver.Instance.messages.Count,
          scrollController: scrollController
        );
      }

      private Widget BuildLogMessageEntry(BuildContext ctx, int i) {
        var entry = SpookConsoleHistoryObserver.Instance.messages[i];
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

        var text = entry.summary;
        if (entry.repeatCount > 0) {
          text = $"{entry.repeatCount + 1}x {text}";
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
          new HColumn(crossAxisAlign: Align.Center, mainAxisAlign: Justify.Center) {
            new HIcon(icon, FaSolidIcons.FontDefinition, color: color).Tight()
          }.Size(16f),
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