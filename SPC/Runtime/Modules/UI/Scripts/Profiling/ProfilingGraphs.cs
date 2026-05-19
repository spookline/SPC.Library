using System.Collections.Generic;
using HELIX.Coloring;
using HELIX.Painting;
using HELIX.Widgets.Elements;
using HELIX.Widgets.Visual;
using Unity.Mathematics;

namespace Spookline.SPC.UI {
  using System;
  using UnityEngine;
  using UnityEngine.UIElements;
  using Vector2 = UnityEngine.Vector2;


  public interface IMultiLineGraphSampler {

    int Length { get; }
    int LineCount { get; }
    float SampleLineAt(int index, int lineIndex);

  }

  public interface IFpsGraphSampler {

    int Length { get; }
    float3 SampleFrameDataAt(int index);

  }

  [UxmlElement]
  public partial class SimpleMultiLineGraph : PaintingElement {

    public IMultiLineGraphSampler multiLineGraphSampler = null;

    public int lineIndex;
    public int lineCount = 1;
    public ScriptablePathDrawer[] lineDrawers;
    public ScriptablePathDrawer[] fillDrawers;

    public SimpleMultiLineGraph() {
      style.overflow = Overflow.Hidden;
    }

    public void Refresh() {
      MarkDirtyRepaint();
    }

    public override void Paint(PaintCanvas canvas, Rect bounds) {
      var painter = canvas.painter;

      var len = multiLineGraphSampler.Length;
      if (len < 2) return;

      var rect = contentRect;

      Span<Vector2> pts = stackalloc Vector2[len];
      for (int li = 0; li < lineCount; li++) {
        var n = 0;
        for (var index = 0; index < len; index++) {
          var x = index / (float)len;
          var v = multiLineGraphSampler.SampleLineAt(index, lineIndex + li);
          v = Mathf.Clamp01(v);
          pts[n++] = new Vector2(rect.xMin + x * rect.width, (1 - v) * rect.height + rect.yMin);
        }

        pts[n - 1].x = rect.xMax;

        painter.BeginPath();
        painter.MoveTo(pts[0]);
        for (var i = 1; i < n; i++) painter.LineTo(pts[i]);
        var lineDrawer = lineDrawers[li];
        lineDrawer?.Draw(canvas);

        var fillDrawer = fillDrawers[li];
        if (fillDrawer == null) continue;
        painter.LineTo(new Vector2(rect.xMax, rect.yMax));
        painter.LineTo(new Vector2(rect.xMin, rect.yMax));
        painter.ClosePath();
        fillDrawer.Draw(canvas);
      }
    }

  }

  [UxmlElement]
  public partial class FpsGraphPainter : PaintingElement {

    public IFpsGraphSampler sampleDatasource = null;

    public FpsGraphPainter() {
      style.overflow = Overflow.Visible;
    }

    public Color lineHigh = Colors.Blue.WithOpacity(0.5f);
    public Color line = Colors.AlphaBlend(Colors.Blue, Colors.White20);
    public Color background = Colors.Blue.WithOpacity(0.05f);
    public Color backgroundLow = Colors.Blue.WithOpacity(0.2f);

    public void Refresh() {
      MarkDirtyRepaint();
    }


    public override void Paint(PaintCanvas canvas, Rect bounds) {
      var painter = canvas.painter;
      var len = sampleDatasource.Length;
      if (len < 2) return;

      var rect = contentRect;
      Span<float4> pts = stackalloc float4[len];
      var n = 0;
      for (var index = 0; index < len; index++) {
        var x = index / (float)len;
        var v = sampleDatasource.SampleFrameDataAt(index);
        v = math.saturate(v);
        var c = (new float3(1) - v) * rect.height + rect.yMin;
        pts[n++] = new float4(rect.xMin + x * rect.width, c);
      }

      pts[n - 1].x = rect.xMax; // ensure last point is flush with the right edge

      // Ceiling
      painter.BeginPath();
      painter.MoveTo(new Vector2(pts[0].x, pts[0].w));
      for (var i = 1; i < n; i++) painter.LineTo(new Vector2(pts[i].x, pts[i].w));
      painter.strokeColor = lineHigh;
      painter.lineWidth = 0.5f;
      painter.Stroke();

      painter.LineTo(new Vector2(rect.xMax, rect.yMax));
      painter.LineTo(new Vector2(rect.xMin, rect.yMax));
      painter.ClosePath();
      painter.fillColor = background;
      painter.Fill();

      // Floor
      painter.BeginPath();
      painter.MoveTo(new Vector2(pts[0].x, pts[0].y));
      for (var i = 1; i < n; i++) painter.LineTo(new Vector2(pts[i].x, pts[i].y));
      painter.LineTo(new Vector2(rect.xMax, rect.yMax));
      painter.LineTo(new Vector2(rect.xMin, rect.yMax));
      painter.ClosePath();
      painter.fillColor = backgroundLow;
      painter.Fill();

      // Average
      painter.BeginPath();
      painter.MoveTo(new Vector2(pts[0].x, pts[0].z));
      for (var i = 1; i < n; i++) painter.LineTo(new Vector2(pts[i].x, pts[i].z));
      painter.strokeColor = line;
      painter.lineWidth = 1f;
      painter.Stroke();
    }

  }
}