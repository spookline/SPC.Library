using System;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Geometry {
    [Serializable]
    public struct TRS {

        public static readonly TRS Identity = new(Vector3.zero, Quaternion.identity, Vector3.one);

        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public TRS(Vector3 position) {
            this.position = position;
            rotation = Quaternion.identity;
            scale = Vector3.one;
        }

        public TRS(Vector3 position, Quaternion rotation) {
            this.position = position;
            this.rotation = rotation;
            scale = Vector3.one;
        }

        public TRS(Vector3 position, Quaternion rotation, Vector3 scale) {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public void Apply(Transform transform) {
            transform.SetPositionAndRotation(position, rotation);
            transform.SetGlobalScale(scale);
        }

        public void ApplyRigid(Transform transform) {
            transform.SetPositionAndRotation(position, rotation);
        }

        public void ApplyLocal(Transform transform) {
            transform.SetLocalPositionAndRotation(position, rotation);
            if (transform.localScale != scale) transform.localScale = scale;
        }

        public void ApplyLocalRigid(Transform transform) {
            transform.SetLocalPositionAndRotation(position, rotation);
        }

        public AffineTransform Affine() {
            return new AffineTransform(position, math.normalizesafe(rotation), scale);
        }

        public RigidTransform Rigid() => new(rotation, position);

        public Matrix4x4 Matrix() => Matrix4x4.TRS(position, rotation, scale);
        public float4x4 FloatMatrix() => float4x4.TRS(position, rotation, scale);

        public static TRS Translate(Vector3 translation) => new(translation);
        public static TRS Rotate(Quaternion rotation) => new(Vector3.zero, rotation);
        public static TRS Rotate(Vector3 euler) => new(Vector3.zero, Quaternion.Euler(euler));

        public static TRS Scale(Vector3 scale) => new(Vector3.zero, Quaternion.identity, scale);

        public static TRS Transform(Vector3 translation, Quaternion rotation, Vector3 scale) =>
            new(translation, rotation, scale);

        public static TRS Transform(Vector3 translation, Vector3 euler, Vector3 scale) =>
            new(translation, Quaternion.Euler(euler), scale);

        public static TRS PosRot(Vector3 position, Quaternion rotation) => new(position, rotation);
        public static TRS PosRot(Vector3 position, Vector3 euler) => new(position, Quaternion.Euler(euler));

        public static TRS Lerp(TRS a, TRS b, float t) {
            return new TRS(
                Vector3.Lerp(a.position, b.position, t),
                Quaternion.Slerp(a.rotation, b.rotation, t),
                Vector3.Lerp(a.scale, b.scale, t)
            );
        }

        public static TRS Delta(TRS from, TRS to) {
            var invFrom = from.Affine().Inverse();
            return invFrom.Transform(to).Decompose();
        }

        public static implicit operator AffineTransform(TRS vt) {
            return new AffineTransform(vt.position, vt.rotation, vt.scale);
        }

        public static implicit operator RigidTransform(TRS vt) {
            return new RigidTransform(vt.rotation, vt.position);
        }

        public static implicit operator TRS(RigidTransform rt) {
            return new TRS(rt.pos, rt.rot);
        }

        public static implicit operator Matrix4x4(TRS vt) {
            return Matrix4x4.TRS(vt.position, vt.rotation, vt.scale);
        }

        public static implicit operator float4x4(TRS vt) {
            return float4x4.TRS(vt.position, vt.rotation, vt.scale);
        }

    }
}