using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Cinemachine;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;

namespace Spookline.SPC.Geometry {
    public static class BoundsHelper {

        public static MinMaxAABB ToMinMaxAABB(this Bounds bounds) {
            return new MinMaxAABB(bounds.min, bounds.max);
        }

        public static Bounds ComputeBounds(IReadOnlyList<Renderer> renderers) {
            if (renderers == null || renderers.Count == 0) { return new Bounds(); }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Count; i++) {
                var localBounds = renderers[i].bounds;
                bounds.Encapsulate(localBounds);
            }
            return bounds;
        }

        public static Bounds ComputeBounds(IReadOnlyList<Collider> colliders) {
            if (colliders == null || colliders.Count == 0) { return new Bounds(); }

            var bounds = colliders[0].bounds;
            for (var i = 1; i < colliders.Count; i++) { bounds.Encapsulate(colliders[i].bounds); }

            return bounds;
        }

        public static OrientedBox ToOrientedBox(this Renderer renderer) {
            var bounds = renderer.localBounds;
            var transform = renderer.transform;
            var center = transform.TransformPoint(bounds.center);
            var size = Vector3.Scale(bounds.size, transform.lossyScale.Abs());
            return new OrientedBox(center, size, transform.rotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ClosestPoint(this MinMaxAABB aabb, float3 point) {
            return math.clamp(point, aabb.Min, aabb.Max);
        }
    }
}