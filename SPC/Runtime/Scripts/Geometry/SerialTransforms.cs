using System;
using UnityEngine;

namespace Spookline.SPC.Geometry {
    [Serializable]
    public class SPoint {
        public float x;
        public float y;
        public float z;

        public static SPoint From(Vector3 vec3) {
            return new SPoint { x = vec3.x, y = vec3.y, z = vec3.z };
        }

        public Vector3 ToVector3() {
            return new Vector3(x, y, z);
        }
    }

    [Serializable]
    public class SRotation {
        public float x;
        public float y;
        public float z;
        public float w;

        public static SRotation From(Quaternion quat) {
            return new SRotation { x = quat.x, y = quat.y, z = quat.z, w = quat.w };
        }

        public Quaternion ToQuaternion() {
            return new Quaternion(x, y, z, w);
        }
    }


    [Serializable]
    public class STransform {
        public SPoint point;
        public SRotation rotation;

        public static STransform From(Transform transform) {
            return new STransform {
                point = SPoint.From(transform.position), rotation = SRotation.From(transform.rotation)
            };
        }

        public static STransform From(Vector3 position, Quaternion rotation) {
            return new STransform { point = SPoint.From(position), rotation = SRotation.From(rotation) };
        }

        public float DistanceTo(STransform other) {
            return Vector3.Distance(point.ToVector3(), other.point.ToVector3());
        }

        public Transform Apply(Transform transform) {
            transform.position = point.ToVector3();
            transform.rotation = rotation.ToQuaternion();
            return transform;
        }
    }

    public static class SerialExtensions {
        public static SPoint ToSerialPoint(this Vector3 vec3) {
            return SPoint.From(vec3);
        }

        public static SRotation ToSerialRotation(this Quaternion quat) {
            return SRotation.From(quat);
        }

        public static STransform ToSerialTransform(this Transform transform) {
            return STransform.From(transform);
        }
    }
}