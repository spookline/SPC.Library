using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spookline.SPC.Draw {
    public struct HandlesDrawingApi : IDrawingAPI {

        public void Line(Vector3 a, Vector3 b) {
#if UNITY_EDITOR
            Handles.DrawLine(a, b);
#endif
        }

        public void Lines(ReadOnlySpan<Vector3> points) {
#if UNITY_EDITOR
            if (points == null || points.Length < 2) return;
            for (var i = 0; i < points.Length - 1; i++) Handles.DrawLine(points[i], points[i + 1]);
#endif
        }

        public void Strip(ReadOnlySpan<Vector3> points, bool closed = false) {
#if UNITY_EDITOR
            if (points == null || points.Length < 2) return;
            for (var i = 0; i < points.Length - 1; i++) Handles.DrawLine(points[i], points[i + 1]);
            if (closed) Handles.DrawLine(points[^1], points[0]);
#endif
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c) {
#if UNITY_EDITOR
            Handles.DrawAAConvexPolygon(a, b, c);
#endif
        }

        public void Triangles(ReadOnlySpan<Vector3> points) {
            DrawingApiDefaults<HandlesDrawingApi>.Triangles(this, points);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
#if UNITY_EDITOR
            Handles.DrawAAConvexPolygon(a, b, c, d);
#endif
        }

        public void WireCube(Vector3 center, Vector3 size) {
#if UNITY_EDITOR
            Handles.DrawWireCube(center, size);
#endif
        }

        public void Sphere(Vector3 center, float radius, int segments = 16) => WireSphere(center, radius, segments);

        public void WireSphere(Vector3 center, float radius, int segments = 16) {
#if UNITY_EDITOR
            Handles.DrawWireDisc(center, Vector3.up, radius);
            Handles.DrawWireDisc(center, Vector3.right, radius);
            Handles.DrawWireDisc(center, Vector3.forward, radius);
#endif
        }


        public void Cube(Vector3 center, Vector3 size) {
#if UNITY_EDITOR
            Span<Vector3> corners = stackalloc Vector3[8];
            var halfSize = size / 2;
            corners[0] = center - halfSize;
            corners[1] = center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
            corners[2] = center + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            corners[3] = center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            corners[4] = center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            corners[5] = center + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            corners[6] = center + halfSize;
            corners[7] = center + new Vector3(-halfSize.x, halfSize.y, halfSize.z);

            // Draw faces
            Vector3[] front = {corners[0], corners[1], corners[2], corners[3]};
            Vector3[] back = {corners[4], corners[5], corners[6], corners[7]};
            Vector3[] left = {corners[0], corners[3], corners[7], corners[4]};
            Vector3[] right = {corners[1], corners[2], corners[6], corners[5]};
            Vector3[] top = {corners[0], corners[1], corners[5], corners[4]};
            Vector3[] bottom = {corners[2], corners[3], corners[7], corners[6]};

            Handles.DrawSolidRectangleWithOutline(front, Color, Color.clear);
            Handles.DrawSolidRectangleWithOutline(back, Color, Color.clear);
            Handles.DrawSolidRectangleWithOutline(left, Color, Color.clear);
            Handles.DrawSolidRectangleWithOutline(right, Color, Color.clear);
            Handles.DrawSolidRectangleWithOutline(top, Color, Color.clear);
            Handles.DrawSolidRectangleWithOutline(bottom, Color, Color.clear);
#endif
        }

        public void Disc(Vector3 center, Vector3 normal, float radius, int segments = 16) {
#if UNITY_EDITOR
            Handles.DrawSolidDisc(center, normal, radius);
#endif
        }

        public void WireDisc(Vector3 center, Vector3 normal, float radius, int segments = 16) {
#if UNITY_EDITOR
            Handles.DrawWireDisc(center, normal, radius);
#endif
        }

        public void Arc(Vector3 center, Vector3 normal, Vector3 from, float radius, float angle, int segments = 16) {
#if UNITY_EDITOR
            Handles.DrawSolidArc(center, normal, from, angle, radius);
#endif
        }

        public void WireArc(
            Vector3 center,
            Vector3 normal,
            Vector3 from,
            float radius,
            float angle,
            int segments = 16
        ) {
#if UNITY_EDITOR
            Handles.DrawWireArc(center, normal, from, angle, radius);
#endif
        }

        public void Mesh(Mesh mesh) {
            DrawingApiDefaults<HandlesDrawingApi>.Mesh(this, mesh);
        }
        public void WireMesh(Mesh mesh) {
            DrawingApiDefaults<HandlesDrawingApi>.WireMesh(this, mesh);
        }

        public void LineBuffer(PolyDrawBuffer buffer) {
            DrawingApiDefaults<HandlesDrawingApi>.LineBuffer(this, buffer);
        }

        public void MeshBuffer(PolyDrawBuffer buffer) {
            DrawingApiDefaults<HandlesDrawingApi>.MeshBuffer(this, buffer);
        }


#if UNITY_EDITOR
        public Matrix4x4 Matrix {
            get => Handles.matrix;
            set => Handles.matrix = value;
        }

        public Color Color {
            get => Handles.color;
            set => Handles.color = value;
        }
#else
        public Matrix4x4 Matrix {
            get => Matrix4x4.identity;
            set {}
        }

        public Color Color {
            get => Color.white;
            set {}
        }
#endif

    }
}