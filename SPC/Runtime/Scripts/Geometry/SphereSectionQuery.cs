using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;

namespace Spookline.SPC.Geometry {
    public struct SphereSectionQuery {

        public float3 origin;
        public float radius;

        // Must be normalized.
        public float3 direction;

        // Cosine of half-angle.
        // Example: 60 degree FOV => half angle 30 degrees => cosAngle = cos(radians(30)).
        public float cosAngle;

        public float radiusSq;

        public SphereSectionQuery(float3 origin, float3 direction, float halfAngleRadians, float radius) {
            this.origin = origin;
            this.radius = math.max(radius, 0f);
            this.direction = math.normalizesafe(direction, new float3(0f, 0f, 1f));
            this.cosAngle = math.cos(halfAngleRadians);
            this.radiusSq = this.radius * this.radius;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SphereSectionQuery FromDegrees(float3 origin, float3 direction, float degrees, float distance) {
            return new SphereSectionQuery(origin, direction, math.radians(degrees) * 0.5f, distance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsPoint(float3 point, float epsilon = 1e-5f) {
            var v = point - origin;
            var distSq = math.lengthsq(v);

            if (distSq > radiusSq + epsilon) return false;
            if (distSq <= epsilon) return true;

            var dist = math.sqrt(distSq);
            var axial = math.dot(v, direction);

            return axial >= dist * cosAngle - epsilon;
        }

        // Minimal optimization with one branch less for sphere overlap checks.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsPointOverlap(float3 point, float epsilon = 1e-5f) {
            var v = point - origin;
            var distSq = math.lengthsq(v);

            if (distSq <= epsilon) return true;

            var dist = math.sqrt(distSq);
            var axial = math.dot(v, direction);

            return axial >= dist * cosAngle - epsilon;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(float3 sphereCenter, float sphereRadius, float epsilon = 1e-5f) {
            sphereRadius = math.max(sphereRadius, 0f);

            var v = sphereCenter - origin;
            var distSq = math.lengthsq(v);
            var dist = math.sqrt(distSq);

            // Must fit inside the finite radius sphere.
            if (dist + sphereRadius > radius + epsilon)
                return false;

            if (sphereRadius <= epsilon)
                return ContainsPoint(sphereCenter, epsilon);

            // A non-zero sphere around the cone origin cannot be fully contained
            // by a normal directional cone.
            if (dist <= sphereRadius + epsilon)
                return false;

            var centerCos = math.dot(v, direction) / dist;

            // Sphere angular radius as seen from cone origin.
            var sinAlpha = math.saturate(sphereRadius / dist);
            var cosAlpha = math.sqrt(math.max(0f, 1f - sinAlpha * sinAlpha));

            var sinCone = math.sqrt(math.max(0f, 1f - cosAngle * cosAngle));

            // For full containment:
            // center angle + sphere angular radius <= cone half angle.
            var requiredCos = cosAngle * cosAlpha + sinCone * sinAlpha;

            return centerCos >= requiredCos - epsilon;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(float3 sphereCenter, float sphereRadius, float epsilon = 1e-5f) {
            sphereRadius = math.max(sphereRadius, 0f);

            var v = sphereCenter - origin;
            var distSq = math.lengthsq(v);

            var maxDist = radius + sphereRadius + epsilon;
            if (distSq > maxDist * maxDist) return false;
            if (distSq <= epsilon) return true;

            var dist = math.sqrt(distSq);

            // Sphere contains the cone origin.
            if (dist <= sphereRadius + epsilon) return true;

            var centerCos = math.dot(v, direction) / dist;

            // Conservative angular expansion.
            // If the sphere subtends angle alpha, test:
            // centerAngle <= coneAngle + alpha
            var sinAlpha = math.saturate(sphereRadius / dist);
            var cosAlpha = math.sqrt(math.max(0f, 1f - sinAlpha * sinAlpha));

            var sinCone = math.sqrt(math.max(0f, 1f - cosAngle * cosAngle));

            var expandedCos = cosAngle * cosAlpha - sinCone * sinAlpha;

            return centerCos >= expandedCos - epsilon;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(MinMaxAABB aabb, float epsilon = 1e-5f) {
            for (var i = 0; i < 8; i++) {
                var p = new float3(
                    (i & 1) == 0 ? aabb.Min.x : aabb.Max.x,
                    (i & 2) == 0 ? aabb.Min.y : aabb.Max.y,
                    (i & 4) == 0 ? aabb.Min.z : aabb.Max.z
                );

                if (!ContainsPoint(p, epsilon))
                    return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(OrientedBoxQuery box, float epsilon = 1e-5f) {
            var e = box.halfExtent;

            for (var i = 0; i < 8; i++) {
                var p = box.center;

                p += ((i & 1) == 0 ? -e.x : e.x) * box.axisX;
                p += ((i & 2) == 0 ? -e.y : e.y) * box.axisY;
                p += ((i & 4) == 0 ? -e.z : e.z) * box.axisZ;

                if (!ContainsPoint(p, epsilon))
                    return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IntersectsBroad(MinMaxAABB aabb, float epsilon = 1e-5f) {
            var boundingRadius = math.length(aabb.Extents);

            // Conservative: AABB is approximated by its bounding sphere.
            return Intersects(aabb.Center, boundingRadius, epsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IntersectsBroad(OrientedBoxQuery box, float epsilon = 1e-5f) {
            // First reject: finite sphere of cone vs OBB.
            if (!box.OverlapsSphere(origin, radius, epsilon)) return false;

            // Conservative angular test: approximate OBB by bounding sphere.
            var boundingRadius = math.length(box.halfExtent);

            return Intersects(box.center, boundingRadius, epsilon);
        }

    }
}