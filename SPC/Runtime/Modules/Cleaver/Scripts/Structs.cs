using System;
using Spookline.SPC.Geometry;
using Unity.Mathematics.Geometry;
using UnityEngine;

namespace Spookline.SPC.Cleaver {
    public struct CleaverProxyGroupData {

        public ulong id;
        public int parentIndex;
        public byte mask;

        public MinMaxAABB bounds;

        public int proxyIndex;
        public byte proxyCount;

    }

    public struct CleaverSectionData {

        public ulong id;
        public int regionIndex;
        public MinMaxAABB bounds;
        public bool closed;
        public byte mask;

        public int volumeIndex;
        public int portalIndex;

        public byte volumeCount;
        public byte portalCount;

    }

    public struct CleaverProxyData {

        public ulong id;
        public MinMaxAABB bounds;
        public OrientedBoxQuery query;
        public float radius;

        public int pointIndex;
        public ushort pointCount; // Total count (most)
        public ushort tier0; // Total count for t0 (least)
        public ushort tier1; // Total count for t1 (normal)
        public ushort tier2; // Total count for t2 (higher)

    }

    public struct CleaverVolumeData {

        public OrientedBoxQuery query;

    }

    public struct CleaverPortalData {

        // If this is -1 and regionIndex is specified, the portal functions as an "entrance" or "window"
        public int fromIndex;

        public int toIndex;

        public bool open;

        // If specified, the region must be visible for the portal to be open
        public int regionIndex;

    }


    [Flags]
    public enum ProxyGroupVisibility : byte {

        // Initial state, the region's visibility is not yet evaluated.
        None = 0,

        /// <summary>
        /// The group is excluded from the visibility check because of the <see cref="CleaverProxyGroupData.mask"/>.
        /// </summary>
        Excluded = 1 << 0,

        /// <summary>
        /// The group is culled by the <see cref="CleaverProxyData.bounds"/> volume.
        /// </summary>
        Culled = 1 << 1,

        /// <summary>
        /// The group was not <see cref="Excluded"/> or <see cref="Culled"/>, but no raycast hit was inside of the
        /// <see cref="CleaverProxyData.query"/> volume.
        /// </summary>
        /// <remarks>
        /// If both <see cref="Occluded"/> and <see cref="Raycast"/> are true, the group has a raycast hit, that is valid
        /// from the viewer's camera position but is outside the viewer's visibility volume.
        /// </remarks>
        Occluded = 1 << 2,

        /// <summary>
        /// The viewer's visibility volume intersects the <see cref="CleaverProxyData.bounds"/> volume.
        /// </summary>
        Frustum = 1 << 4,

        /// <summary>
        /// The viewer's position is fully inside a <see cref="CleaverProxyData.bounds"/> volume.
        /// </summary>
        Bounds = 1 << 5,

        /// <summary>
        /// The viewer can see a sample point of a <see cref="CleaverProxy"/> from any camera rotation.
        /// </summary>
        /// <remarks>
        /// This flag is conservative and does not guarantee that the raycast hits are inside the viewing volume,
        /// just that from the viewer's position, there is a valid raycast hit that is not occluded.
        ///
        /// To further restrict the raycast hits, use <see cref="Occluded"/>.
        /// </remarks>
        Raycast = 1 << 6,

        /// <summary>
        /// The viewer's position is fully inside a <see cref="CleaverProxyData.query"/> volume.
        /// </summary>
        Contained = 1 << 7,

        /// <summary>
        /// All negative flags combined.
        /// </summary>
        AllNegative = Excluded | Culled | Occluded,

        /// <summary>
        /// All strictly negative flags, I.e. excluding <see cref="Occluded"/>.
        /// </summary>
        AllStrictNegative = Excluded | Culled,

        /// <summary>
        /// All positive flags combined.
        /// </summary>
        /// <remarks>
        /// To check if a group is completely visible, do Bitwise-And on <see cref="AllPositive"/> and verify that
        /// the result is equal and not <see cref="None"/> -> Any visibility flag is set and no negative flags are set.
        /// </remarks>
        AllPositive = Frustum | Bounds | Raycast | Contained

    }

    public static class ProxyGroupVisibilityExtensions {

        public static bool IsVisible(this ProxyGroupVisibility visibility) {
            return visibility != ProxyGroupVisibility.None &&
                   (visibility & ProxyGroupVisibility.AllPositive) == visibility;
        }

        public static bool IsBroadVisible(this ProxyGroupVisibility visibility) {
            return visibility != ProxyGroupVisibility.None &&
                   (visibility & ProxyGroupVisibility.AllPositive) != ProxyGroupVisibility.None &&
                   (visibility & ProxyGroupVisibility.AllStrictNegative) == ProxyGroupVisibility.None;
        }

        public static Color ToDebugColor(this ProxyGroupVisibility visibility) {
            if (visibility.IsVisible()) {
                if (visibility.HasFlag(ProxyGroupVisibility.Contained)) return Color.blue;
                return new Color(0f, 1f, 1f, 1f) {
                    g = visibility.HasFlag(ProxyGroupVisibility.Raycast) ? 1f : 0f,
                    b = visibility.HasFlag(ProxyGroupVisibility.Bounds) ? 1f : 0f
                };

            }

            if (visibility.HasFlag(ProxyGroupVisibility.Occluded)) return Color.yellow;
            if (visibility.HasFlag(ProxyGroupVisibility.Culled)) return Color.red;
            if (visibility.HasFlag(ProxyGroupVisibility.Excluded)) return Color.gray;
            return Color.white;
        }

    }

    [Flags]
    public enum SectionVisibility : byte {

        None = 0,

        // The section's AABB intersects the viewer frustum.
        Frustum = 1 << 0,

        // The section's AABB and any of its OBBs (when defined) contain the viewer position.
        Contained = 1 << 1,

        // The section's AABB intersects the viewer frustum.
        // If closed, there also must be a portal-path from the viewer to the section.
        Visible = 1 << 2,

        // The distance between the viewer and the closest point in the section AABB is smaller than the distance threshold.
        // Not connected to visibility and region visibility.
        Near = 1 << 5

    }

    [Flags]
    public enum ByteMask : byte {

        None = byte.MinValue,
        Player = 1 << 0,
        Client = 1 << 1,
        Npc = 1 << 2,
        World = 1 << 3,
        A = 1 << 4,
        B = 1 << 5,
        C = 1 << 6,
        D = 1 << 7,
        All = byte.MaxValue

    }

    public class NavmeshAgentTypeAttribute : PropertyAttribute { }

    public class NavmeshAreaAttribute : PropertyAttribute { }
}