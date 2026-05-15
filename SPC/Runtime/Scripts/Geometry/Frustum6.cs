using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Spookline.SPC.Draw;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;
using Plane = Unity.Mathematics.Geometry.Plane;

namespace Spookline.SPC.Geometry {
    [Serializable]
    [BurstCompile]
    [StructLayout(LayoutKind.Sequential)]
    public struct Frustum6 {

        public Plane left;
        public Plane right;
        public Plane bottom;
        public Plane top;
        public Plane near;
        public Plane far;

        public Frustum6(
            Plane left,
            Plane right,
            Plane bottom,
            Plane top,
            Plane near,
            Plane far
        ) {
            this.left = left;
            this.right = right;
            this.bottom = bottom;
            this.top = top;
            this.near = near;
            this.far = far;
        }

        [BurstCompile]
        public bool Intersects(float3 point, float epsilon = math.EPSILON) {
            return left.SignedDistanceToPoint(point) >= -epsilon
                   && right.SignedDistanceToPoint(point) >= -epsilon
                   && bottom.SignedDistanceToPoint(point) >= -epsilon
                   && top.SignedDistanceToPoint(point) >= -epsilon
                   && near.SignedDistanceToPoint(point) >= -epsilon
                   && far.SignedDistanceToPoint(point) >= -epsilon;
        }

        [BurstCompile]
        public bool Contains(float3 point, float epsilon = math.EPSILON) {
            return Intersects(point, epsilon);
        }

        [BurstCompile]
        public bool Intersects(MinMaxAABB aabb, float epsilon = math.EPSILON) {
            return IntersectsPlane(left, aabb, epsilon)
                   && IntersectsPlane(right, aabb, epsilon)
                   && IntersectsPlane(bottom, aabb, epsilon)
                   && IntersectsPlane(top, aabb, epsilon)
                   && IntersectsPlane(near, aabb, epsilon)
                   && IntersectsPlane(far, aabb, epsilon);
        }

