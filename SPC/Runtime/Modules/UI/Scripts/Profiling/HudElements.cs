using System.Collections.Generic;
using HELIX.Coloring;
using HELIX.Extensions;
using HELIX.Widgets.Visual;
using HELIX.Widgets.Visual.PathDrawers;
using Spookline.SPC.Common;
using Spookline.SPC.Debugging;
using Spookline.SPC.Examples;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.UI.Profiling {
  public class FpsGraphFieldFactory : IOverlayFieldFactory<FrameCountSource, FpsGraphFieldFactory.Field> {

    public static readonly FpsGraphFieldFactory Instance = new();

    public Field CreateElement(string label) {
      return new Field();
    }

    public void UpdateElement(Field element, FrameCountSource value) {
      element.graphPainter.sampleDatasource = value;
      element.targetLabel.text = value.TargetFps.FormatNonAlloc(decimals: 0);
      element.missLabel.text = (value.MissedPercent * 100f).FormatNonAlloc(decimals: 0);
      element.fpsLabel.text = value.CurrentFps.FormatNonAlloc(decimals: 0);
      element.averageLabel.text = value.AverageFps.FormatNonAlloc(decimals: 0);
      element.highestLabel.text = value.HighestFps.FormatNonAlloc(decimals: 0);
      element.lowestLabel.text = value.LowestFps.FormatNonAlloc(decimals: 0);

      element.timeLabel.text = value.BufferSeconds.FormatNonAlloc(
        Mathf.Approximately(math.frac(value.BufferSeconds), 0f) ? 0 : 1
      );
      element.windowLabel.text = value.BufferHz.FormatNonAlloc(decimals: 0);

      element.graphPainter.Refresh();
    }


    public class Field : IFieldElement {

      public VisualElement Root { get; }

      public readonly FpsGraphPainter graphPainter;
      public readonly Label targetLabel;
      public readonly Label fpsLabel;
      public readonly Label missLabel;
      public readonly Label highestLabel;
      public readonly Label lowestLabel;
      public readonly Label averageLabel;

      public readonly Label windowLabel;
      public readonly Label timeLabel;

      public Field() {
        graphPainter = new FpsGraphPainter().Padding(3f).Fill();

        var bottomRow = new List<VisualElement>();
        fpsLabel = OverlayGraphHelper.Value(bottomRow);
        bottomRow.Add(new Label("/").NoPaddingAndMargin());
        targetLabel = OverlayGraphHelper.Value(bottomRow, highlight: false);
        bottomRow.Add(new Label(" FPS").NoPaddingAndMargin());
        missLabel = OverlayGraphHelper.ValueLabel(bottomRow, "% Miss", padStart: false).Margined(left: 3f);
        bottomRow.Add(OverlayGraphHelper.Spacer());

        averageLabel = OverlayGraphHelper.ValueLabelColor(
          bottomRow,
          "Avg",
          Colors.AlphaBlend(Colors.Blue, Colors.White20)
        );
        lowestLabel = OverlayGraphHelper.ValueLabelColor(bottomRow, "Min", Colors.Blue.WithOpacity(0.5f));
        highestLabel = OverlayGraphHelper.ValueLabelColor(bottomRow, "Max", Colors.Blue.WithOpacity(0.2f));

        Root = OverlayGraphHelper.Container(
          new[] {
            graphPainter,
            OverlayGraphHelper.SampleOverlay(out timeLabel, out windowLabel),
            OverlayGraphHelper.BottomRow(bottomRow)
          }
        );
      }

      public void SetVisible(bool visible) {
        Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
      }

    }

  }

  public class MemoryGraphFieldFactory : IOverlayFieldFactory<MemorySource, MemoryGraphFieldFactory.Field> {

    public static readonly MemoryGraphFieldFactory Instance = new();

    public Field CreateElement(string label) {
      return new Field();
    }

    public void UpdateElement(Field element, MemorySource value) {
      element.graphPainter.multiLineGraphSampler = value;
      element.totalReservedLabel.text = value.LastTotalReserved.FormatNonAlloc(decimals: 2);
      element.totalUsedLabel.text = value.LastTotalUsed.FormatNonAlloc(decimals: 2);
      element.monoUsedLabel.text = value.LastMonoUsed.FormatNonAlloc(decimals: 2);

      element.timeLabel.text = value.BufferSeconds.FormatNonAlloc(
        Mathf.Approximately(math.frac(value.BufferSeconds), 0f) ? 0 : 1
      );
      element.windowLabel.text = value.BufferHz.FormatNonAlloc(decimals: 0);

      element.graphPainter.Refresh();
    }

    public class Field : IFieldElement {

      public VisualElement Root { get; }

      public readonly SimpleMultiLineGraph graphPainter;
      public readonly Label totalReservedLabel;
      public readonly Label totalUsedLabel;
      public readonly Label monoUsedLabel;

      public readonly Label windowLabel;
      public readonly Label timeLabel;

      public Field() {
        graphPainter = new SimpleMultiLineGraph {
          lineCount = 3,
          lineDrawers = new ScriptablePathDrawer[] {
            null,
            new SolidStrokePathDrawer {
              Color = Colors.Orange
            },
            new SolidStrokePathDrawer {
              Color = Colors.AlphaBlend(Colors.Yellow, Colors.White20),
            },
          },
          fillDrawers = new ScriptablePathDrawer[] {
            new SolidFillPathDrawer {
              Color = Colors.Orange.WithOpacity(0.15f)
            },
            null,
            null
          }
        }.Padding(3f).Fill();

        var bottomRow = new List<VisualElement>();
        bottomRow.Add(new Label("Memory GiB").NoPaddingAndMargin());
        bottomRow.Add(OverlayGraphHelper.Spacer());
        totalReservedLabel = OverlayGraphHelper.ValueLabelColor(
          bottomRow,
          "Reserved",
          Colors.Orange.WithOpacity(0.3f),
          true,
          24f
        );
        totalUsedLabel = OverlayGraphHelper.ValueLabelColor(
          bottomRow,
          "Allocated",
          Colors.Orange,
          true,
          24f
        );
        monoUsedLabel = OverlayGraphHelper.ValueLabelColor(
          bottomRow,
          "Mono",
          Colors.AlphaBlend(Colors.Yellow, Colors.White20),
          true,
          24f
        );

        Root = OverlayGraphHelper.Container(
          new[] {
            graphPainter,
            OverlayGraphHelper.SampleOverlay(out timeLabel, out windowLabel),
            OverlayGraphHelper.BottomRow(bottomRow)
          }
        );
      }


      public void SetVisible(bool visible) {
        Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
      }

    }

  }
}