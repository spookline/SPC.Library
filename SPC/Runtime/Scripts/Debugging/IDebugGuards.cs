using Spookline.SPC.Geometry;
using Unity.Mathematics.Geometry;
using UnityEngine;

namespace Spookline.SPC.Debugging {
    public interface IDebugGuards { }

    public interface IDebugCullable {

        public Frustum6 Frustum { get; }
        public bool HasFrustum { get; }
    }

    public static class DebugGuards {

        public static bool HasFlag<T>(this T guards, string flag) where T : IDebugGuards => Globals.HasDebugFlag(flag);

        public static bool HasFlagOrDebugging<T>(this T guards, string flag) where T : IDebugGuards =>
            Globals.HasDebugFlagOrDebugging(flag);

        public static bool HasFlag<T>(this T guards, params string[] flag) where T : IDebugGuards {
            foreach (var f in flag) if (Globals.HasDebugFlag(f)) return true;
            return false;
        }

        public static bool HasFlagOrDebugging<T>(this T guards, params string[] flag) where T : IDebugGuards {
            foreach (var f in flag) if (Globals.HasDebugFlagOrDebugging(f)) return true;
            return false;
        }

        public static bool Cull<T>(this T guards, MinMaxAABB aabb) where T : IDebugCullable =>
            guards.HasFrustum && !guards.Frustum.Intersects(aabb);

        public static bool Cull<T>(this T guards, Bounds bounds) where T : IDebugCullable =>
            guards.HasFrustum && !guards.Frustum.Intersects(new MinMaxAABB(bounds.min, bounds.max));

        public static bool Cull<T>(this T guards, Vector3 position) where T : IDebugCullable =>
            guards.HasFrustum && !guards.Frustum.Contains(new MinMaxAABB(position, position));

        public static bool Cull<T>(this T guards, OrientedBox box) where T : IDebugCullable =>
            guards.HasFrustum && !guards.Frustum.Intersects(box.AABB());

    }
}