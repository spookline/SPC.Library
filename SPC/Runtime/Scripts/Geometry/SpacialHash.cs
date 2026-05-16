using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Geometry {
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [BurstCompile]
    public static class SpacialHash {

        private const float _positionStep = 0.01f;
        private const float _scaleStep = 0.001f;
        private const float _eulerStepDeg = 0.1f;
        private const float _directionStep = 0.001f;
        private const float _quaternionStep = 0.0005f;
        private const float _boundsStep = 0.01f;

        public static int3 ToGrid(float3 position, float gridSize) {
            return (int3)math.round(position / gridSize);
        }

        public static float3 FromGrid(int3 gridPosition, float gridSize) {
            return gridPosition * new float3(gridSize);
        }

        // [XXXXXX][ZZZZZZ][YYYY] | 24bit 24bit 16bit
        public static long PackGridPosition(int3 position) {
            var x = (long)position.x & 0xFFFFFF;
            var z = (long)position.z & 0xFFFFFF;
            var y = (long)position.y & 0xFFFF;

            return (x << 40) | (z << 16) | y;
        }

        // [XXXXXX][ZZZZZZ][YYYY] | 24bit 24bit 16bit
        public static int3 UnpackGridPosition(long gridPosition) {
            var x = (int)((gridPosition >> 40) & 0xFFFFFF);
            var z = (int)((gridPosition >> 16) & 0xFFFFFF);
            var y = (int)(gridPosition & 0xFFFF);

            return new int3(x, y, z);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Pos(float3 position) {
            var pos = QuantizePosition(position);
            return Hash(pos.x, pos.y, pos.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Pos(float3 previous, float3 current) {
            var a = QuantizePosition(previous);
            var b = QuantizePosition(current);
            return Equal(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Dir(float3 direction) {
            var dir = QuantizeDirection(direction);
            return Hash(dir.x, dir.y, dir.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Dir(float3 previous, float3 current) {
            var a = QuantizeDirection(previous);
            var b = QuantizeDirection(current);
            return Equal(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Euler(float3 eulerDeg) {
            var euler = QuantizeEulerDeg(eulerDeg);
            return Hash(euler.x, euler.y, euler.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Euler(float3 previous, float3 current) {
            var a = QuantizeEulerDeg(previous);
            var b = QuantizeEulerDeg(current);
            return Equal(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Rot(quaternion rotation) {
            var quat = QuantizeQuaternion(rotation);
            return Hash(quat.x, quat.y, quat.z, quat.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Rot(quaternion previous, quaternion current) {
            var a = QuantizeQuaternion(previous);
            var b = QuantizeQuaternion(current);
            return Equal(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Scale(float3 scale) {
            var scl = QuantizeScale(scale);
            return Hash(scl.x, scl.y, scl.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Scale(float3 previous, float3 current) {
            var a = QuantizeScale(previous);
            var b = QuantizeScale(current);
            return Equal(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Bounds(Bounds bounds) {
            var center = QuantizePosition(bounds.center);
            var extents = QuantizeBounds(bounds.extents);
            return Hash(center.x, center.y, center.z, extents.x, extents.y, extents.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Bounds(Bounds previous, Bounds current) {
            var a = QuantizePosition(previous.center);
            var b = QuantizePosition(current.center);
            if (!Equal(a, b)) return false;

            var c = QuantizeBounds(previous.extents);
            var d = QuantizeBounds(current.extents);
            return Equal(c, d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Forward(float3 position, float3 forward) {
            var pos = QuantizePosition(position);
            var fwd = QuantizeDirection(forward);
            return Hash(pos.x, pos.y, pos.z, fwd.x, fwd.y, fwd.z);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong PosRot(float3 position, quaternion rotation) {
            var pos = QuantizePosition(position);
            var rot = QuantizeQuaternion(rotation);
            return Hash(pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PosRot(
            float3 previous,
            float3 current,
            quaternion previousRotation,
            quaternion currentRotation
        ) {
            var a = QuantizePosition(previous);
            var b = QuantizePosition(current);
            if (!Equal(a, b)) return false;
            var c = QuantizeQuaternion(previousRotation);
            var d = QuantizeQuaternion(currentRotation);
            return Equal(c, d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong PosRot(float3 position, float3 eulerAngles) {
            var pos = QuantizePosition(position);
            var rot = QuantizeEulerDeg(eulerAngles);
            return Hash(pos.x, pos.y, pos.z, rot.x, rot.y, rot.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong PosRotScale(float3 position, quaternion rotation, float3 scale) {
            var pos = QuantizePosition(position);
            var rot = QuantizeQuaternion(rotation);
            var scl = QuantizeScale(scale);
            return Hash(pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w, scl.x, scl.y, scl.z);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong PosRotScale(float3 position, float3 rotation, float3 scale) {
            var pos = QuantizePosition(position);
            var rot = QuantizeEulerDeg(rotation);
            var scl = QuantizeScale(scale);
            return Hash(pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, scl.x, scl.y, scl.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong PosRot(Transform transform) {
            return PosRot(transform.position, transform.rotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong PosRotScale(Transform transform) {
            return PosRotScale(transform.position, transform.rotation, transform.lossyScale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Finalize(ulong hash) {
            unchecked {
                hash ^= hash >> 33;
                hash *= 0xff51afd7ed558ccdUL;
                hash ^= hash >> 33;
                hash *= 0xc4ceb9fe1a85ec53UL;
                hash ^= hash >> 33;

                return (int)(hash ^ (hash >> 32));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FinalizeFast(ulong hash) {
            unchecked {
                hash ^= hash >> 33;
                hash *= 0xff51afd7ed558ccdUL;
                hash ^= hash >> 33;

                return (int)(hash ^ (hash >> 32));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int3 QuantizePosition(float3 v) {
            return (int3)math.round(v / _positionStep);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int3 QuantizeScale(float3 v) {
            return (int3)math.round(v / _scaleStep);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int3 QuantizeEulerDeg(float3 eulerDeg) {
            return (int3)math.round(eulerDeg / _eulerStepDeg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int3 QuantizeDirection(float3 dir) {
            dir = math.normalizesafe(dir);
            return (int3)math.round(dir / _directionStep);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int4 QuantizeQuaternion(quaternion q) {
            var v = q.value;

            // q and -q represent the same rotation.
            if (v.w < 0f)
                v = -v;

            v = math.normalize(v);

            return (int4)math.round(v / _quaternionStep);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int3 QuantizeBounds(float3 comp) {
            return (int3)math.round(comp / _boundsStep);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equal(int3 a, int3 b) {
            return a.x == b.x && a.y == b.y && a.z == b.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equal(int4 a, int4 b) {
            return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Hash(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j) {
            unchecked {
                var hash = 14695981039346656037UL;

                hash = (hash ^ (uint)a) * 1099511628211UL;
                hash = (hash ^ (uint)b) * 1099511628211UL;
                hash = (hash ^ (uint)c) * 1099511628211UL;
                hash = (hash ^ (uint)d) * 1099511628211UL;
                hash = (hash ^ (uint)e) * 1099511628211UL;
                hash = (hash ^ (uint)f) * 1099511628211UL;
                hash = (hash ^ (uint)g) * 1099511628211UL;
                hash = (hash ^ (uint)h) * 1099511628211UL;
                hash = (hash ^ (uint)i) * 1099511628211UL;
                hash = (hash ^ (uint)j) * 1099511628211UL;

                return hash;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Hash(int a, int b, int c, int d, int e, int f, int g, int h, int i) {
            unchecked {
                var hash = 14695981039346656037UL;

                hash = (hash ^ (uint)a) * 1099511628211UL;
                hash = (hash ^ (uint)b) * 1099511628211UL;
                hash = (hash ^ (uint)c) * 1099511628211UL;
                hash = (hash ^ (uint)d) * 1099511628211UL;
                hash = (hash ^ (uint)e) * 1099511628211UL;
                hash = (hash ^ (uint)f) * 1099511628211UL;
                hash = (hash ^ (uint)g) * 1099511628211UL;
                hash = (hash ^ (uint)h) * 1099511628211UL;
                hash = (hash ^ (uint)i) * 1099511628211UL;

                return hash;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Hash(int a, int b, int c, int d, int e, int f, int g) {
            unchecked {
                var hash = 14695981039346656037UL;

                hash = (hash ^ (uint)a) * 1099511628211UL;
                hash = (hash ^ (uint)b) * 1099511628211UL;
                hash = (hash ^ (uint)c) * 1099511628211UL;
                hash = (hash ^ (uint)d) * 1099511628211UL;
                hash = (hash ^ (uint)e) * 1099511628211UL;
                hash = (hash ^ (uint)f) * 1099511628211UL;
                hash = (hash ^ (uint)g) * 1099511628211UL;

                return hash;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Hash(int a, int b, int c, int d, int e, int f) {
            unchecked {
                var hash = 14695981039346656037UL;

                hash = (hash ^ (uint)a) * 1099511628211UL;
                hash = (hash ^ (uint)b) * 1099511628211UL;
                hash = (hash ^ (uint)c) * 1099511628211UL;
                hash = (hash ^ (uint)d) * 1099511628211UL;
                hash = (hash ^ (uint)e) * 1099511628211UL;
                hash = (hash ^ (uint)f) * 1099511628211UL;

                return hash;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Hash(int a, int b, int c, int d) {
            unchecked {
                var hash = 14695981039346656037UL;

                hash = (hash ^ (uint)a) * 1099511628211UL;
                hash = (hash ^ (uint)b) * 1099511628211UL;
                hash = (hash ^ (uint)c) * 1099511628211UL;
                hash = (hash ^ (uint)d) * 1099511628211UL;

                return hash;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Hash(int a, int b, int c) {
            unchecked {
                var hash = 14695981039346656037UL;

                hash = (hash ^ (uint)a) * 1099511628211UL;
                hash = (hash ^ (uint)b) * 1099511628211UL;
                hash = (hash ^ (uint)c) * 1099511628211UL;

                return hash;
            }
        }

    }
}