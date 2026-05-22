using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Spookline.SPC.Draw;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;

namespace Spookline.SPC.Geometry {
    [Serializable]
    [BurstCompile]
    [StructLayout(LayoutKind.Sequential)]
    public struct OrientedBox {

        public static readonly OrientedBox identity = new(
            float3.zero,
            new float3(1f, 1f, 1f),
            quaternion.identity
        );

        public static readonly OrientedBox zero = new(
            float3.zero,
            float3.zero,
            quaternion.identity
        );

        public float3 center;
        public float3 halfExtent;
        public quaternion rotation;

        public float3 Size => halfExtent * 2f;
        public float3 Extents => Size;
        public float3 Min => center - halfExtent;
        public float3 Max => center + halfExtent;

        // Bottom center along the box's own local -Y axis.
        public float3 LocalGroundCenter => center - math.mul(rotation, new float3(0f, halfExtent.y, 0f));

        // Legacy-style ground center along world -Y.
        // Keep this only if your game logic intentionally defines "ground" as world-Y.
        public float3 WorldGroundCenter => center - new float3(0f, halfExtent.y, 0f);

        public OrientedBox(float3 center, float3 size, quaternion rotation) {
            this.center = center;
            this.halfExtent = size * 0.5f;
            this.rotation = OrientedBoxMath.NormalizeSafe(rotation);
        }

        public static implicit operator Bounds(OrientedBox box) => box.Bounds();
        public static implicit operator MinMaxAABB(OrientedBox box) => box.AABB();

        public static implicit operator OrientedBox(Bounds bounds) =>
            new(bounds.center, bounds.size, quaternion.identity);

        public static implicit operator OrientedBox(MinMaxAABB bounds) =>
            new(bounds.Center, bounds.Extents, quaternion.identity);

        public OrientedBox WithCenter(float3 newCenter) {
            return new OrientedBox(newCenter, Size, rotation);
        }

        public OrientedBox WithSize(float3 size) {
            return new OrientedBox(center, size, rotation);
        }

        public OrientedBox WithRotation(quaternion newRotation) {
            return new OrientedBox(center, Size, newRotation);
        }

        public OrientedBox WithLocalGroundCenter(float3 groundCenter) {
            var newCenter = groundCenter + math.mul(rotation, new float3(0f, halfExtent.y, 0f));
            return new OrientedBox(newCenter, Size, rotation);
        }

