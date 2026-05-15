using System.Runtime.CompilerServices;
using Spookline.SPC.Geometry;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Spookline.SPC.Cleaver {

    public static class CleaverJobMath {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool MaskMatches(byte objectMask, byte queryMask) {
            return (objectMask & queryMask) != 0;
        }

    }

    [BurstCompile(FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct CleaverSectionPointContainmentJob : IJobParallelFor {

        [ReadOnly]
        public NativeArray<CleaverSectionData> sections;
        [ReadOnly]
        public NativeArray<CleaverVolumeData> volumes;

        public float3 point;
        public byte queryMask;

        public NativeHashSet<ulong> result;

        public void Execute(int index) {
            var section = sections[index];

            if (!CleaverJobMath.MaskMatches(section.mask, queryMask)) return;
            if (!section.bounds.Contains(point)) return;
            var contained = section.volumeCount == 0;

            for (var i = 0; i < section.volumeCount; i++) {
                var volume = volumes[section.volumeIndex + i];

                if (!volume.query.ContainsPoint(point)) continue;
                contained = true;
                break;
            }

            if (contained) result.Add(section.id);
        }

    }

    [BurstCompile(FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct CleaverSectionPointContainmentCollectJob : IJobParallelFor {

        [ReadOnly]
        public NativeArray<CleaverSectionData> sections;
        [ReadOnly]
        public NativeArray<CleaverVolumeData> volumes;

        public float3 point;
        public byte queryMask;

        public NativeQueue<ulong>.ParallelWriter containedSectionIds;

        public void Execute(int index) {
            var section = sections[index];

            if (!CleaverJobMath.MaskMatches(section.mask, queryMask))
                return;

            if (!section.bounds.Contains(point))
                return;

            var contained = section.volumeCount == 0;

            for (var i = 0; i < section.volumeCount; i++) {
                var volume = volumes[section.volumeIndex + i];

                if (volume.query.ContainsPoint(point)) {
                    contained = true;
                    break;
                }
            }

            if (contained)
                containedSectionIds.Enqueue(section.id);
        }

    }

    [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
    public struct CleaverSectionIdQueueToHashSetJob : IJob {

        public NativeQueue<ulong> containedSectionIds;
        public NativeHashSet<ulong> result;

        public void Execute() {
            while (containedSectionIds.TryDequeue(out var id)) result.Add(id);
        }

    }

    [BurstCompile(FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct CleaverFrustumProxyBroadPhaseJob : IJobParallelFor {

        [ReadOnly]
        public NativeArray<CleaverProxyGroupData> proxyGroups;

        [ReadOnly]
        public NativeArray<CleaverProxyData> proxies;

        public Frustum6 frustum;
        public float4x4 viewMatrix;
        public float3 viewerPoint;
        public float viewerRadiusSq;
        public byte queryMask;

        [WriteOnly]
        public NativeArray<ProxyGroupVisibility> groupVisibility;

        [NativeDisableParallelForRestriction]
        public NativeArray<float> proxyCoverage;

        public void Execute(int groupIndex) {
            var group = proxyGroups[groupIndex];

            if (!CleaverJobMath.MaskMatches(group.mask, queryMask)) {
                groupVisibility[groupIndex] = ProxyGroupVisibility.Excluded;
                ClearProxyRange(group);
                return;
            }

            var viewerInsideGroup = group.bounds.Contains(viewerPoint);
            var groupIntersectsFrustum = viewerInsideGroup || frustum.Intersects(group.bounds);

            if (!groupIntersectsFrustum) {
                groupVisibility[groupIndex] = ProxyGroupVisibility.Culled;
                ClearProxyRange(group);
                return;
            }

            groupVisibility[groupIndex] = viewerInsideGroup
                ? ProxyGroupVisibility.Contained | ProxyGroupVisibility.Visible
                : ProxyGroupVisibility.Visible;

            var start = group.proxyIndex;
            var end = start + group.proxyCount;

            for (var i = start; i < end; i++) {
                var proxy = proxies[i];

                if (proxy.query.DistanceSqToPoint(viewerPoint) <= viewerRadiusSq) {
                    proxyCoverage[i] = 1f;
                    continue;
                }

                if (frustum.Intersects(proxy.bounds)) {
                    FastFrustumHelpers.CalculateScreenCoverage(proxy.bounds, viewMatrix, out var coverage);
                    proxyCoverage[i] = coverage;
                    continue;
                }

                proxyCoverage[i] = 0f;
            }
        }

        private void ClearProxyRange(CleaverProxyGroupData group) {
            var start = group.proxyIndex;
            var end = start + group.proxyCount;

            for (var i = start; i < end; i++) {
                proxyCoverage[i] = 0f;
            }
        }

    }
}