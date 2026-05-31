using System;
using System.Collections.Generic;
using HELIX.Coloring;
using HELIX.Widgets;
using HELIX.Widgets.Modifiers;
using HELIX.Widgets.Navigation;
using HELIX.Widgets.Signals;
using HELIX.Widgets.Theming;
using HELIX.Widgets.Universal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.Conscript.UI {
  public class GraphBlackboard : StatefulWidget<GraphBlackboard> {

    public readonly ConscriptBrain brain;

    public static readonly ThemeProperty<Signal<BlackboardViewState>> SignalProperty = new("graph-blackboard-state");

    public GraphBlackboard(
      ConscriptBrain brain,
      Key key = default,
      object[] constants = null,
      IReadOnlyCollection<Modifier> modifiers = null
    ) : base(key, constants, modifiers) {
      this.brain = brain;
    }

    public override State<GraphBlackboard> CreateState() {
      return new State();
    }

    public struct BlackboardViewState : IEquatable<BlackboardViewState> {

      public uint tickId;
      public ConscriptMachine machine;
      public ConscriptBrain brain;
      public State state;
      public bool isRecording;
      public bool isLive;


      public bool Equals(BlackboardViewState other) {
        return tickId == other.tickId && Equals(machine, other.machine) && Equals(brain, other.brain) &&
               Equals(state, other.state) && isRecording == other.isRecording && isLive == other.isLive;
      }

      public override bool Equals(object obj) {
        return obj is BlackboardViewState other && Equals(other);
      }

      public override int GetHashCode() {
        return HashCode.Combine(tickId, machine, brain, state, isRecording, isLive);
      }

    }

    public class State : State<GraphBlackboard> {

      private readonly GlobalKey _moveable = new();

      public float zoom = 1f;
      public Vector2 panOffset = Vector2.zero;
      public Signal<BlackboardViewState> stateSignal;


      public NodeWidget node;
      public ConscriptMachine machine;


      public void UpdateTransform() {
        var element = _moveable.Target.Element;
        element.style.translate = panOffset;
        element.style.scale = new Vector3(zoom, zoom, 1f);
      }

      public override void InitState() {
        stateSignal = AddDisposable(Signal.Value<BlackboardViewState>());
        RefreshNode();
        mount.Element.schedule.Execute(RefreshNode).Every(1000).Resume();
      }

      public void RefreshNode() {
        var currentHierarchy = widget.brain.machine;
        if (machine == currentHierarchy) {
          RefreshState();
          return;
        }

        if (currentHierarchy == null) {
          machine = null;
          node = null;
          stateSignal.SetValue(
            new BlackboardViewState {
              tickId = 0,
              machine = null,
              brain = widget.brain,
              state = this,
              isRecording = false,
              isLive = true
            }
          );
          SetState();
          return;
        }

        machine = currentHierarchy;
        node = NodeWidget.Create(null, currentHierarchy.Root.BuildWidget());
        stateSignal.SetValue(
          new BlackboardViewState {
            tickId = machine?.CurrentTick ?? 0,
            machine = machine,
            brain = widget.brain,
            state = this,
            isRecording = currentHierarchy.Recording,
            isLive = true
          }
        );
        SetState();
      }


      public void RefreshState() {
        if (machine == null) return;

        var current = stateSignal.PeekValue();
        stateSignal.SetValue(
          new BlackboardViewState {
            tickId = current.isLive ? machine.CurrentTick : current.tickId,
            machine = machine,
            brain = widget.brain,
            state = this,
            isRecording = machine.Recording,
            isLive = current.isLive
          }
        );
      }

      public void SetSelectedTick(uint tickId, bool live) {
        stateSignal.SetValue(
          new BlackboardViewState {
            tickId = tickId,
            machine = machine,
            brain = widget.brain,
            state = this,
            isRecording = machine.Recording,
            isLive = live
          }
        );
      }

      public override Widget Build(BuildContext context) {
        if (node == null) return new HText("No Node selected");

        return new HThemeProvider(
          properties: new Dictionary<ThemeProperty, object>() {
            { SignalProperty, stateSignal }
          }
        ) {
          new HScaffold {
            new HColumn(crossAxisAlign: Align.Stretch) {
              new HBox(
                background: Colors.Black15,
                modifiers: new ModifierSet {
                  FlexibleModifier.Fill,
                  ClipModifier.Clip,
                  new ManipulatorModifier(new GraphManipulator(this)),
                }
              ) {
                new HBox(
                  key: _moveable,
                  modifiers: new ModifierSet {
                    TransformModifier.Of(translate: panOffset, scale: new Vector3(zoom, zoom, 1f))
                  }
                ) { node }
              },
              new HBox(background: Colors.Black30) {
                new GraphTimelineRow()
              }.Padding(4)
            }
          }.Size(height: 300, width: Length.Percent(100))
        };
      }

    }

    private class GraphManipulator : PointerManipulator {

      private readonly State _state;

      private bool _isDragging;
      private Vector2 _startPointerPosition;
      private Vector2 _startElementPosition;

      private const float _minScale = 0.1f;
      private const float _maxScale = 5.0f;
      private const float _zoomSpeed = 0.05f;

      public GraphManipulator(State state) {
        _state = state;
      }


      protected override void RegisterCallbacksOnTarget() {
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        target.RegisterCallback<WheelEvent>(OnWheel);
      }

      protected override void UnregisterCallbacksFromTarget() {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        target.UnregisterCallback<WheelEvent>(OnWheel);
      }

      private void OnPointerDown(PointerDownEvent evt) {
        if (evt.button == 2 || (evt.button == 0 && evt.actionKey)) {
          _isDragging = true;
          _startPointerPosition = evt.position;
          _startElementPosition = _state.panOffset;

          target.CapturePointer(evt.pointerId);
          evt.StopPropagation();
        }
      }

      private void OnPointerMove(PointerMoveEvent evt) {
        if (!_isDragging || !target.HasPointerCapture(evt.pointerId)) return;

        var delta = (Vector2)evt.position - _startPointerPosition;
        _state.panOffset = new Vector2(
          _startElementPosition.x + delta.x,
          _startElementPosition.y + delta.y
        );
        _state.UpdateTransform();

        evt.StopPropagation();
      }

      private void OnPointerUp(PointerUpEvent evt) {
        if (_isDragging && target.HasPointerCapture(evt.pointerId)) {
          _isDragging = false;
          target.ReleasePointer(evt.pointerId);
          evt.StopPropagation();
        }
      }

      private void OnWheel(WheelEvent evt) {
        var before = _state.zoom;
        var zoomFactor = -evt.delta.y * _zoomSpeed;
        var after = Mathf.Clamp(before + zoomFactor, _minScale, _maxScale);

        var offsetBefore = _state.panOffset;
        var offsetAfter = offsetBefore * after / before;

        _state.zoom = after;
        _state.panOffset = offsetAfter;
        _state.UpdateTransform();

        evt.StopPropagation();
      }

    }

  }
}