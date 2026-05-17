using System.Collections.Generic;
using HELIX.Coloring;
using HELIX.Extensions;
using HELIX.Types;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.UI.Overlay {

  public interface IFieldElement {

    VisualElement Root { get; }
    void SetVisible(bool visible);

  }
  public class OverlayElement : VisualElement {

    public Label titleLabel;
    public Label subtitleLabel;
    public VisualElement fieldsContainer;
    public Vector3 worldPosition;


    private readonly Dictionary<string, IFieldElement> _fieldElements = new();

    public OverlayElement() {
      style.position = Position.Absolute;
      style.width = StyleKeyword.Auto;

      this.Padding(new StyleLength2(4, 2))
        .BorderRadius(3)
        .BackgroundColor(new Color(0, 0, 0, 0.6f));

      var titleRow = new VisualElement {
          name = "title-row",
          style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, justifyContent = Justify.SpaceBetween }
        }
        .Margined(bottom: 1).AddTo(this);

      titleLabel = new Label {
          name = "title",
          style = { unityFontStyleAndWeight = FontStyle.Bold }
        }
        .NoPaddingAndMargin()
        .TextColor(Colors.White)
        .TextSize(11)
        .AddTo(titleRow);

      subtitleLabel = new Label {
          name = "subtitle",
          style = { unityFontStyleAndWeight = FontStyle.Normal }
        }
        .NoPaddingAndMargin()
        .TextColor(Colors.White)
        .TextSize(11).Margined(left: 4).AddTo(titleRow);

      fieldsContainer = new VisualElement { name = "fields" }.AddTo(this);
      style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(0));
    }

    public IFieldElement GetOrCreateElement(string label, OverlayField field) {
      if (_fieldElements.TryGetValue(label, out var element)) {
        // Check if existing element matches field type
        var isCompatible = (field is OverlayField.String or OverlayField.Float or OverlayField.Int &&
                            element is OverlayFieldElement.Single) ||
                           (field is OverlayField.Vec3 && element is OverlayFieldElement.Vec3);

        if (isCompatible) return element;

        // Incompatible, remove old
        fieldsContainer.Remove(element.Root);
      }

      IFieldElement newElement = field switch {
        OverlayField.Vec3 => new OverlayFieldElement.Vec3(label, field.unit),
        _ => new OverlayFieldElement.Single(label, field.unit)
      };

      _fieldElements[label] = newElement;
      fieldsContainer.Add(newElement.Root);
      return newElement;
    }

    public void HideUnusedFields(HashSet<string> activeLabels) {
      foreach (var kv in _fieldElements) {
        if (!activeLabels.Contains(kv.Key)) { kv.Value.SetVisible(false); }
      }
    }

  }
}