        public OrientedBox WithWorldGroundCenter(float3 groundCenter) {
            var newCenter = groundCenter + new float3(0f, halfExtent.y, 0f);
            return new OrientedBox(newCenter, Size, rotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(OrientedBox other) {
            return OrientedBoxMath.Intersects(this, other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(OrientedBox other) {
            return OrientedBoxMath.Contains(this, other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrientedBox Encapsulate(OrientedBox other) {
            return OrientedBoxMath.Encapsulate(this, other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrientedBox Encapsulate(float3 sphereCenter, float radius) {
            return OrientedBoxMath.Encapsulate(this, sphereCenter, radius);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrientedBox Encapsulate(float3 point) {
            return OrientedBoxMath.Encapsulate(this, point);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrientedBox EncapsulateFixedCenter(OrientedBox other) {
            return OrientedBoxMath.EncapsulateFixedCenter(this, other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrientedBox Grow(float3 amount) {
            return new OrientedBox(center, Size + amount, rotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrientedBox EncapsulatedAxisAligned(OrientedBox other) {
            return OrientedBoxMath.EncapsulateAxisAligned(this, other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MinMaxAABB AABB() {
            return rotation.Equals(quaternion.identity)
                ? MinMaxAABB.CreateFromCenterAndHalfExtents(center, halfExtent)
                : OrientedBoxMath.AABB(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bounds Bounds() {
            return rotation.Equals(quaternion.identity) ? new Bounds(center, Size) : OrientedBoxMath.Bounds(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 TransformPoint(float3 localPoint) {
            return center + math.mul(rotation, localPoint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 InverseTransformPoint(float3 worldPoint) {
            return math.mul(math.inverse(rotation), worldPoint - center);
        }

        public override string ToString() {
            return $"{nameof(center)}: {center}, {nameof(rotation)}: {rotation}, {nameof(Size)}: {Size}";
        }

        public static OrientedBox FromLocalGroundAlignedBox(float3 groundCenter, float3 size, quaternion rotation) {
            rotation = OrientedBoxMath.NormalizeSafe(rotation);
            var center = groundCenter + math.mul(rotation, new float3(0f, size.y * 0.5f, 0f));
            return new OrientedBox(center, size, rotation);
        }

        public static OrientedBox FromWorldGroundAlignedBox(float3 groundCenter, float3 size, quaternion rotation) {
            var center = groundCenter + new float3(0f, size.y * 0.5f, 0f);
            return new OrientedBox(center, size, rotation);
        }

    }

    [BurstCompile]
    public static class OrientedBoxMath {

        private const float _axisEpsilon = 1e-12f;
        private const float _containmentEpsilon = 1e-5f;
        private const float _pointContainmentEpsilon = 1e-5f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion NormalizeSafe(quaternion q) {
            var lenSq = math.lengthsq(q.value);
            return lenSq > 1e-20f ? new quaternion(q.value * math.rsqrt(lenSq)) : quaternion.identity;
        }

        public static bool Intersects(OrientedBox a, OrientedBox b) {
            GetAxes(a.rotation, out var a0, out var a1, out var a2);
            GetAxes(b.rotation, out var b0, out var b1, out var b2);

            var d = b.center - a.center;

            // Face normals of A.
            if (!OverlapOnAxis(a0, d, a.halfExtent, b.halfExtent, a0, a1, a2, b0, b1, b2)) return false;
            if (!OverlapOnAxis(a1, d, a.halfExtent, b.halfExtent, a0, a1, a2, b0, b1, b2)) return false;
            if (!OverlapOnAxis(a2, d, a.halfExtent, b.halfExtent, a0, a1, a2, b0, b1, b2)) return false;

            // Face normals of B.
            if (!OverlapOnAxis(b0, d, a.halfExtent, b.halfExtent, a0, a1, a2, b0, b1, b2)) return false;
            if (!OverlapOnAxis(b1, d, a.halfExtent, b.halfExtent, a0, a1, a2, b0, b1, b2)) return false;
            if (!OverlapOnAxis(b2, d, a.halfExtent, b.halfExtent, a0, a1, a2, b0, b1, b2)) return false;

            // Cross products of edge directions.
            if (!OverlapOnAxis(math.cross(a0, b0), d, a.halfExtent, b.halfExtent, a0, a1, a2, b0, b1, b2)) return false;
            if (!OverlapOnAxis(math.cross(a0, b1), d, a.halfExtent, b.halfExtent, a0, a1, a2, b0, b1, b2)) return false;
            if (!OverlapOnAxis(math.cross(a0, b2), d, a.halfExtent, b.halfExtent, a0, a1, a2, b0, b1, b2)) return false;

            if (!OverlapOnAxis(math.cross(a1, b0), d, a.halfExtent, b.halfExtent, a0, a1, a2, b0, b1, b2)) return false;
            if (!OverlapOnAxis(math.cross(a1, b1), d, a.halfExtent, b.halfExtent, a0, a1, a2, b0, b1, b2)) return false;
            if (!OverlapOnAxis(math.cross(a1, b2), d, a.halfExtent, b.halfExtent, a0, a1, a2, b0, b1, b2)) return false;

            if (!OverlapOnAxis(math.cross(a2, b0), d, a.halfExtent, b.halfExtent, a0, a1, a2, b0, b1, b2)) return false;
            if (!OverlapOnAxis(math.cross(a2, b1), d, a.halfExtent, b.halfExtent, a0, a1, a2, b0, b1, b2)) return false;
            if (!OverlapOnAxis(math.cross(a2, b2), d, a.halfExtent, b.halfExtent, a0, a1, a2, b0, b1, b2)) return false;

            return true;
        }

        public static bool Contains(OrientedBox outer, OrientedBox inner) {
            for (var i = 0; i < 8; i++) {
                var localCorner = GetCornerLocal(inner.halfExtent, i);
                var worldCorner = inner.TransformPoint(localCorner);
                var outerLocal = outer.InverseTransformPoint(worldCorner);

                var outside = math.abs(outerLocal) - outer.halfExtent;
                if (math.any(outside > _containmentEpsilon))
                    return false;
            }

            return true;
        }

        // Produces a box with `a.rotation` that contains both `a` and `b`.
        // This is not the globally minimal OBB over all possible rotations.
        // It is the minimal box for the fixed orientation `a.rotation`.
        public static OrientedBox Encapsulate(OrientedBox a, OrientedBox b) {
            var min = -a.halfExtent;
            var max = a.halfExtent;

            for (var i = 0; i < 8; i++) {
                var bLocalCorner = GetCornerLocal(b.halfExtent, i);
                var bWorldCorner = b.TransformPoint(bLocalCorner);
                var pointInALocal = a.InverseTransformPoint(bWorldCorner);

                min = math.min(min, pointInALocal);
                max = math.max(max, pointInALocal);
            }

            var localCenter = (min + max) * 0.5f;
            var halfExtent = (max - min) * 0.5f;
            var worldCenter = a.TransformPoint(localCenter);

            return new OrientedBox(worldCenter, halfExtent * 2f, a.rotation);
        }

        public static OrientedBox EncapsulateFixedCenter(OrientedBox a, OrientedBox b) {
            var maxAbs = a.halfExtent;

            for (var i = 0; i < 8; i++) {
                var bLocalCorner = GetCornerLocal(b.halfExtent, i);
                var bWorldCorner = b.TransformPoint(bLocalCorner);
                var pointInALocal = a.InverseTransformPoint(bWorldCorner);

                maxAbs = math.max(maxAbs, math.abs(pointInALocal));
            }

            return new OrientedBox(a.center, maxAbs * 2f, a.rotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrientedBox Encapsulate(OrientedBox box, float3 sphereCenter, float radius) {
            var localCenter = box.InverseTransformPoint(sphereCenter);

            var min = math.min(-box.halfExtent, localCenter - radius);
            var max = math.max(box.halfExtent, localCenter + radius);

            var newLocalCenter = (min + max) * 0.5f;
            var newHalfExtent = (max - min) * 0.5f;
            var newWorldCenter = box.TransformPoint(newLocalCenter);

            return new OrientedBox(newWorldCenter, newHalfExtent * 2f, box.rotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrientedBox Encapsulate(OrientedBox box, float3 point) {
            var localPoint = box.InverseTransformPoint(point);

            var min = math.min(-box.halfExtent, localPoint);
            var max = math.max(box.halfExtent, localPoint);

            var newLocalCenter = (min + max) * 0.5f;
            var newHalfExtent = (max - min) * 0.5f;
            var newWorldCenter = box.TransformPoint(newLocalCenter);

            return new OrientedBox(newWorldCenter, newHalfExtent * 2f, box.rotation);
        }

        // Produces the smallest world-axis-aligned box containing both boxes.
        // Result rotation is identity.
        public static OrientedBox EncapsulateAxisAligned(OrientedBox a, OrientedBox b) {
            var min = new float3(float.PositiveInfinity);
            var max = new float3(float.NegativeInfinity);

            for (var i = 0; i < 8; i++) {
                var p = GetCornerWorld(a, i);
                min = math.min(min, p);
                max = math.max(max, p);
            }

            for (var i = 0; i < 8; i++) {
                var p = GetCornerWorld(b, i);
                min = math.min(min, p);
                max = math.max(max, p);
            }

            var center = (min + max) * 0.5f;
            var size = max - min;

            return new OrientedBox(center, size, quaternion.identity);
        }

        public static Bounds Bounds(OrientedBox box) {
            var min = new float3(float.PositiveInfinity);
            var max = new float3(float.NegativeInfinity);

            for (var i = 0; i < 8; i++) {
                var p = GetCornerWorld(box, i);
                min = math.min(min, p);
                max = math.max(max, p);
            }

            var center = (min + max) * 0.5f;
            var size = max - min;
            return new Bounds(center, size);
        }

        public static MinMaxAABB AABB(OrientedBox box) {
            var min = new float3(float.PositiveInfinity);
            var max = new float3(float.NegativeInfinity);

            for (var i = 0; i < 8; i++) {
                var p = GetCornerWorld(box, i);
                min = math.min(min, p);
                max = math.max(max, p);
            }

            return new MinMaxAABB(min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetCornerWorld(OrientedBox box, int index) {
            return box.TransformPoint(GetCornerLocal(box.halfExtent, index));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GetAxes(
            quaternion rotation,
            out float3 x,
            out float3 y,
            out float3 z
        ) {
            x = math.mul(rotation, new float3(1f, 0f, 0f));
            y = math.mul(rotation, new float3(0f, 1f, 0f));
            z = math.mul(rotation, new float3(0f, 0f, 1f));
        }

        private static bool OverlapOnAxis(
            float3 axis,
            float3 centerDelta,
            float3 aHalf,
            float3 bHalf,
            float3 a0,
            float3 a1,
            float3 a2,
            float3 b0,
            float3 b1,
            float3 b2
        ) {
            // Do not normalize the axis. SAT works with any non-zero axis
            // as long as all projections use the same axis.
            if (math.lengthsq(axis) < _axisEpsilon)
                return true;

            var projectionA =
                math.abs(math.dot(axis, a0)) * aHalf.x +
                math.abs(math.dot(axis, a1)) * aHalf.y +
                math.abs(math.dot(axis, a2)) * aHalf.z;

            var projectionB =
                math.abs(math.dot(axis, b0)) * bHalf.x +
                math.abs(math.dot(axis, b1)) * bHalf.y +
                math.abs(math.dot(axis, b2)) * bHalf.z;

            var distance = math.abs(math.dot(centerDelta, axis));

            return distance <= projectionA + projectionB + _containmentEpsilon;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float3 GetCornerLocal(float3 halfExtent, int index) {
            return new float3(
                (index & 1) == 0 ? -halfExtent.x : halfExtent.x,
                (index & 2) == 0 ? -halfExtent.y : halfExtent.y,
                (index & 4) == 0 ? -halfExtent.z : halfExtent.z
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsPoint(this OrientedBox box, float3 point) {
            var localPoint = box.InverseTransformPoint(point);
            var delta = math.abs(localPoint) - box.halfExtent;

            return math.all(delta <= _pointContainmentEpsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsSphere(this OrientedBox box, float3 sphereCenter, float radius) {
            radius = math.max(radius, 0f);

            var localCenter = box.InverseTransformPoint(sphereCenter);

            // A sphere is fully inside the box if it is at least `radius`
            // away from every face of the box.
            var requiredExtent = math.abs(localCenter) + radius;

            return math.all(requiredExtent <= box.halfExtent + _pointContainmentEpsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool OverlapsSphere(this OrientedBox box, float3 sphereCenter, float radius) {
            radius = math.max(radius, 0f);

            var localCenter = box.InverseTransformPoint(sphereCenter);

            // Closest point on the local AABB to the sphere center.
            var closestLocal = math.clamp(localCenter, -box.halfExtent, box.halfExtent);

            var delta = localCenter - closestLocal;
            var distanceSq = math.lengthsq(delta);

            return distanceSq <= radius * radius + _pointContainmentEpsilon;
        }

        public static float3 GetCorner(this OrientedBox box, int index) {
            return box.TransformPoint(GetCornerLocal(box.halfExtent, index));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ClosestPoint(this OrientedBox box, float3 point) {
            var localPoint = box.InverseTransformPoint(point);
            var closestLocal = math.clamp(localPoint, -box.halfExtent, box.halfExtent);

            return box.TransformPoint(closestLocal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSqToPoint(this OrientedBox box, float3 point) {
            var localPoint = box.InverseTransformPoint(point);
            var closestLocal = math.clamp(localPoint, -box.halfExtent, box.halfExtent);
            var delta = localPoint - closestLocal;

            return math.lengthsq(delta);
        }

    }

    public static class OrientedBoxExtensions {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TRS ToVirtualTransform(this OrientedBox box) {
            return new TRS(box.center, box.rotation, box.Size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrientedBox ToOrientedBox(this TRS transform) {
            return new OrientedBox(transform.position, transform.scale, transform.rotation);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform Affine(this OrientedBox box) {
            return new AffineTransform(box.center, box.rotation, box.Size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrientedBox Transform(
            this AffineTransform transform,
            OrientedBox box
        ) {
            var affine = new AffineTransform(box.center, box.rotation, box.Size);
            affine = math.mul(transform, affine);
            math.decompose(affine, out var pos, out var rot, out var scale);
            return new OrientedBox(pos, scale, rot);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrientedBox InverseTransform(
            this AffineTransform transform,
            OrientedBox box
        ) {
            var inverse = math.inverse(transform);
            return inverse.Transform(box);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrientedBox ToOrientedBox(this Transform transform) {
            return new OrientedBox(transform.position, transform.lossyScale, transform.rotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TRS ToGroundAlignedVirtualTransform(this OrientedBox box) {
            return new TRS(box.LocalGroundCenter, box.rotation);
        }

        public static void DrawGizmos(this OrientedBox box, bool wireframe = true) {
            var previousMatrix = Gizmos.matrix;

            Gizmos.matrix = Matrix4x4.TRS(
                box.center,
                new Quaternion(box.rotation.value.x, box.rotation.value.y, box.rotation.value.z, box.rotation.value.w),
                Vector3.one
            );

            if (wireframe)
                Gizmos.DrawWireCube(Vector3.zero, box.Size);
            else
                Gizmos.DrawCube(Vector3.zero, box.Size);

            Gizmos.matrix = previousMatrix;
        }

        public static void OrientedBox<T>(this T api, OrientedBox box, bool wireframe = true) where T : IDrawingAPI {
            using (api.ScopeTransformation(
                       Matrix4x4.TRS(
                           box.center,
                           new Quaternion(
                               box.rotation.value.x,
                               box.rotation.value.y,
                               box.rotation.value.z,
                               box.rotation.value.w
                           ),
                           Vector3.one
                       )
                   )) {
                if (wireframe) api.WireCube(Vector3.zero, box.Size);
                else api.Cube(Vector3.zero, box.Size);
            }
        }

    }
}