using UnityEngine.UIElements;

namespace Spookline.SPC.Debugging {
    public interface IOverlayFieldFactory<T, E> where E : IFieldElement {

        E CreateElement(string label);

        void UpdateElement(E element, T value);

    }
}