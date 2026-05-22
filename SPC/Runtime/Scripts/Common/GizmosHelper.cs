using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spookline.SPC.Common {
    public class GizmosHelper {

        public static bool IsSelected(GameObject obj) {
#if UNITY_EDITOR
            return Selection.activeGameObject == obj;
#else
            return false;
#endif
        }

    }
}