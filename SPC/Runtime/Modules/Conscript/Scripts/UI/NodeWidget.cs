using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HELIX.Coloring;
using HELIX.Painting;
using HELIX.Types;
using HELIX.Widgets;
using HELIX.Widgets.Elements;
using HELIX.Widgets.Scrolling;
using HELIX.Widgets.Universal;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.Conscript.UI {
  public struct NodeTemplate {

    public Widget content;
    public ConscriptNode node;
    public List<NodeTemplate> inline;
    public List<NodeTemplate> sequence;
    public List<NodeTemplate> parallel;

    public NodeTemplate(ConscriptNode node) : this() {
      this.node = node;
    }

  }

  public class NodeWidget : StatefulWidget<NodeWidget> {

    public ConscriptNode node;
    public NodeWidget parent;
    public Widget content;
    public GlobalKey contentKey = new();
    public List<NodeWidget> inline;
    public List<NodeWidget> sequence;
    public List<NodeWidget> parallel;
    public bool drawLine;

    public static NodeWidget Create(NodeWidget parent, NodeTemplate template, bool drawLine = true) {
      var t = new NodeWidget(
        template.node,
        parent: parent,
        content: template.content,
        drawLine: drawLine
      );
      if (template.inline != null) { t.inline = template.inline.ConvertAll(nt => Create(t, nt, false)); }

      if (template.sequence != null) {
        var list = new List<NodeWidget>();
        for (var i = 0; i < template.sequence.Count; i++) {
          var nt = template.sequence[i];
          list.Add(Create(t, nt, i == 0));
        }

        t.sequence = list;
      }

      if (template.parallel != null) { t.parallel = template.parallel.ConvertAll(nt => Create(t, nt)); }

      return t;
    }

    public NodeWidget(
      ConscriptNode node,
      NodeWidget parent,
      Widget content,
      List<NodeWidget> inline = null,
      List<NodeWidget> sequence = null,
      List<NodeWidget> parallel = null,
      bool drawLine = true,
      Key key = default,
      object[] constants = null,
      IReadOnlyCollection<Modifier> modifiers = null
    ) : base(key, constants, modifiers) {
      this.node = node;
      this.parent = parent;
      this.content = content;
      this.inline = inline;
      this.sequence = sequence;
      this.parallel = parallel;
      this.drawLine = drawLine;
    }


    public override State<NodeWidget> CreateState() {
      return new State();
    }

    private class State : State<NodeWidget> {

      public override Widget Build(BuildContext context) {
        var blackboardSignal = GraphBlackboard.SignalProperty.Get(context);
        var value = blackboardSignal.Value;

        var state = NodeStatus.Uninitialized;
        ConscriptNode.StateDeltaFlags flags = default;

        if (widget.node != null) {
          state = widget.node.GetFinalState(value.tickId, out flags);
        }

        var color = state.ToColor();
        // if (flags.HasFlag(ConscriptNode.StateDeltaFlags.HasInterrupted)) {
        //   color = Colors.Teal;
        // }

        var parent = widget.parent;
        return new HColumn {
          new FactoryWidget<LineDrawer> {
            creator = () => new LineDrawer(parent, widget, mount.Element),
            updater = lineDrawer => {
              lineDrawer.from = parent;
              lineDrawer.to = mount.Element;
              lineDrawer.color = color;
              lineDrawer.active = flags.HasFlag(ConscriptNode.StateDeltaFlags.WasTickedThisFrame);
              lineDrawer.interrupted = flags.HasFlag(ConscriptNode.StateDeltaFlags.HasInterrupted);
              lineDrawer.MarkDirtyRepaint();
            }
          }.If(parent != null && mount.Element != null && widget.drawLine),
          new HBox(
            key: widget.contentKey,
            background: Colors.Black,
            border: Border.All(1, color),
            borderRadius: BorderRadius.All(4)
          ) {
            new HColumn() {
              widget.content,
              new HColumn(children: widget.inline, gap: 2).If(widget.inline is { Count: > 0 }),
            }
          }.Padding(4),
          new HColumn(children: widget.sequence, gap: 2).Margin(EdgeInsets.Only(top: 16))
            .If(widget.sequence is { Count: > 0 }),
          new HRow(children: widget.parallel, crossAxisAlign: Align.FlexStart, gap: 8)
            .Margin(EdgeInsets.Only(top: 24))
            .If(widget.parallel is { Count: > 0 }),
        };
      }

    }

    public class LineDrawer : PaintingElement {

      public NodeWidget from;
      public NodeWidget owner;
      public VisualElement to;
      public Color color;
      public bool active;
      public bool interrupted;

      public LineDrawer(NodeWidget from, NodeWidget owner, VisualElement to) {
        this.from = from;
        this.to = to;
        this.owner = owner;
      }

      public override void Paint(PaintCanvas canvas, Rect bounds) {
        if (from == null || to == null) return;

        // Get bottom center of from bounds and draw to top center of to bounds
        var key = from?.contentKey;

        var element = key?.Target?.Element;
        if (element == null) return;

        var fromBounds = element.worldBound;
        var toBounds = to.worldBound;
        var fromPoint = this.WorldToLocal(new Vector2(fromBounds.center.x, fromBounds.yMax));
        var toPoint = this.WorldToLocal(new Vector2(toBounds.center.x, toBounds.yMin));

        var arrowLength = 3f;
        var arrowWidth = 5f;
        var curvature = 0.8f;

        canvas.painter.strokeColor = color;
        canvas.painter.fillColor = color;
        canvas.painter.lineWidth = 2.5f;

        if (interrupted) {
          canvas.painter.SetDashPattern(2,2);
        }

        var distanceY = Mathf.Abs(toPoint.y - fromPoint.y) * curvature;

        var arrowTip = toPoint;
        var arrowBase = arrowTip - new Vector2(0, arrowLength);
        var controlPoint1 = new Vector2(fromPoint.x, fromPoint.y + distanceY);
        var controlPoint2 = new Vector2(arrowBase.x, arrowBase.y - distanceY);


        if (active) {
          canvas.painter.BeginPath();
          canvas.painter.MoveTo(fromPoint);
          canvas.painter.BezierCurveTo(controlPoint1, controlPoint2, arrowBase);
          canvas.painter.Stroke();

          var leftWing = arrowBase + new Vector2(-arrowWidth, -arrowLength);
          var rightWing = arrowBase + new Vector2(arrowWidth, -arrowLength);
          canvas.painter.BeginPath();
          canvas.painter.MoveTo(arrowTip);
          canvas.painter.LineTo(leftWing);
          canvas.painter.LineTo(rightWing);
          canvas.painter.ClosePath();
          canvas.painter.Fill();
        } else {
          canvas.painter.BeginPath();
          canvas.painter.MoveTo(fromPoint);
          canvas.painter.BezierCurveTo(controlPoint1, controlPoint2, toPoint);
          canvas.painter.Stroke();
        }
      }

    }

  }
}