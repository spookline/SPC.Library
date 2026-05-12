using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Geometry {
    public static class TransformExtensions {

        public static void SetGlobalScale(this Transform target, float3 globalScale) {
            var parent = target.parent;
            if (!parent) {
                target.localScale = globalScale;
                return;
            }

            float3 parentScale = parent.lossyScale;
            target.localScale = globalScale / math.select(parentScale, 1f, math.abs(parentScale) <= math.EPSILON);
        }

        public static async UniTask SmoothMoveTo(this Transform target, Transform to, float duration) {
            var startTime = Time.time;
            while (Time.time - startTime < duration) {
                var t = (Time.time - startTime) / duration;
                target.position = math.lerp(target.position, to.position, t);
                target.rotation = math.slerp(target.rotation, to.rotation, t);
                await UniTask.Yield();
            }

            target.position = to.position;
            target.rotation = to.rotation;
        }

    }
}