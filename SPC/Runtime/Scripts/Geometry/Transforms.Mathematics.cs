using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Geometry {
    public static partial class Transforms {

        public static AffineTransform Deltas(AffineTransform before, AffineTransform after) {
            return math.mul(math.inverse(before), after);
        }

        public static float4x4 MatrixDeltas(AffineTransform before, AffineTransform after) {
            float4x4 beforeMatrix = before;
            float4x4 afterMatrix = after;
            return math.mul(math.inverse(beforeMatrix), afterMatrix);
        }

        public static TRS Decompose(this AffineTransform transform) {
            math.decompose(transform, out var pos, out var rot, out var scale);
            return new TRS(pos, rot, scale);
        }

        public static TRS Decompose(this RigidTransform transform) => transform;

        public static AffineTransform Inverse(this AffineTransform transform) => math.inverse(transform);
        public static float4x4 Inverse(this float4x4 transform) => math.inverse(transform);

        public static float4x4 Transform(
            this AffineTransform transform,
            float4x4 matrix
        ) {
            return math.mul(transform, math.AffineTransform(matrix));
        }

        public static AffineTransform Transform(
            this AffineTransform transform,
            AffineTransform other
        ) =>
            math.mul(transform, other);

        public static float3 TransformPoint(
            this AffineTransform transform,
            float3 point
        ) =>
            math.transform(transform, point);

        public static float3 TransformPoint(
            this float4x4 transform,
            float3 point
        ) =>
            math.transform(transform, point);


        public static AffineTransform Transform(
            this float4x4 transform,
            AffineTransform affine
        ) {
            float4x4 matrix = affine;
            matrix = math.mul(transform, matrix);
            return math.AffineTransform(matrix);
        }

        public static quaternion Rotate(
            this AffineTransform transform,
            quaternion rotation
        ) {
            return math.mul(math.rotation(transform.rs), rotation);
        }

        public static quaternion Rotate(
            this float4x4 transform,
            quaternion rotation
        ) {
            var linear = new float3x3(transform);
            var linearRot = ExtractRotation(linear);
            return math.normalize(math.mul(new quaternion(linearRot), rotation));
        }

        private static float3x3 ExtractRotation(float3x3 linear) {
            var x = linear.c0;
            var y = linear.c1;
            var z = linear.c2;
            x = math.normalizesafe(x, new float3(1, 0, 0));
            y -= x * math.dot(x, y);
            y = math.normalizesafe(y, new float3(0, 1, 0));
            z = math.cross(x, y);
            if (math.dot(z, linear.c2) < 0f) z = -z;
            return new float3x3(x, y, z);
        }

        public static float3 TransformDirection(
            this AffineTransform transform,
            float3 direction
        ) =>
            math.rotate(transform, direction);

        public static float3 Scale(
            this AffineTransform transform,
            float3 scale
        ) {
            math.decompose(transform, out _, out _, out var s);
            return s * scale;
        }

        public static float3 Scale(
            this float4x4 transform,
            float3 scale
        ) {
            var linear = new float3x3(transform);
            var x = math.length(linear.c0);
            var y = math.length(linear.c1);
            var z = math.length(linear.c2);
            return new float3(x, y, z) * scale;
        }

    }
}