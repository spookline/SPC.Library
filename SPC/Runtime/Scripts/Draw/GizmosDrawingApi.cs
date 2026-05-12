using System;
using UnityEngine;

namespace Spookline.SPC.Draw {
    public readonly struct GizmosDrawingApi : IDrawingAPI {

        public void Ray(Vector3 origin, Vector3 direction) {
#if UNITY_EDITOR
            Gizmos.DrawRay(origin, direction);
#endif
        }

        public void WireCube(Vector3 center, Vector3 size) {
#if UNITY_EDITOR
            Gizmos.DrawWireCube(center, size);
#endif
        }

        public void WireSphere(Vector3 center, float radius, int segments = 32) {
#if UNITY_EDITOR
            Gizmos.DrawWireSphere(center, radius);
#endif
        }

        public void Disc(Vector3 center, Vector3 normal, float radius, int segments = 16) {
            WireDisc(center, normal, radius, segments);
        }
        public void WireDisc(Vector3 center, Vector3 normal, float radius, int segments = 16) {
            DrawingApiDefaults<GizmosDrawingApi>.WireDisc(this, center, normal, radius, segments);
        }
        public void Arc(Vector3 center, Vector3 normal, Vector3 from, float radius, float angle, int segments = 16) {
            WireArc(center, normal, from, radius, angle, segments);
        }
        public void WireArc(Vector3 center, Vector3 normal, Vector3 from, float radius, float angle, int segments = 16) {
            DrawingApiDefaults<GizmosDrawingApi>.WireArc(this, center, normal, from, radius, angle, segments);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
            DrawingApiDefaults<GizmosDrawingApi>.Quad(this, a, b, c, d);
        }

        public void Cube(Vector3 center, Vector3 size) {
#if UNITY_EDITOR
            Gizmos.DrawCube(center, size);
#endif
        }

        public void Sphere(Vector3 center, float radius, int segments = 32) {
#if UNITY_EDITOR
            Gizmos.DrawSphere(center, radius);
#endif
        }

#if UNITY_EDITOR
        public Matrix4x4 Matrix {
            get => Gizmos.matrix;
            set => Gizmos.matrix = value;
        }

        public Color Color {
            get => Gizmos.color;
            set => Gizmos.color = value;
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

        public void Line(Vector3 a, Vector3 b) {
#if UNITY_EDITOR
            Gizmos.DrawLine(a, b);
#endif
        }

        public void Lines(ReadOnlySpan<Vector3> points) {
#if UNITY_EDITOR
            Gizmos.DrawLineList(points);
#endif
        }

        public void Strip(ReadOnlySpan<Vector3> points, bool closed = false) {
#if UNITY_EDITOR
            Gizmos.DrawLineStrip(points, closed);
#endif
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c) {
            DrawingApiDefaults<GizmosDrawingApi>.Triangle(this, a, b, c);
        }
    }
}