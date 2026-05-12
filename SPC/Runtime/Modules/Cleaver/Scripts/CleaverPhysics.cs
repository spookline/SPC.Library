using System;
using Spookline.SPC.Geometry;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Cleaver {
    public static class CleaverPhysics {

        public const int DefaultMaxResults = 512;
        private static readonly Collider[] _overlapCache = new Collider[512];
        private static readonly RaycastHit[] _raycastCache = new RaycastHit[512];

        public static ArraySegment<Collider> SphereSection(
            Vector3 origin,
            Vector3 direction,
            float degrees,
            float distance,
            int layerMask = Physics.AllLayers,
            QueryTriggerInteraction triggers = QueryTriggerInteraction.UseGlobal,
            int maxResults = DefaultMaxResults
        ) {
            var count = SphereSection(
                origin,
                direction,
                degrees,
                distance,
                _overlapCache,
                _overlapCache,
                layerMask,
                triggers,
                maxResults
            );
            return new ArraySegment<Collider>(_overlapCache, 0, count);
        }


        public static int SphereSection(
            Vector3 origin,
            Vector3 direction,
            float degrees,
            float distance,
            Collider[] results,
            int layerMask = Physics.AllLayers,
            QueryTriggerInteraction triggers = QueryTriggerInteraction.UseGlobal,
            int maxResults = DefaultMaxResults
        ) {
            return SphereSection(
                origin,
                direction,
                degrees,
                distance,
                _overlapCache,
                results,
                layerMask,
                triggers,
                maxResults
            );
        }

        public static int SphereSection(
            Vector3 origin,
            Vector3 direction,
            float degrees,
            float distance,
            Collider[] cache,
            Collider[] results,
            int layerMask = Physics.AllLayers,
            QueryTriggerInteraction triggers = QueryTriggerInteraction.UseGlobal,
            int maxResults = -1
        ) {
            maxResults = maxResults < 0 ? results.Length : math.min(maxResults, results.Length);
            var query = SphereSectionQuery.FromDegrees(origin, direction, degrees, distance);
            var hitCount = Physics.OverlapSphereNonAlloc(query.origin, query.radius, cache, layerMask);
            var resultCount = 0;
            for (var i = 0; i < hitCount; i++) {
                if (resultCount >= maxResults) break;
                var collider = cache[i];
                if (query.ContainsPointOverlap(collider.transform.position)) results[resultCount++] = collider;
            }

            return resultCount;
        }

        public static ArraySegment<Collider> Box(
            OrientedBox box,
            int layerMask = Physics.AllLayers,
            QueryTriggerInteraction triggers = QueryTriggerInteraction.UseGlobal,
            int maxResults = DefaultMaxResults
        ) {
            var count = Box(box, _overlapCache, _overlapCache, layerMask, triggers, maxResults);
            return new ArraySegment<Collider>(_overlapCache, 0, count);
        }

        public static int Box(
            OrientedBox box,
            Collider[] results,
            int layerMask = Physics.AllLayers,
            QueryTriggerInteraction triggers = QueryTriggerInteraction.UseGlobal,
            int maxResults = DefaultMaxResults
        ) {
            return Box(box, _overlapCache, results, layerMask, triggers, maxResults);
        }

        public static int Box(
            OrientedBox box,
            Collider[] cache,
            Collider[] results,
            int layerMask = Physics.AllLayers,
            QueryTriggerInteraction triggers = QueryTriggerInteraction.UseGlobal,
            int maxResults = -1
        ) {
            maxResults = maxResults < 0 ? results.Length : math.min(maxResults, results.Length);
            OrientedBoxQuery query = box;
            var hitCount = Physics.OverlapBoxNonAlloc(
                box.center,
                box.halfExtent,
                cache,
                box.rotation,
                layerMask,
                triggers
            );
            var resultCount = 0;
            for (var i = 0; i < hitCount; i++) {
                if (resultCount >= maxResults) break;
                var collider = cache[i];
                if (query.ContainsPoint(collider.transform.position)) results[resultCount++] = collider;
            }

            return resultCount;
        }

        public static ArraySegment<Collider> Sphere(
            Vector3 center,
            float radius,
            int layerMask = Physics.AllLayers,
            QueryTriggerInteraction triggers = QueryTriggerInteraction.UseGlobal
        ) {
            var hits = Physics.OverlapSphereNonAlloc(center, radius, _overlapCache, layerMask, triggers);
            return new ArraySegment<Collider>(_overlapCache, 0, hits);
        }

        public static ArraySegment<Collider> Capsule(
            Vector3 point1,
            Vector3 point2,
            float radius,
            int layerMask = Physics.AllLayers,
            QueryTriggerInteraction triggers = QueryTriggerInteraction.UseGlobal
        ) {
            var hits = Physics.OverlapCapsuleNonAlloc(point1, point2, radius, _overlapCache, layerMask, triggers);
            return new ArraySegment<Collider>(_overlapCache, 0, hits);
        }


        public static ArraySegment<RaycastHit> Raycast(
            Vector3 origin,
            Vector3 direction,
            float maxDistance = Mathf.Infinity,
            int layerMask = Physics.DefaultRaycastLayers,
            QueryTriggerInteraction triggers = QueryTriggerInteraction.UseGlobal
        ) {
            var count = Physics.RaycastNonAlloc(origin, direction, _raycastCache, maxDistance, layerMask, triggers);
            return new ArraySegment<RaycastHit>(_raycastCache, 0, count);
        }

    }
}