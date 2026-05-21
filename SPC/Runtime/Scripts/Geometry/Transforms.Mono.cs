using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Geometry {
    public static partial class Transforms {

        public static AffineTransform Affine(this Transform transform) {
            transform.GetPositionAndRotation(out var position, out var rotation);
            return new AffineTransform(
                position,
                rotation.normalized,
                transform.lossyScale
            );
        }

        public static RigidTransform Rigid(this Transform transform) {
            transform.GetPositionAndRotation(out var position, out var rotation);
            return new RigidTransform(
                rotation.normalized,
                position
            );
        }

        public static AffineTransform LocalAffine(this Transform transform) {
            transform.GetLocalPositionAndRotation(out var position, out var rotation);
            return new AffineTransform(
                position,
                rotation.normalized,
                transform.localScale
            );
        }

        public static RigidTransform LocalRigid(this Transform transform) {
            transform.GetLocalPositionAndRotation(out var position, out var rotation);
            return new RigidTransform(
                rotation.normalized,
                position
            );
        }

        public static TRS Decompose(this Transform transform) {
            transform.GetPositionAndRotation(out var position, out var rotation);
            return new TRS(
                position,
                rotation.normalized,
                transform.lossyScale
            );
        }

        public static TRS DecomposeRigid(this Transform transform) {
            transform.GetPositionAndRotation(out var position, out var rotation);
            return new TRS(
                position,
                rotation.normalized,
                Vector3.one
            );
        }

        public static TRS DecomposeLocal(this Transform transform) {
            transform.GetLocalPositionAndRotation(out var position, out var rotation);
            return new TRS(
                position,
                rotation.normalized,
                transform.localScale
            );
        }

        public static TRS DecomposeLocalRigid(this Transform transform) {
            transform.GetLocalPositionAndRotation(out var position, out var rotation);
            return new TRS(
                position,
                rotation.normalized,
                Vector3.one
            );
        }


        public static void Apply(this AffineTransform transform, Transform target, bool lazy = true) {
            math.decompose(transform, out var pos, out var rot, out var scale);
            target.SetPositionAndRotation(pos, rot);
            SetGlobalScale(target, scale, lazy: lazy);
        }

        public static void ApplyLocal(this AffineTransform transform, Transform target) {
            math.decompose(transform, out var pos, out var rot, out var scale);
            target.SetLocalPositionAndRotation(pos, rot);
            var vecScale = (Vector3)scale;
            if (target.localScale != vecScale) target.localScale = vecScale;
        }

        public static void Apply(this RigidTransform transform, Transform target) {
            target.SetPositionAndRotation(transform.pos, transform.rot);
        }

        public static void ApplyLocal(this RigidTransform transform, Transform target) {
            target.SetLocalPositionAndRotation(transform.pos, transform.rot);
        }

        public static void SetGlobalScale(this Transform target, float3 scale, bool lazy = true) {
            var parent = target.parent;
            if (!parent) {
                target.localScale = scale;
                return;
            }

            if (lazy && !math.any(math.abs(scale - (float3)target.lossyScale) > math.EPSILON)) return;
            float3 parentScale = parent.lossyScale;
            target.localScale = scale / math.select(
                parentScale,
                1f,
                math.abs(parentScale) <= math.EPSILON
            );
        }

    }
}