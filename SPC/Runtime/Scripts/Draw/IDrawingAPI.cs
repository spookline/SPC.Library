using System;
using System.Buffers;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Draw {
    public interface IDrawingAPI {

        void Line(Vector3 a, Vector3 b);
        void Ray(Vector3 origin, Vector3 direction) => Line(origin, origin + direction);

        void Lines(ReadOnlySpan<Vector3> points);
        void Strip(ReadOnlySpan<Vector3> points, bool closed = false);


        void Triangle(Vector3 a, Vector3 b, Vector3 c);
        void Triangles(ReadOnlySpan<Vector3> points);

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d);

        void Cube(Vector3 center, Vector3 size);
        void WireCube(Vector3 center, Vector3 size);
        void Sphere(Vector3 center, float radius, int segments = 16);
        void WireSphere(Vector3 center, float radius, int segments = 16);


        void Disc(Vector3 center, Vector3 normal, float radius, int segments = 16);

        void WireDisc(Vector3 center, Vector3 normal, float radius, int segments = 16);
        void Arc(Vector3 center, Vector3 normal, Vector3 from, float radius, float angle, int segments = 16);

        void WireArc(Vector3 center, Vector3 normal, Vector3 from, float radius, float angle, int segments = 16);

        void Mesh(Mesh mesh);
        void WireMesh(Mesh mesh);

        void LineBuffer(PolyDrawBuffer buffer);
        void MeshBuffer(PolyDrawBuffer buffer);

        Matrix4x4 Matrix { get; set; }
        Color Color { get; set; }

    }

    public static class DrawingApiDefaults<T> where T : IDrawingAPI {

        public static void Triangle(T api, Vector3 a, Vector3 b, Vector3 c) {
            Span<Vector3> points = stackalloc Vector3[3];
            points[0] = a;
            points[1] = b;
            points[2] = c;
            api.Strip(points, true);
        }

        public static void Triangles(T api, ReadOnlySpan<Vector3> points) {
            for (var i = 0; i + 2 < points.Length; i += 3) { Triangle(api, points[i], points[i + 1], points[i + 2]); }
        }

        public static void Quad(T api, Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
            Span<Vector3> points = stackalloc Vector3[4];
            points[0] = a;
            points[1] = b;
            points[2] = c;
            points[3] = d;
            api.Strip(points, true);
        }

        public static void WireSphere(T api, Vector3 center, float radius, int segments = 16) {
            WireArc(api, center, Vector3.up, Vector3.forward, radius, 360f, segments);
            WireArc(api, center, Vector3.right, Vector3.forward, radius, 360f, segments);
            WireArc(api, center, Vector3.forward, Vector3.up, radius, 360f, segments);
        }

        public static void Sphere(T api, Vector3 center, float radius, int segments = 16) {
            segments = math.max(4, segments);
            radius = math.max(0f, radius);

            var rings = math.max(2, segments / 2);

            for (var y = 0; y < rings; y++) {
                var v0 = (float)y / rings;
                var v1 = (float)(y + 1) / rings;

                var lat0 = math.lerp(-math.PI * 0.5f, math.PI * 0.5f, v0);
                var lat1 = math.lerp(-math.PI * 0.5f, math.PI * 0.5f, v1);

                for (var x = 0; x < segments; x++) {
                    var u0 = (float)x / segments;
                    var u1 = (float)(x + 1) / segments;

                    var lon0 = u0 * math.TAU;
                    var lon1 = u1 * math.TAU;

                    var a = SpherePoint(center, radius, lat0, lon0);
                    var b = SpherePoint(center, radius, lat0, lon1);
                    var c = SpherePoint(center, radius, lat1, lon1);
                    var d = SpherePoint(center, radius, lat1, lon0);

                    // Quad → two triangles
                    api.Triangle(a, b, c);
                    api.Triangle(a, c, d);
                }
            }
        }

        public static void WireCube(T api, Vector3 center, Vector3 size) {
            var halfSize = size / 2;

            Span<Vector3> corners = stackalloc Vector3[8];
            corners[0] = center - halfSize;
            corners[1] = center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
            corners[2] = center + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            corners[3] = center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            corners[4] = center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            corners[5] = center + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            corners[6] = center + halfSize;
            corners[7] = center + new Vector3(-halfSize.x, halfSize.y, halfSize.z);

            api.Line(corners[0], corners[1]);
            api.Line(corners[1], corners[2]);
            api.Line(corners[2], corners[3]);
            api.Line(corners[3], corners[0]);

            api.Line(corners[4], corners[5]);
            api.Line(corners[5], corners[6]);
            api.Line(corners[6], corners[7]);
            api.Line(corners[7], corners[4]);

            api.Line(corners[0], corners[4]);
            api.Line(corners[1], corners[5]);
            api.Line(corners[2], corners[6]);
            api.Line(corners[3], corners[7]);
        }

        public static void Cube(T api, Vector3 center, Vector3 size) {
            var halfSize = size / 2;

            Span<Vector3> corners = stackalloc Vector3[8];
            corners[0] = center - halfSize;
            corners[1] = center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
            corners[2] = center + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            corners[3] = center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            corners[4] = center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            corners[5] = center + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            corners[6] = center + halfSize;
            corners[7] = center + new Vector3(-halfSize.x, halfSize.y, halfSize.z);

            api.Quad(corners[0], corners[1], corners[2], corners[3]);
            api.Quad(corners[4], corners[5], corners[6], corners[7]);
            api.Quad(corners[0], corners[4], corners[5], corners[1]);
            api.Quad(corners[2], corners[6], corners[7], corners[3]);
            api.Quad(corners[1], corners[5], corners[6], corners[2]);
            api.Quad(corners[0], corners[3], corners[7], corners[4]);
        }

        public static void WireDisc(
            T api,
            Vector3 center,
            Vector3 normal,
            float radius,
            int segments = 16
        ) {
            segments = math.max(3, segments);
            radius = math.max(0f, radius);

            var size = segments;

            if (segments > 128) {
                var array = ArrayPool<Vector3>.Shared.Rent(size);
                try { Render(array.AsSpan(0, size)); } finally { ArrayPool<Vector3>.Shared.Return(array); }
            } else {
                Span<Vector3> span = stackalloc Vector3[size];
                Render(span);
            }

            return;

            void Render(Span<Vector3> span) {
                var n = normal.sqrMagnitude > 1e-10f
                    ? normal.normalized
                    : Vector3.up;

                var axisA = GetFallbackAxis(n);
                var axisB = Vector3.Cross(n, axisA).normalized;

                var step = math.TAU / segments;

                for (var i = 0; i < segments; i++) {
                    var theta = step * i;

                    span[i] = center + (
                        axisA * math.cos(theta) +
                        axisB * math.sin(theta)
                    ) * radius;
                }

                api.Strip(span, true);
            }
        }

        public static void Disc(
            T api,
            Vector3 center,
            Vector3 normal,
            float radius,
            int segments = 16
        ) {
            segments = math.max(3, segments);
            radius = math.max(0f, radius);

            var n = normal.sqrMagnitude > 1e-10f
                ? normal.normalized
                : Vector3.up;

            var axisA = DrawingApiDefaults<ImmediateGlDrawingApi>.GetFallbackAxis(n);
            var axisB = Vector3.Cross(n, axisA).normalized;

            var step = math.TAU / segments;

            for (var i = 0; i < segments; i++) {
                var t0 = step * i;
                var t1 = step * (i + 1);

                var a = center + (axisA * math.cos(t0) + axisB * math.sin(t0)) * radius;
                var b = center + (axisA * math.cos(t1) + axisB * math.sin(t1)) * radius;

                api.Triangle(center, a, b);
            }
        }


        public static void WireArc(
            T api,
            Vector3 center,
            Vector3 normal,
            Vector3 from,
            float radius,
            float angle,
            int segments = 16
        ) {
            segments = math.max(1, segments);
            radius = math.max(0f, radius);
            angle = math.clamp(angle * math.TORADIANS, -math.TAU, math.TAU);

            var size = segments + 1;

            if (segments > 128) {
                var array = ArrayPool<Vector3>.Shared.Rent(size);
                try { Render(array.AsSpan(0, size)); } finally { ArrayPool<Vector3>.Shared.Return(array); }
            } else {
                Span<Vector3> span = stackalloc Vector3[size];
                Render(span);
            }

            return;

            void Render(Span<Vector3> span) {
                var n = normal.sqrMagnitude > 1e-10f
                    ? normal.normalized
                    : Vector3.up;

                // Project "from" onto the arc plane. This lets callers pass any
                // approximately correct direction without requiring it to be perfectly planar.
                var projectedFrom = from - n * Vector3.Dot(from, n);

                var axisA = projectedFrom.sqrMagnitude > 1e-10f
                    ? projectedFrom.normalized
                    : GetFallbackAxis(n);

                // Positive angle follows right-handed rotation around "normal".
                var axisB = Vector3.Cross(n, axisA).normalized;

                for (var segment = 0; segment <= segments; segment++) {
                    var t = (float)segment / segments;
                    var theta = angle * t;

                    span[segment] = center + (
                        axisA * math.cos(theta) +
                        axisB * math.sin(theta)
                    ) * radius;
                }

                api.Strip(span, false);
            }
        }

        public static void Arc(
            T api,
            Vector3 center,
            Vector3 normal,
            Vector3 from,
            float radius,
            float angle,
            int segments = 16
        ) {
            segments = math.max(1, segments);
            radius = math.max(0f, radius);
            angle = math.clamp(angle * math.TORADIANS, -math.TAU, math.TAU);

            if (radius <= 0f || math.abs(angle) <= 1e-6f) return;

            var n = normal.sqrMagnitude > 1e-10f
                ? normal.normalized
                : Vector3.up;

            var projectedFrom = from - n * Vector3.Dot(from, n);

            var axisA = projectedFrom.sqrMagnitude > 1e-10f
                ? projectedFrom.normalized
                : DrawingApiDefaults<ImmediateGlDrawingApi>.GetFallbackAxis(n);

            var axisB = Vector3.Cross(n, axisA).normalized;

            for (var i = 0; i < segments; i++) {
                var t0 = (float)i / segments;
                var t1 = (float)(i + 1) / segments;

                var theta0 = angle * t0;
                var theta1 = angle * t1;

                var a = center + (
                    axisA * math.cos(theta0) +
                    axisB * math.sin(theta0)
                ) * radius;

                var b = center + (
                    axisA * math.cos(theta1) +
                    axisB * math.sin(theta1)
                ) * radius;

                api.Triangle(center, a, b);
            }
        }

        public static void Cone(T api, Vector3 origin, Vector3 direction, float length, float angle, int segments = 8) {
            segments = math.max(3, segments);
            length = math.max(0f, length);
            angle /= 2;

            if (length <= 0f) { return; }

            var axis = direction.sqrMagnitude > 1e-10f
                ? direction.normalized
                : Vector3.forward;

            // Treat angle as a half-angle in degrees, consistent with WireArc.
            angle = math.clamp(math.abs(angle), 0f, 180f);
            var angleRad = angle * math.TORADIANS;

            // Degenerate cone: just draw the center ray.
            if (angle <= 1e-4f) {
                api.Line(origin, origin + axis * length);
                return;
            }

            var basisA = GetFallbackAxis(axis);
            var basisB = Vector3.Cross(axis, basisA).normalized;

            // Spherical-section rim: all rim points are exactly `length` from origin.
            var rimCenter = origin + axis * (math.cos(angleRad) * length);
            var rimRadius = math.sin(angleRad) * length;

            var size = segments;

            if (segments > 128) {
                var array = ArrayPool<Vector3>.Shared.Rent(size);
                try { Render(array.AsSpan(0, size)); } finally { ArrayPool<Vector3>.Shared.Return(array); }
            } else {
                Span<Vector3> span = stackalloc Vector3[size];
                Render(span);
            }

            return;

            void Render(Span<Vector3> rim) {
                var step = math.TAU / segments;

                for (var i = 0; i < segments; i++) {
                    var theta = step * i;

                    rim[i] = rimCenter + (
                        basisA * math.cos(theta) +
                        basisB * math.sin(theta)
                    ) * rimRadius;
                }

                var a = rimCenter + (basisA * math.cos(0) + basisB * math.sin(0)) * rimRadius;
                var b = rimCenter + (basisA * math.cos(math.TAU * 0.25f) + basisB * math.sin(math.TAU * 0.25f)) *
                    rimRadius;
                var c = rimCenter + (basisA * math.cos(math.TAU * 0.50f) + basisB * math.sin(math.TAU * 0.50f)) *
                    rimRadius;
                var d = rimCenter + (basisA * math.cos(math.TAU * 0.75f) + basisB * math.sin(math.TAU * 0.75f)) *
                    rimRadius;

                // Rim of the spherical section.
                api.Strip(rim, true);

                // Center axis.
                //Line(origin, origin + axis * length);

                // Four representative cone boundary rays.
                api.Line(origin, a);
                api.Line(origin, b);
                api.Line(origin, c);
                api.Line(origin, d);

                // Four spherical meridian arcs from the axis direction to the rim.
                api.WireArc(origin, Vector3.Cross(axis, basisA).normalized, axis, length, angle, segments);
                api.WireArc(origin, Vector3.Cross(axis, -basisA).normalized, axis, length, angle, segments);
                api.WireArc(origin, Vector3.Cross(axis, basisB).normalized, axis, length, angle, segments);
                api.WireArc(origin, Vector3.Cross(axis, -basisB).normalized, axis, length, angle, segments);
            }
        }

        public static void Mesh(T api, Mesh mesh) {
            var indices = mesh.triangles;
            var vertices = mesh.vertices;

            var lineCount = indices.Length;
            if (lineCount > 128) {
                var array = ArrayPool<Vector3>.Shared.Rent(lineCount);
                try { Render(array.AsSpan(0, lineCount)); } finally { ArrayPool<Vector3>.Shared.Return(array); }
            } else {
                Span<Vector3> span = stackalloc Vector3[lineCount];
                Render(span);
            }

            return;

            void Render(Span<Vector3> triangles) {
                var triangleIndex = 0;
                for (var i = 0; i < triangles.Length; i += 3) {
                    var a = vertices[indices[i]];
                    var b = vertices[indices[i + 1]];
                    var c = vertices[indices[i + 2]];

                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;
                }

                api.Triangles(triangles);
            }
        }

        public static void WireMesh(T api, Mesh mesh) {
            var indices = mesh.triangles;
            var vertices = mesh.vertices;

            var lineCount = indices.Length * 2;
            if (lineCount > 128) {
                var array = ArrayPool<Vector3>.Shared.Rent(lineCount);
                try { Render(array.AsSpan(0, lineCount)); } finally { ArrayPool<Vector3>.Shared.Return(array); }
            } else {
                Span<Vector3> span = stackalloc Vector3[lineCount];
                Render(span);
            }

            return;

            void Render(Span<Vector3> lines) {
                var lineIndex = 0;
                for (var i = 0; i < indices.Length; i += 3) {
                    var a = vertices[indices[i]];
                    var b = vertices[indices[i + 1]];
                    var c = vertices[indices[i + 2]];

                    lines[lineIndex++] = a;
                    lines[lineIndex++] = b;

                    lines[lineIndex++] = b;
                    lines[lineIndex++] = c;

                    lines[lineIndex++] = c;
                    lines[lineIndex++] = a;
                }

                api.Lines(lines);
            }
        }

        public static void MeshBuffer(T api, PolyDrawBuffer buffer) {
            var indices = buffer.indices;
            var vertices = buffer.vertices;

            if (indices.Length > 128) {
                var array = ArrayPool<Vector3>.Shared.Rent(indices.Length);
                try { Render(array.AsSpan(0, indices.Length)); } finally { ArrayPool<Vector3>.Shared.Return(array); }
            } else {
                Span<Vector3> span = stackalloc Vector3[indices.Length];
                Render(span);
            }

            return;

            void Render(Span<Vector3> triangles) {
                var triangleIndex = 0;
                for (var i = 0; i < indices.Length; i++) {
                    var vertexIndex = indices[i];
                    var v = vertices[vertexIndex];
                    triangles[triangleIndex++] = v.position;
                }

                api.Triangles(triangles);
            }
        }

        public static void LineBuffer(T api, PolyDrawBuffer buffer) {
            var indices = buffer.indices;
            var vertices = buffer.vertices;

            if (indices.Length > 128) {
                var array = ArrayPool<Vector3>.Shared.Rent(indices.Length);
                try { Render(array.AsSpan(0, indices.Length)); } finally { ArrayPool<Vector3>.Shared.Return(array); }
            } else {
                Span<Vector3> span = stackalloc Vector3[indices.Length];
                Render(span);
            }

            return;

            void Render(Span<Vector3> lines) {
                var lineIndex = 0;

                for (var i = 0; i < indices.Length; i++) {
                    var vertexIndex = indices[i];
                    var v = vertices[vertexIndex];
                    lines[lineIndex++] = v.position;
                }

                api.Lines(lines);
            }
        }

        public static Vector3 GetFallbackAxis(Vector3 n) {
            var reference = math.abs(Vector3.Dot(n, Vector3.right)) > 0.999f
                ? Vector3.forward
                : Vector3.right;

            return (reference - n * Vector3.Dot(reference, n)).normalized;
        }

        private static Vector3 SpherePoint(Vector3 center, float radius, float lat, float lon) {
            var cosLat = math.cos(lat);

            return center + new Vector3(
                cosLat * math.cos(lon),
                math.sin(lat),
                cosLat * math.sin(lon)
            ) * radius;
        }

    }
}