        [BurstCompile]
        public bool Contains(MinMaxAABB aabb, float epsilon = math.EPSILON) {
            return ContainsPlane(left, aabb, epsilon)
                   && ContainsPlane(right, aabb, epsilon)
                   && ContainsPlane(bottom, aabb, epsilon)
                   && ContainsPlane(top, aabb, epsilon)
                   && ContainsPlane(near, aabb, epsilon)
                   && ContainsPlane(far, aabb, epsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Plane GetPlane(int index) {
            return index switch {
                0 => left,
                1 => right,
                2 => bottom,
                3 => top,
                4 => near,
                5 => far,
                _ => left
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IntersectsPlane(Plane plane, MinMaxAABB aabb, float epsilon) {
            var n = plane.Normal;
            var positive = math.select(aabb.Min, aabb.Max, n >= 0f);
            return plane.SignedDistanceToPoint(positive) >= -epsilon;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ContainsPlane(Plane plane, MinMaxAABB aabb, float epsilon) {
            var n = plane.Normal;
            var negative = math.select(aabb.Max, aabb.Min, n >= 0f);
            return plane.SignedDistanceToPoint(negative) >= -epsilon;
        }

    }

    public static class FrustumHelper {

        private static readonly UnityEngine.Plane[] _sharedUnityPlanes = new UnityEngine.Plane[6];

        public static Frustum6 ToFrustum6(this UnityEngine.Plane[] planes) {
            if (planes == null)
                throw new ArgumentNullException(nameof(planes));

            if (planes.Length != 6)
                throw new ArgumentException("Frustum plane array must contain exactly 6 planes.", nameof(planes));

            return new Frustum6(
                planes[0].ToMathematicsPlane(),
                planes[1].ToMathematicsPlane(),
                planes[2].ToMathematicsPlane(),
                planes[3].ToMathematicsPlane(),
                planes[4].ToMathematicsPlane(),
                planes[5].ToMathematicsPlane()
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToFrustum6(this UnityEngine.Plane[] planes, ref Frustum6 frustum) {
            frustum.left = planes[0].ToMathematicsPlane();
            frustum.right = planes[1].ToMathematicsPlane();
            frustum.bottom = planes[2].ToMathematicsPlane();
            frustum.top = planes[3].ToMathematicsPlane();
            frustum.near = planes[4].ToMathematicsPlane();
            frustum.far = planes[5].ToMathematicsPlane();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Plane ToMathematicsPlane(this UnityEngine.Plane plane) {
            var n = math.normalizesafe(plane.normal, new float3(0, 1f, 0));
            return new Plane(new float3(n.x, n.y, n.z), plane.distance);
        }

        public static Frustum6 CalculateFrustum6(this Camera camera) {
            GeometryUtility.CalculateFrustumPlanes(camera, _sharedUnityPlanes);
            return _sharedUnityPlanes.ToFrustum6();
        }


        public static void CalculateFrustum6(this Camera camera, ref Frustum6 frustum) {
            GeometryUtility.CalculateFrustumPlanes(camera, _sharedUnityPlanes);
            _sharedUnityPlanes.ToFrustum6(ref frustum);
        }

        public static float4x4 CalculateWorldToProjectionMatrix(this Camera camera) {
            return camera.projectionMatrix * camera.worldToCameraMatrix;
        }

        public static Frustum6 CalculateFrustum6(float4x4 worldToProjectionMatrix) {
            GeometryUtility.CalculateFrustumPlanes(worldToProjectionMatrix, _sharedUnityPlanes);
            return _sharedUnityPlanes.ToFrustum6();
        }

        public static float4x4 CalculateWorldToProjectionMatrix(
            float3 position,
            float3 forward,
            float3 up,
            float verticalFovDegrees,
            float aspect,
            float near,
            float far
        ) {
            if (math.lengthsq(forward) <= float.Epsilon)
                throw new ArgumentException("Forward vector must be non-zero.", nameof(forward));

            if (math.lengthsq(up) <= float.Epsilon)
                throw new ArgumentException("Up vector must be non-zero.", nameof(up));

            if (aspect <= 0f)
                throw new ArgumentOutOfRangeException(nameof(aspect), "Aspect must be greater than zero.");

            if (near <= 0f)
                throw new ArgumentOutOfRangeException(nameof(near), "Near plane must be greater than zero.");

            if (far <= near)
                throw new ArgumentOutOfRangeException(nameof(far), "Far plane must be greater than near plane.");

            var f = math.normalize(forward);
            var u = math.normalize(up);

            var rotation = quaternion.LookRotation(f, u);

            var worldMatrix = float4x4.TRS(
                position,
                rotation,
                new float3(1f, 1f, 1f)
            );

            var worldToView = math.inverse(worldMatrix);

            var unityCameraSpaceFix = float4x4.Scale(new float3(1f, 1f, -1f));

            worldToView = math.mul(unityCameraSpaceFix, worldToView);

            var verticalFovRadians = math.radians(verticalFovDegrees);

            var projection = float4x4.PerspectiveFov(
                verticalFovRadians,
                aspect,
                near,
                far
            );

            var worldToClip = math.mul(projection, worldToView);

            return worldToClip;
        }

        public static float RemapPerceptionScreenCoverage(float screenCoverage) {
            return math.pow(screenCoverage, 0.25f);
        }

        public static float InverseRemapPerceptionScreenCoverage(float screenCoverage) {
            return math.pow(screenCoverage, 4f);
        }

        private static void ProjectCorner(
            float3 p,
            float4x4 m,
            ref float2 rectMin,
            ref float2 rectMax
        ) {
            var clip = math.mul(m, new float4(p, 1f));

            // Clamp w to avoid NaN/Inf and to make near-plane intersections conservative.
            var invW = math.rcp(math.max(clip.w, 1e-4f));
            var ndc = clip.xy * invW;

            rectMin = math.min(rectMin, ndc);
            rectMax = math.max(rectMax, ndc);
        }


        public static void DrawGizmo(this Frustum6 frustum) {
            Vector3 nearBottomLeft = Intersect(frustum.near, frustum.bottom, frustum.left);
            Vector3 nearBottomRight = Intersect(frustum.near, frustum.bottom, frustum.right);
            Vector3 nearTopLeft = Intersect(frustum.near, frustum.top, frustum.left);
            Vector3 nearTopRight = Intersect(frustum.near, frustum.top, frustum.right);

            Vector3 farBottomLeft = Intersect(frustum.far, frustum.bottom, frustum.left);
            Vector3 farBottomRight = Intersect(frustum.far, frustum.bottom, frustum.right);
            Vector3 farTopLeft = Intersect(frustum.far, frustum.top, frustum.left);
            Vector3 farTopRight = Intersect(frustum.far, frustum.top, frustum.right);

            DrawQuad(nearBottomLeft, nearBottomRight, nearTopRight, nearTopLeft);
            DrawQuad(farBottomLeft, farBottomRight, farTopRight, farTopLeft);

            Gizmos.DrawLine(nearBottomLeft, farBottomLeft);
            Gizmos.DrawLine(nearBottomRight, farBottomRight);
            Gizmos.DrawLine(nearTopLeft, farTopLeft);
            Gizmos.DrawLine(nearTopRight, farTopRight);
        }

        public static void Frustum<T>(this T api, Frustum6 frustum) where T : IDrawingAPI {
            Vector3 nearBottomLeft = Intersect(frustum.near, frustum.bottom, frustum.left);
            Vector3 nearBottomRight = Intersect(frustum.near, frustum.bottom, frustum.right);
            Vector3 nearTopLeft = Intersect(frustum.near, frustum.top, frustum.left);
            Vector3 nearTopRight = Intersect(frustum.near, frustum.top, frustum.right);

            Vector3 farBottomLeft = Intersect(frustum.far, frustum.bottom, frustum.left);
            Vector3 farBottomRight = Intersect(frustum.far, frustum.bottom, frustum.right);
            Vector3 farTopLeft = Intersect(frustum.far, frustum.top, frustum.left);
            Vector3 farTopRight = Intersect(frustum.far, frustum.top, frustum.right);

            api.Quad(nearBottomLeft, nearBottomRight, nearTopRight, nearTopLeft);
            api.Quad(farBottomLeft, farBottomRight, farTopRight, farTopLeft);
            api.Line(nearBottomLeft, farBottomLeft);
            api.Line(nearBottomRight, farBottomRight);
            api.Line(nearTopLeft, farTopLeft);
            api.Line(nearTopRight, farTopRight);
        }


        private static void DrawQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }

        private static float3 Intersect(Plane a, Plane b, Plane c) {
            var n1 = a.Normal;
            var n2 = b.Normal;
            var n3 = c.Normal;

            var d1 = a.Distance;
            var d2 = b.Distance;
            var d3 = c.Distance;

            var n2n3 = math.cross(n2, n3);
            var n3n1 = math.cross(n3, n1);
            var n1n2 = math.cross(n1, n2);

            var denominator = math.dot(n1, n2n3);

            return -(d1 * n2n3 + d2 * n3n1 + d3 * n1n2) / denominator;
        }

    }


    [BurstCompile(FloatMode = FloatMode.Fast)]
    public static class FastFrustumHelpers {

        [BurstCompile(FloatMode = FloatMode.Fast)]
        public struct BatchedCoverageJob : IJobParallelFor {

            public NativeArray<MinMaxAABB> aabb;
            public NativeArray<float> coverage;
            public float4x4 matrix;

            public void Execute(int index) {
                CalculateScreenCoverage(aabb[index], matrix, out var result);
                coverage[index] = result;
            }

        }

        public static JobHandle CalculateScreenCoverageBatched(
            NativeArray<MinMaxAABB> aabbs,
            float4x4 worldToProjectionMatrix,
            NativeArray<float> results,
            JobHandle dependency = default
        ) {
            var job = new BatchedCoverageJob {
                aabb = aabbs,
                coverage = results,
                matrix = worldToProjectionMatrix
            };

            return job.Schedule(aabbs.Length, 64, dependency);
        }


        [BurstCompile]
        public static void CalculateScreenCoverage(
            in MinMaxAABB aabb,
            in float4x4 worldToProjectionMatrix,
            out float result
        ) {
            CalculateClipRectNdc(aabb, worldToProjectionMatrix, out var ndcRect);
            var minNdc = math.max(ndcRect.xy, new float2(-1f));
            var maxNdc = math.min(ndcRect.zw, new float2(1f));
            var size = math.max(maxNdc - minNdc, float2.zero);
            result = math.saturate((size.x * size.y) * 0.25f);
        }

        public static void CalculateScreenCoverage(
            in float4 ndcRect,
            out float result
        ) {
            var minNdc = math.max(ndcRect.xy, new float2(-1f));
            var maxNdc = math.min(ndcRect.zw, new float2(1f));
            var size = math.max(maxNdc - minNdc, float2.zero);
            result = math.saturate(size.x * size.y * 0.25f);
        }

        [BurstCompile]
        public static void CalculateClipRectNdc(
            in MinMaxAABB aabb,
            in float4x4 worldToProjectionMatrix,
            out float4 clipRect
        ) {
            var min = aabb.Min;
            var max = aabb.Max;

            // Finite sentinels, not +/-Infinity.
            var rectMin = new float2(1e20f);
            var rectMax = new float2(-1e20f);

            ProjectCorner(new float3(min.x, min.y, min.z), worldToProjectionMatrix, ref rectMin, ref rectMax);
            ProjectCorner(new float3(max.x, min.y, min.z), worldToProjectionMatrix, ref rectMin, ref rectMax);
            ProjectCorner(new float3(min.x, max.y, min.z), worldToProjectionMatrix, ref rectMin, ref rectMax);
            ProjectCorner(new float3(max.x, max.y, min.z), worldToProjectionMatrix, ref rectMin, ref rectMax);

            ProjectCorner(new float3(min.x, min.y, max.z), worldToProjectionMatrix, ref rectMin, ref rectMax);
            ProjectCorner(new float3(max.x, min.y, max.z), worldToProjectionMatrix, ref rectMin, ref rectMax);
            ProjectCorner(new float3(min.x, max.y, max.z), worldToProjectionMatrix, ref rectMin, ref rectMax);
            ProjectCorner(new float3(max.x, max.y, max.z), worldToProjectionMatrix, ref rectMin, ref rectMax);

            clipRect = new float4(rectMin.x, rectMin.y, rectMax.x, rectMax.y);
        }

        private static void ProjectCorner(
            float3 p,
            float4x4 m,
            ref float2 rectMin,
            ref float2 rectMax
        ) {
            var clip = math.mul(m, new float4(p, 1f));

            // Clamp w to avoid NaN/Inf and to make near-plane intersections conservative.
            var invW = math.rcp(math.max(clip.w, 1e-4f));
            var ndc = clip.xy * invW;

            rectMin = math.min(rectMin, ndc);
            rectMax = math.max(rectMax, ndc);
        }

    }
}