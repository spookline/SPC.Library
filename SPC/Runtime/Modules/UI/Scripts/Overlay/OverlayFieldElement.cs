using HELIX.Coloring;
using HELIX.Coloring.Material;
using HELIX.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.UI.Overlay {
  public abstract class OverlayFieldElement : VisualElement, IFieldElement {

    public VisualElement Root => this;
    public Label labelLabel;
    public Label unitLabel;

    protected OverlayFieldElement(string label, string unit) {
      style.flexDirection = FlexDirection.Row;
      style.justifyContent = Justify.SpaceBetween;
      style.marginTop = -1;
      style.marginBottom = -1;
      style.fontSize = 10;
      style.minWidth = 100;
      style.color = Colors.White;

      labelLabel = new Label(label).Opacity(0.8f).NoPaddingAndMargin().Margined(right: 4).AddTo(this);

      var valueContainer = new VisualElement {
        style = { flexDirection = FlexDirection.Row }
      }.AddTo(this);

      // ReSharper disable once VirtualMemberCallInConstructor
      InitializeValueContainer(valueContainer);

      unitLabel = new Label(unit).NoPaddingAndMargin().AddTo(valueContainer);
      unitLabel.style.marginLeft = 2;
      unitLabel.Opacity(0.6f);
    }

    protected abstract void InitializeValueContainer(VisualElement container);

    public virtual void SetVisible(bool visible) {
      style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    protected void UpdateBase(string label, string unit) {
      labelLabel.text = label;
      if (string.IsNullOrEmpty(unit)) { unitLabel.style.display = DisplayStyle.None; } else {
        unitLabel.text = unit;
        unitLabel.style.display = DisplayStyle.Flex;
      }
    }

    public class Single : OverlayFieldElement {

      public Label valueLabel;

      public Single(string label, string unit) : base(label, unit) { }

      protected override void InitializeValueContainer(VisualElement container) {
        valueLabel = new Label().NoPaddingAndMargin().AddTo(container);
      }

      public void Update(string label, string value, string unit, Color? color) {
        UpdateBase(label, unit);
        valueLabel.text = value;
        valueLabel.style.color = color ?? Colors.White;
      }

    }

    public class Vec3 : OverlayFieldElement {

      public Label[] vectorValues;

      public Vec3(string label, string unit) : base(label, unit) { }

      protected override void InitializeValueContainer(VisualElement container) {
        var vX = new Label().NoPaddingAndMargin().AddTo(container);
        var vY = new Label().NoPaddingAndMargin().AddTo(container);
        var vZ = new Label().NoPaddingAndMargin().AddTo(container);

        vectorValues = new[] { vX, vY, vZ };
      }

      public void Update(string label, string[] values, string unit, Color? color) {
        UpdateBase(label, unit);

        vectorValues[0].text = values[0];
        vectorValues[0].style.color = color ?? MaterialColors.RedAccent;

        vectorValues[1].text = values[1];
        vectorValues[1].style.color = color ?? MaterialColors.GreenAccent;
        vectorValues[1].style.marginLeft = 2;

        vectorValues[2].text = values[2];
        vectorValues[2].style.color = color ?? MaterialColors.BlueAccent;
        ;
        vectorValues[2].style.marginLeft = 4;
      }

    }

  }
}