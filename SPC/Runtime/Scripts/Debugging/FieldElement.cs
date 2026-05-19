using UnityEngine.UIElements;

namespace Spookline.SPC.Debugging {
  public interface IFieldElement {

    VisualElement Root { get; }
    void SetVisible(bool visible);

  }
}

