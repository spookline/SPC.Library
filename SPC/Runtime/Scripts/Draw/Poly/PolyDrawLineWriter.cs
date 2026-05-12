using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Draw {
    public struct PolyDrawLineWriter {

        private NativeList<PolyDrawVertex> _vertices;

        public int VertexCount => _vertices.Length;

        public PolyDrawLineWriter(NativeList<PolyDrawVertex> vertices) {
            this._vertices = vertices;
        }

        public void Clear() {
            _vertices.Clear();
        }

        public void Line(float3 a, float3 b, float4 color) {
            _vertices.Add(new PolyDrawVertex(a, color, float3.zero, PolyDrawShaderFlags.DoubleSided));
            _vertices.Add(new PolyDrawVertex(b, color, float3.zero, PolyDrawShaderFlags.DoubleSided));
        }

        public void Line(float4x4 matrix, float3 a, float3 b, float4 color) {
            Line(
                math.transform(matrix, a),
                math.transform(matrix, b),
                color
            );
        }

        public void Lines(ReadOnlySpan<float3> points, float4 color) {
            if (points == null)
                return;

            var count = points.Length - (points.Length % 2);

            for (var i = 0; i < count; i += 2)
                Line(points[i], points[i + 1], color);
        }

        public void Lines(ReadOnlySpan<Vector3> points, float4 color) {
            if (points == null)
                return;

            var count = points.Length - (points.Length % 2);

            for (var i = 0; i < count; i += 2)
                Line(points[i], points[i + 1], color);
        }

        public void Lines(float4x4 matrix, ReadOnlySpan<float3> points, float4 color) {
            if (points == null)
                return;

            var count = points.Length - (points.Length % 2);

            for (var i = 0; i < count; i += 2)
                Line(matrix, points[i], points[i + 1], color);
        }

        public void Lines(float4x4 matrix, ReadOnlySpan<Vector3> points, float4 color) {
            if (points == null)
                return;

            var count = points.Length - (points.Length % 2);

            for (var i = 0; i < count; i += 2)
                Line(matrix, points[i], points[i + 1], color);
        }


        public void Strip(ReadOnlySpan<float3> points, float4 color, bool closed = false) {
            if (points == null || points.Length < 2)
                return;

            for (var i = 0; i < points.Length - 1; i++)
                Line(points[i], points[i + 1], color);

            if (closed)
                Line(points[^1], points[0], color);
        }

        public void Strip(float4x4 matrix, ReadOnlySpan<float3> points, float4 color, bool closed = false) {
            if (points == null || points.Length < 2)
                return;

            for (var i = 0; i < points.Length - 1; i++)
                Line(matrix, points[i], points[i + 1], color);

            if (closed)
                Line(matrix, points[^1], points[0], color);
        }

        public void Strip(float4x4 matrix, ReadOnlySpan<Vector3> points, float4 color, bool closed = false) {
            if (points == null || points.Length < 2)
                return;

            for (var i = 0; i < points.Length - 1; i++)
                Line(matrix, points[i], points[i + 1], color);

            if (closed)
                Line(matrix, points[^1], points[0], color);
        }

        public void Strip(ReadOnlySpan<float3> points, Color color, bool closed = false) {
            if (points == null || points.Length < 2)
                return;

            var c = PolyDrawCommandFactory.Color(color);

            for (var i = 0; i < points.Length - 1; i++)
                Line(points[i], points[i + 1], c);

            if (closed)
                Line(points[^1], points[0], c);
        }

        public void Strip(ReadOnlySpan<Vector3> points, Color color, bool closed = false) {
            if (points == null || points.Length < 2)
                return;

            var c = PolyDrawCommandFactory.Color(color);

            for (var i = 0; i < points.Length - 1; i++)
                Line(points[i], points[i + 1], c);

            if (closed)
                Line(points[^1], points[0], c);
        }

        public NativeArray<PolyDrawVertex> AsArray() {
            return _vertices.AsArray();
        }

    }
}