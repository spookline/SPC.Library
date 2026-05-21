using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Draw.Poly {
    /// <summary>
    /// Factory for creating primitive draw commands with various builder overloads.
    /// Encapsulates all the complex geometry building logic.
    /// </summary>
    public static class PolyDrawCommandFactory {

        #region Triangle

        public static PolyDrawCommand Triangle(
            float3 a,
            float3 b,
            float3 c,
            float4 color,
            PolyDrawCommandFlags flags = PolyDrawCommandFlags.None
        ) {
            return TrianglePoints(a, b, c, color, flags);
        }

        public static PolyDrawCommand Triangle(
            float4x4 matrix,
            float3 a,
            float3 b,
            float3 c,
            float4 color,
            PolyDrawCommandFlags flags = PolyDrawCommandFlags.None
        ) {
            return TrianglePoints(
                math.transform(matrix, a),
                math.transform(matrix, b),
                math.transform(matrix, c),
                color,
                flags
            );
        }

        private static PolyDrawCommand TrianglePoints(
            float3 a,
            float3 b,
            float3 c,
            float4 color,
            PolyDrawCommandFlags flags
        ) {
            var matrix = float4x4.identity;

            matrix.c0 = new float4(a, 0f);
            matrix.c1 = new float4(b, 0f);
            matrix.c2 = new float4(c, 0f);
            matrix.c3 = new float4(0f, 0f, 0f, 1f);

            return new PolyDrawCommand {
                matrix = matrix,
                color = color,
                args = default,
                type = PolyDrawCommandType.Triangle,
                flags = (ushort)flags,
            };
        }

        #endregion

        #region Quad

        public static PolyDrawCommand Quad(
            float3 a,
            float3 b,
            float3 c,
            float3 d,
            float4 color,
            PolyDrawCommandFlags flags = PolyDrawCommandFlags.None
        ) {
            return QuadPoints(a, b, c, d, color, flags);
        }

        public static PolyDrawCommand Quad(
            float4x4 matrix,
            float3 a,
            float3 b,
            float3 c,
            float3 d,
            float4 color,
            PolyDrawCommandFlags flags = PolyDrawCommandFlags.None
        ) {
            return QuadPoints(
                math.transform(matrix, a),
                math.transform(matrix, b),
                math.transform(matrix, c),
                math.transform(matrix, d),
                color,
                flags
            );
        }

        private static PolyDrawCommand QuadPoints(
            float3 a,
            float3 b,
            float3 c,
            float3 d,
            float4 color,
            PolyDrawCommandFlags flags
        ) {
            var matrix = float4x4.identity;

            matrix.c0 = new float4(a, 0f);
            matrix.c1 = new float4(b, 0f);
            matrix.c2 = new float4(c, 0f);
            matrix.c3 = new float4(d, 1f);

            return new PolyDrawCommand {
                matrix = matrix,
                color = color,
                args = default,
                type = PolyDrawCommandType.Quad,
                flags = (ushort)flags
            };
        }

        #endregion

        #region Cube

        public static PolyDrawCommand Cube(
            float4x4 matrix,
            float4 color,
            PolyDrawCommandFlags flags = PolyDrawCommandFlags.None
        ) {
            return new PolyDrawCommand {
                matrix = matrix,
                color = color,
                args = default,
                type = PolyDrawCommandType.Cube,
                flags = (ushort)flags,
            };
        }

        public static PolyDrawCommand Cube(
            float3 center,
            float3 size,
            float4 color,
            PolyDrawCommandFlags flags = PolyDrawCommandFlags.None
        ) {
            return Cube(float4x4.TRS(center, quaternion.identity, size), color, flags);
        }

        public static PolyDrawCommand Cube(
            float3 center,
            quaternion rotation,
            float3 size,
            float4 color,
            PolyDrawCommandFlags flags = PolyDrawCommandFlags.None
        ) {
            return Cube(float4x4.TRS(center, rotation, size), color, flags);
        }

        public static PolyDrawCommand Cube(
            float4x4 matrix,
            float3 center,
            float3 size,
            float4 color,
            PolyDrawCommandFlags flags = PolyDrawCommandFlags.None
        ) {
            return Cube(math.mul(matrix, float4x4.TRS(center, quaternion.identity, size)), color, flags);
        }

        public static PolyDrawCommand Cube(
            float4x4 matrix,
            float3 center,
            quaternion rotation,
            float3 size,
            float4 color,
            PolyDrawCommandFlags flags = PolyDrawCommandFlags.None
        ) {
            return Cube(math.mul(matrix, float4x4.TRS(center, rotation, size)), color, flags);
        }

        #endregion

        #region Sphere

        public static PolyDrawCommand Sphere(
            float4x4 matrix,
            float radius,
            float segments,
            float4 color,
            PolyDrawCommandFlags flags = PolyDrawCommandFlags.None
        ) {
            return new PolyDrawCommand {
                matrix = matrix,
                color = color,
                args = new float3(radius, segments, 0f),
                type = PolyDrawCommandType.Sphere,
                flags = (ushort)flags,
            };
        }

        public static PolyDrawCommand Sphere(
            float3 center,
            float radius,
            float segments,
            float4 color,
            PolyDrawCommandFlags flags = PolyDrawCommandFlags.None
        ) {
            return Sphere(float4x4.Translate(center), radius, segments, color, flags);
        }

        public static PolyDrawCommand Sphere(
            float4x4 matrix,
            float3 center,
            float radius,
            float segments,
            float4 color,
            PolyDrawCommandFlags flags = PolyDrawCommandFlags.None
        ) {
            return Sphere(math.mul(matrix, float4x4.Translate(center)), radius, segments, color, flags);
        }

        #endregion

        #region Disc

        public static PolyDrawCommand Disc(
            float4x4 matrix,
            float radius,
            float segments,
            float4 color,
            PolyDrawCommandFlags flags = PolyDrawCommandFlags.None
        ) {
            return new PolyDrawCommand {
                matrix = matrix,
                color = color,
                args = new float3(radius, segments, 0f),
                type = PolyDrawCommandType.Disc,
                flags = (ushort)flags
            };
        }

        public static PolyDrawCommand Disc(
            float3 center,
            float3 normal,
            float radius,
            float segments,
            float4 color,
            PolyDrawCommandFlags flags = PolyDrawCommandFlags.None
        ) {
            return Disc(BasisFromNormal(center, normal), radius, segments, color, flags);
        }

        public static PolyDrawCommand Disc(
            float4x4 matrix,
            float3 center,
            float3 normal,
            float radius,
            float segments,
            float4 color,
            PolyDrawCommandFlags flags = PolyDrawCommandFlags.None
        ) {
            return Disc(math.mul(matrix, BasisFromNormal(center, normal)), radius, segments, color, flags);
        }

        #endregion

        #region Arc

        public static PolyDrawCommand Arc(
            float4x4 matrix,
            float radius,
            float segments,
            float angleDegrees,
            float4 color,
            PolyDrawCommandFlags flags = PolyDrawCommandFlags.None
        ) {
            return new PolyDrawCommand {
                matrix = matrix,
                color = color,
                args = new float3(radius, segments, angleDegrees),
                type = PolyDrawCommandType.Arc,
                flags = (ushort)flags,
            };
        }

        public static PolyDrawCommand Arc(
            float3 center,
            float3 normal,
            float3 from,
            float radius,
            float segments,
            float angleDegrees,
            float4 color,
            PolyDrawCommandFlags flags = PolyDrawCommandFlags.None
        ) {
            return Arc(BasisFromNormalAndFrom(center, normal, from), radius, segments, angleDegrees, color, flags);
        }

        public static PolyDrawCommand Arc(
            float4x4 matrix,
            float3 center,
            float3 normal,
            float3 from,
            float radius,
            float segments,
            float angleDegrees,
            float4 color,
            PolyDrawCommandFlags flags = PolyDrawCommandFlags.None
        ) {
            return Arc(
                math.mul(matrix, BasisFromNormalAndFrom(center, normal, from)),
                radius,
                segments,
                angleDegrees,
                color,
                flags
            );
        }

        #endregion

        #region Helpers

        public static float4 Color(Color color) {
            return new float4(color.r, color.g, color.b, color.a);
        }

        private static float4x4 BasisFromNormal(float3 center, float3 normal) {
            var z = math.normalizesafe(normal, new float3(0f, 0f, 1f));

            var reference = math.abs(z.y) < 0.999f
                ? new float3(0f, 1f, 0f)
                : new float3(1f, 0f, 0f);

            var x = math.normalizesafe(math.cross(reference, z), new float3(1f, 0f, 0f));
            var y = math.cross(z, x);

            return new float4x4(
                new float4(x, 0f),
                new float4(y, 0f),
                new float4(z, 0f),
                new float4(center, 1f)
            );
        }

        private static float4x4 BasisFromNormalAndFrom(float3 center, float3 normal, float3 from) {
            var z = math.normalizesafe(normal, new float3(0f, 0f, 1f));

            var projectedFrom = from - z * math.dot(from, z);
            var x = math.normalizesafe(projectedFrom, default);

            if (math.lengthsq(x) < 1e-8f) {
                var reference = math.abs(z.y) < 0.999f
                    ? new float3(0f, 1f, 0f)
                    : new float3(1f, 0f, 0f);

                x = math.normalizesafe(math.cross(reference, z), new float3(1f, 0f, 0f));
            }

            var y = math.cross(z, x);

            return new float4x4(
                new float4(x, 0f),
                new float4(y, 0f),
                new float4(z, 0f),
                new float4(center, 1f)
            );
        }

        #endregion

    }
}

