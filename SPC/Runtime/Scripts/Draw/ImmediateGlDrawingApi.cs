using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Spookline.SPC.Draw {
    public class ImmediateGlDrawingApi : IDrawingAPI, IDisposable {

        private static Material _material;

        public static Material Material {
            get {
                if (_material) return _material;

                _material = Resources.Load<Material>("PolyDrawGLURP");
                if (_material) {
                    return _material;
                }

                var shader = Shader.Find("Hidden/Internal-Colored");
                _material = new Material(shader) {
                    hideFlags = HideFlags.HideAndDontSave
                };

                _material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                _material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                _material.SetInt("_Cull", (int)CullMode.Off);
                _material.SetInt("_ZWrite", 0);
                _material.SetInt("_ZTest", (int)CompareFunction.Always);

                return _material;
            }
        }

        private Matrix4x4 _matrix = Matrix4x4.identity;
        private Color _color = Color.white;

        public Matrix4x4 Matrix {
            get => _matrix;
            set => _matrix = value;
        }

        public Color Color {
            get => _color;
            set => _color = value;
        }

        void Begin(int mode) {
            Material.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(_matrix);
            GL.Begin(mode);
            GL.Color(_color);
        }

        static void End() {
            GL.End();
            GL.PopMatrix();
        }

        public void Line(Vector3 a, Vector3 b) {
            Begin(GL.LINES);
            GL.Vertex(a);
            GL.Vertex(b);
            End();
        }

        public void Ray(Vector3 origin, Vector3 direction) {
            Line(origin, origin + direction);
        }

        public void Lines(ReadOnlySpan<Vector3> points) {
            if (points.Length < 2) return;

            Begin(GL.LINES);

            for (var i = 0; i + 1 < points.Length; i += 2) {
                GL.Vertex(points[i]);
                GL.Vertex(points[i + 1]);
            }

            End();
        }

        public void Strip(ReadOnlySpan<Vector3> points, bool closed = false) {
            if (points.Length < 2) return;

            Begin(GL.LINE_STRIP);

            for (var i = 0; i < points.Length; i++) { GL.Vertex(points[i]); }

            if (closed) { GL.Vertex(points[0]); }

            End();
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c) {
            Begin(GL.TRIANGLES);
            GL.Vertex(a);
            GL.Vertex(b);
            GL.Vertex(c);
            End();
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
            Begin(GL.TRIANGLES);

            GL.Vertex(a);
            GL.Vertex(b);
            GL.Vertex(c);

            GL.Vertex(a);
            GL.Vertex(c);
            GL.Vertex(d);

            End();
        }

        public void Cube(Vector3 center, Vector3 size) {
            var h = size * 0.5f;

            var p000 = center + new Vector3(-h.x, -h.y, -h.z);
            var p100 = center + new Vector3(h.x, -h.y, -h.z);
            var p110 = center + new Vector3(h.x, h.y, -h.z);
            var p010 = center + new Vector3(-h.x, h.y, -h.z);

            var p001 = center + new Vector3(-h.x, -h.y, h.z);
            var p101 = center + new Vector3(h.x, -h.y, h.z);
            var p111 = center + new Vector3(h.x, h.y, h.z);
            var p011 = center + new Vector3(-h.x, h.y, h.z);

            Quad(p000, p100, p110, p010); // back
            Quad(p101, p001, p011, p111); // front
            Quad(p001, p000, p010, p011); // left
            Quad(p100, p101, p111, p110); // right
            Quad(p010, p110, p111, p011); // top
            Quad(p001, p101, p100, p000); // bottom
        }

        public void WireCube(Vector3 center, Vector3 size) {
            DrawingApiDefaults<ImmediateGlDrawingApi>.WireCube(this, center, size);
        }

        public void Sphere(Vector3 center, float radius, int segments = 16) {
            segments = math.max(4, segments);
            radius = math.max(0f, radius);

            var rings = math.max(2, segments / 2);

            Begin(GL.TRIANGLES);

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

                    GL.Vertex(a);
                    GL.Vertex(b);
                    GL.Vertex(c);

                    GL.Vertex(a);
                    GL.Vertex(c);
                    GL.Vertex(d);
                }
            }

            End();
        }

        static Vector3 SpherePoint(Vector3 center, float radius, float lat, float lon) {
            var cosLat = math.cos(lat);

            return center + new Vector3(
                cosLat * math.cos(lon),
                math.sin(lat),
                cosLat * math.sin(lon)
            ) * radius;
        }

        public void WireSphere(Vector3 center, float radius, int segments = 16) {
            DrawingApiDefaults<ImmediateGlDrawingApi>.WireSphere(this, center, radius, segments);
        }

        public void Disc(Vector3 center, Vector3 normal, float radius, int segments = 16) {
            segments = math.max(3, segments);
            radius = math.max(0f, radius);

            var n = normal.sqrMagnitude > 1e-10f
                ? normal.normalized
                : Vector3.up;

            var axisA = DrawingApiDefaults<ImmediateGlDrawingApi>.GetFallbackAxis(n);
            var axisB = Vector3.Cross(n, axisA).normalized;

            var step = math.TAU / segments;

            Begin(GL.TRIANGLES);

            for (var i = 0; i < segments; i++) {
                var t0 = step * i;
                var t1 = step * (i + 1);

                var a = center + (axisA * math.cos(t0) + axisB * math.sin(t0)) * radius;
                var b = center + (axisA * math.cos(t1) + axisB * math.sin(t1)) * radius;

                GL.Vertex(center);
                GL.Vertex(a);
                GL.Vertex(b);
            }

            End();
        }

        public void WireDisc(Vector3 center, Vector3 normal, float radius, int segments = 16) {
            DrawingApiDefaults<ImmediateGlDrawingApi>.WireDisc(this, center, normal, radius, segments);
        }

        public void Arc(
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

            Begin(GL.TRIANGLES);

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

                GL.Vertex(center);
                GL.Vertex(a);
                GL.Vertex(b);
            }

            End();
        }

        public void WireArc(
            Vector3 center,
            Vector3 normal,
            Vector3 from,
            float radius,
            float angle,
            int segments = 16
        ) {
            DrawingApiDefaults<ImmediateGlDrawingApi>.WireArc(this, center, normal, from, radius, angle, segments);
        }

        public void Dispose() { }

    }
}