using System.Collections.Generic;
using HELIX.Abstractions;
using UnityEngine;
using HELIX.Coloring;
using HELIX.Extensions;
using HELIX.Types;
using UnityEngine.UIElements;

namespace Spookline.SPC.Examples {

  public static class OverlayGraphHelper {

    public static VisualElement Spacer() {
      return new Element {
        style = {
          flexGrow = 1
        }
      };
    }

    public static VisualElement SampleOverlay(out Label seconds, out Label hz) {
      seconds = new Label().NoPaddingAndMargin();
      hz = new Label().NoPaddingAndMargin();
      return new Element {
        style = {
          flexDirection = FlexDirection.Row,
          alignItems = Align.FlexStart,
          alignSelf = Align.FlexStart,
          fontSize = 6f,
          color = Colors.White80,
          backgroundColor = Colors.Black40,
        },
        Childs = new VisualElement[] {
          seconds,
          new Label("s").NoPadding().Margined(right: 2f),
          hz,
          new Label("Hz").NoPaddingAndMargin(),
        },
      }.MakeAbsolute().Positioned(left: 4f, top: 4f).Padding(new StyleLength2(2f, 1f)).BorderRadius(2f);
    }

    public static VisualElement Container(VisualElement[] elements, float width = 300, float height = 48) {
      return new Element {
        style = {
          width = width,
          height = height,
          flexDirection = FlexDirection.Column,
        },
        Childs = elements
      }.BorderRadius(4).BackgroundColor(Colors.Black70).Margined(top: 2, bottom: 2);
    }

    public static VisualElement BottomRow(List<VisualElement> elements) {
      return new Element {
        style = {
          flexDirection = FlexDirection.Row,
          justifyContent = Justify.FlexStart,
          alignItems = Align.Stretch,
          height = 8f,
          fontSize = 8f,
          color = Colors.White60,
          unityTextAlign = TextAnchor.MiddleCenter,
        },
        Childs = elements
      }.Margined(left: 3f, bottom: 3f, right: 3f, top: 0f);
    }

    public static Label ValueLabelColor(
      List<VisualElement> list,
      string label,
      Color color,
      bool padStart = true,
      float width = 16f
    ) {
      var valueLabel = Value(width);
      list.Add(valueLabel);
      list.Add(LabelField(label, padStart));
      list.Add(ColorField(color));
      return valueLabel;
    }

    public static Label ValueLabel(List<VisualElement> list, string label, bool padStart = true, float width = 16f) {
      var valueLabel = Value(width);
      list.Add(valueLabel);
      list.Add(LabelField(label, padStart));
      return valueLabel;
    }

    public static VisualElement LabelField(string str, bool padStart = true) {
      return new Label(str).NoPadding().Margined(left: padStart ? 3f : 0f, right: 3f);
    }

    public static Label Value(float width = 16f, bool highlight = true) {
      var label = new Label {
        style = {
          unityTextAlign = TextAnchor.MiddleRight,
        }
      }.NoPaddingAndMargin();
      if (highlight) label.TextColor(Colors.White);
      return label.Sized(width);
    }

    public static Label Value(List<VisualElement> list, float width = 16f, bool highlight = true) {
      var valueLabel = Value(width, highlight);
      list.Add(valueLabel);
      return valueLabel;
    }

    public static VisualElement ColorField(Color color) {
      return new Element {
          style = {
            alignSelf = Align.Center,
          }
        }.Size(new StyleLength2(7f)).BorderRadius(2)
        .BackgroundColor(color)
        .Margined(right: 3f);
    }
  }
}