using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Spookline.SPC.Geometry {
    [StructLayout(LayoutKind.Sequential)]
    public struct OrientedBoxQuery {

        public float3 center;
        public float3 halfExtent;

        // World-space local axes.
        // For inverse transform: local.x = dot(world - center, axisX), etc.
        public float3 axisX;
        public float3 axisY;
        public float3 axisZ;

        public OrientedBoxQuery(in OrientedBox box) {
            center = box.center;
            halfExtent = box.halfExtent;

            axisX = math.mul(box.rotation, new float3(1f, 0f, 0f));
            axisY = math.mul(box.rotation, new float3(0f, 1f, 0f));
            axisZ = math.mul(box.rotation, new float3(0f, 0f, 1f));
        }

        public static implicit operator OrientedBoxQuery(in OrientedBox box) => new(box);

        public static implicit operator OrientedBox(in OrientedBoxQuery query) =>
            new(query.center, query.halfExtent*2, quaternion.LookRotationSafe(query.axisZ, query.axisY));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 ToLocal(float3 point) {
            var d = point - center;

            return new float3(
                math.dot(d, axisX),
                math.dot(d, axisY),
                math.dot(d, axisZ)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 ToWorld(float3 local) {
            return center
                   + local.x * axisX
                   + local.y * axisY
                   + local.z * axisZ;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 ClosestPoint(float3 point) {
            var local = ToLocal(point);
            var closestLocal = math.clamp(local, -halfExtent, halfExtent);
            return ToWorld(closestLocal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool ContainsPoint(float3 point, float epsilon = 1e-5f) {
            var local = ToLocal(point);
            return math.all(math.abs(local) <= halfExtent + epsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool ContainsSphere(float3 sphereCenter, float radius, float epsilon = 1e-5f) {
            radius = math.max(radius, 0f);

            var local = ToLocal(sphereCenter);
            var requiredExtent = math.abs(local) + radius;

            return math.all(requiredExtent <= halfExtent + epsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool OverlapsSphere(float3 sphereCenter, float radius, float epsilon = 1e-5f) {
            radius = math.max(radius, 0f);

            var local = ToLocal(sphereCenter);
            var excess = math.max(math.abs(local) - halfExtent, 0f);

            var r = radius + epsilon;
            return math.lengthsq(excess) <= r * r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float DistanceSqToPoint(float3 point) {
            var local = ToLocal(point);
            var excess = math.max(math.abs(local) - halfExtent, 0f);
            return math.lengthsq(excess);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float DistanceToPoint(float3 point) {
            return math.sqrt(DistanceSqToPoint(point));
        }

    }
}