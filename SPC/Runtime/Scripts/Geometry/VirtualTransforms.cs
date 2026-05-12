using UnityEngine;

namespace Spookline.SPC.Geometry {
    public class VirtualTransform {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public VirtualTransform(Vector3 position, Quaternion rotation) {
            this.position = position;
            this.rotation = rotation;
            scale = Vector3.one;
        }

        public VirtualTransform(Vector3 position, Quaternion rotation, Vector3 scale) {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public void ApplyTo(Transform transform) {
            transform.position = position;
            transform.rotation = rotation;
            transform.localScale = scale;
        }

        public Vector3 TransformPoint(Vector3 point) {
            var matrix = Matrix4x4.TRS(position, rotation, scale);
            return matrix.MultiplyPoint3x4(point);
        }

        public VirtualTransform TransformVirtual(VirtualTransform other) {
            var matrix = Matrix4x4.TRS(position, rotation, scale);
            return new VirtualTransform(
                matrix.MultiplyPoint3x4(other.position),
                rotation * other.rotation,
                Vector3.Scale(scale, other.scale)
            );
        }

        public Vector3 InverseTransformPoint(Vector3 point) {
            var matrix = Matrix4x4.TRS(position, rotation, scale);
            return matrix.inverse.MultiplyPoint3x4(point);
        }

        public VirtualTransform InverseTransformVirtual(VirtualTransform other) {
            var matrix = Matrix4x4.TRS(position, rotation, scale);
            var inverseMatrix = matrix.inverse;
            return new VirtualTransform(
                inverseMatrix.MultiplyPoint3x4(other.position),
                Quaternion.Inverse(rotation) * other.rotation,
                Vector3.Scale(new Vector3(
                    Mathf.Approximately(scale.x, 0) ? 1f : 1f / scale.x,
                    Mathf.Approximately(scale.y, 0) ? 1f : 1f / scale.y,
                    Mathf.Approximately(scale.z, 0) ? 1f : 1f / scale.z
                ), other.scale)
            );
        }

        public STransform ToSerial() {
            return new STransform {
                point = SPoint.From(position),
                rotation = SRotation.From(rotation)
            };
        }

        public static VirtualTransform From(Transform transform) {
            return new VirtualTransform(transform.position, transform.rotation, transform.localScale);
        }

        public static VirtualTransform From(STransform transform) {
            return new VirtualTransform(transform.point.ToVector3(), transform.rotation.ToQuaternion());
        }

        public static VirtualTransform Lerp(VirtualTransform a, VirtualTransform b, float t) {
            return new VirtualTransform(
                Vector3.Lerp(a.position, b.position, t),
                Quaternion.Slerp(a.rotation, b.rotation, t),
                Vector3.Lerp(a.scale, b.scale, t)
            );
        }
    }
}