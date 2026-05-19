using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spookline.SPC.Draw {
    public sealed class BufferedGlDrawingApi : IDrawingAPI, IDisposable {

        // TODO: Think about some way to not kill performance using the matrix multiplications

        readonly List<Vector3> _lines = new(1);
        readonly List<Vector3> _triangles = new(1);

        public Matrix4x4 Matrix { get; set; } = Matrix4x4.identity;
        public Color Color { get; set; } = Color.white;

        public void Line(Vector3 a, Vector3 b) {
            _lines.Add(Matrix.MultiplyPoint3x4(a));
            _lines.Add(Matrix.MultiplyPoint3x4(b));
        }

        public void Lines(ReadOnlySpan<Vector3> points) {
            for (var i = 0; i + 1 < points.Length; i += 2) Line(points[i], points[i + 1]);
        }

        public void Strip(ReadOnlySpan<Vector3> points, bool closed = false) {
            for (var i = 0; i + 1 < points.Length; i++) Line(points[i], points[i + 1]);
            if (closed && points.Length > 1) Line(points[^1], points[0]);
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c) {
            _triangles.Add(Matrix.MultiplyPoint3x4(a));
            _triangles.Add(Matrix.MultiplyPoint3x4(b));
            _triangles.Add(Matrix.MultiplyPoint3x4(c));
        }

        public void Triangles(ReadOnlySpan<Vector3> points) {
            DrawingApiDefaults<BufferedGlDrawingApi>.Triangles(this, points);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
            Triangle(a, b, c);
            Triangle(a, c, d);
        }

        public void Flush(Material material) {
            material.SetPass(0);

            if (_lines.Count > 0) {
                GL.Begin(GL.LINES);
                GL.Color(Color);

                for (var i = 0; i < _lines.Count; i++) GL.Vertex(_lines[i]);

                GL.End();
                _lines.Clear();
            }

            if (_triangles.Count > 0) {
                GL.Begin(GL.TRIANGLES);
                GL.Color(Color);

                for (var i = 0; i < _triangles.Count; i++) GL.Vertex(_triangles[i]);

                GL.End();
                _triangles.Clear();
            }
        }

        public void Ray(Vector3 origin, Vector3 direction) => Line(origin, origin + direction);

        public void WireCube(Vector3 center, Vector3 size) =>
            DrawingApiDefaults<BufferedGlDrawingApi>.WireCube(this, center, size);

        public void WireSphere(Vector3 center, float radius, int segments = 16) =>
            DrawingApiDefaults<BufferedGlDrawingApi>.WireSphere(this, center, radius, segments);

        public void WireDisc(Vector3 center, Vector3 normal, float radius, int segments = 16) =>
            DrawingApiDefaults<BufferedGlDrawingApi>.WireDisc(this, center, normal, radius, segments);

        public void WireArc(
            Vector3 center,
            Vector3 normal,
            Vector3 from,
            float radius,
            float angle,
            int segments = 16
        ) =>
            DrawingApiDefaults<BufferedGlDrawingApi>.WireArc(this, center, normal, from, radius, angle, segments);

        public void WireMesh(Mesh mesh) {
            DrawingApiDefaults<BufferedGlDrawingApi>.WireMesh(this, mesh);
        }

        public void Mesh(Mesh mesh) {
            DrawingApiDefaults<BufferedGlDrawingApi>.Mesh(this, mesh);
        }

        public void Cube(Vector3 center, Vector3 size) {
            DrawingApiDefaults<BufferedGlDrawingApi>.Cube(this, center, size);
        }

        public void Sphere(Vector3 center, float radius, int segments = 16) {
            DrawingApiDefaults<BufferedGlDrawingApi>.Sphere(this, center, radius, segments);
        }

        public void Disc(Vector3 center, Vector3 normal, float radius, int segments = 16) {
            DrawingApiDefaults<BufferedGlDrawingApi>.Disc(this, center, normal, radius, segments);
        }

        public void Arc(Vector3 center, Vector3 normal, Vector3 from, float radius, float angle, int segments = 16) {
            DrawingApiDefaults<BufferedGlDrawingApi>.Arc(this, center, normal, from, radius, angle, segments);
        }

        public void Dispose() {
            Flush(ImmediateGlDrawingApi.Material);
        }

    }
}