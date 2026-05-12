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

        Excluded = 1 << 0,
        Culled = 1 << 1,
        Occluded = 1 << 2,

        Visible = 1 << 5,
        SampleVisible = 1 << 6,
        Contained = 1 << 7

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