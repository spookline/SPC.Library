using System;
using HELIX.Coloring;
using HELIX.Widgets;
using HELIX.Widgets.Signals;
using HELIX.Widgets.Universal;
using HELIX.Widgets.Universal.Controllers;
using HELIX.Widgets.Universal.Styles;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.Conscript.UI {
  public class GraphTimelineRow : StatefulWidget<GraphTimelineRow> {

    public override State<GraphTimelineRow> CreateState() => new State();

    private class State : State<GraphTimelineRow> {

      private TextEditingController _controller;

      public override void InitState() {
        base.InitState();
        _controller = new TextEditingController {
          onSubmitted = s => {
            var blackboardSignal = GraphBlackboard.SignalProperty.Get(mount);
            var value = blackboardSignal.PeekValue();
            var next = Math.Clamp(uint.Parse(s), 0, value.machine.CurrentTick);
            _controller.Value = next.ToString();
            value.state.SetSelectedTick(next, false);
          }
        };
        dependencyTracker.OnDependenciesChanged += OnDependenciesSignalChanged;
      }

      private void OnDependenciesSignalChanged(Signal obj) {
        if (obj is Signal<GraphBlackboard.BlackboardViewState> signal) {
          var value = signal.Value;
          _controller.SetValue(value.tickId.ToString());
        }
      }

      public override Widget Build(BuildContext context) {
        var blackboardSignal = GraphBlackboard.SignalProperty.Get(context);
        var value = blackboardSignal.Value;

        return new HRow() {
          new HButton(
            HButtonVariant.Ghost,
            onClick: () => {
              value.machine.SetRecording(true);
              value.machine.Tick();
              value.state.SetSelectedTick(value.machine.CurrentTick, true);
            }
          ) {
            new HRow(gap: 4, crossAxisAlign: Align.FlexEnd) {
              new HText("Tick", style: new TextStyle { style = FontStyle.Bold }),
              new HIcon(FaSolidIcons.Play, FaSolidIcons.FontDefinition)
            }
          },
          new HButton(
            HButtonVariant.Ghost,
            onClick: () => {
              value.machine.SetRecording(!value.isRecording);
              value.state.RefreshState();
            }
          ) {
            new HIcon(
              value.isRecording ? FaSolidIcons.Circle : FaSolidIcons.CircleDot,
              FaSolidIcons.FontDefinition,
              color: value.isRecording ? Colors.Red : Colors.White50
            )
          },
          new HGap(2),
          new HText(
            value.machine.CurrentTick == value.tickId ? "Live" : $"Samples: {value.machine.CurrentTick - value.machine.RecordingStart}",
            style: TextStyle.AlignLeft
          ),
          new HGap().Expand(),
          new HGap(),
          new HTextField(_controller).Size(48),
          new HGap(),
          new HButton(
            HButtonVariant.Ghost,
            onClick: () => {
              var next = ConscriptMachine.GetPreviousTick(value.tickId);
              if (next > value.machine.CurrentTick) return;
              value.state.SetSelectedTick(next, false);
            }
          ) {
            new HIcon(FaSolidIcons.AngleLeft, FaSolidIcons.FontDefinition)
          },
          new HButton(
            HButtonVariant.Ghost,
            onClick: () => {
              var next = ConscriptMachine.GetNextTick(value.tickId);
              if (next > value.machine.CurrentTick) return;
              value.state.SetSelectedTick(next, false);
            }
          ) {
            new HIcon(FaSolidIcons.AngleRight, FaSolidIcons.FontDefinition)
          },
          new HButton(
            HButtonVariant.Ghost,
            onClick: () => { value.state.SetSelectedTick(value.machine.CurrentTick, true); }
          ) {
            new HIcon(FaSolidIcons.AnglesRight, FaSolidIcons.FontDefinition)
          },
        };
      }

    }

  }
}