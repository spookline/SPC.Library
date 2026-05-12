using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Spookline.SPC {
    public static class GameObjectExtensions {

        public static void ChangeLayerRecursively(this GameObject obj, int newLayer) {
            if (!obj) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform) {
                ChangeLayerRecursively(child.gameObject, newLayer);
            }
        }

        public static bool IsInLayerMask(this GameObject obj, LayerMask layerMask) {
            return obj.layer.IsInLayerMask(layerMask);
        }

        public static bool IsInLayerMask(this int layer, LayerMask layerMask) {
            return (layerMask.value & (1 << layer)) != 0;
        }

        public static T GetOrAddComponent<T>(this GameObject obj, [CanBeNull] T existing = null, Action<T> onCreated = null) where T : Component {
            if (existing) return existing;
            if (obj.TryGetComponent<T>(out var component)) {
                return component;
            } else {
                var added = obj.AddComponent<T>();
                onCreated?.Invoke(added);
                return added;
            }
        }

        public static T GetOrAddComponent<T>(this Component component, [CanBeNull] T existing = null, Action<T> onCreated = null)
            where T : Component {
            return component.gameObject.GetOrAddComponent(existing, onCreated);
        }

    }
}