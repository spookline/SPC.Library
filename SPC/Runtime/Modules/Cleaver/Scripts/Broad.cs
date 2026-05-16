using System.Runtime.CompilerServices;
using Spookline.SPC.Geometry;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

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

        [NativeDisableParallelForRestriction]
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
                ? ProxyGroupVisibility.InBounds | ProxyGroupVisibility.VisibleFrustum
                : ProxyGroupVisibility.VisibleFrustum;

            var start = group.proxyIndex;
            var end = start + group.proxyCount;

            for (var i = start; i < end; i++) {
                var proxy = proxies[i];

                if (proxy.query.DistanceSqToPoint(viewerPoint) <= viewerRadiusSq) {
                    proxyCoverage[i] = 2f;
                    groupVisibility[groupIndex] |= ProxyGroupVisibility.SampleVisible | ProxyGroupVisibility.Contained;
                    continue;
                }

                if (frustum.Intersects(proxy.bounds)) {
                    FastFrustumHelpers.CalculateScreenCoverage(proxy.bounds, viewMatrix, out var coverage);
                    proxyCoverage[i] = coverage;
                    continue;
                }

                proxyCoverage[i] = -1f;
            }
        }

        private void ClearProxyRange(CleaverProxyGroupData group) {
            var start = group.proxyIndex;
            var end = start + group.proxyCount;

            for (var i = start; i < end; i++) {
                proxyCoverage[i] = -1f;
            }
        }

    }

    [BurstCompile(FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct CleaverProxyRaycastCommandJob : IJob {

        [ReadOnly]
        public NativeArray<CleaverProxyData> proxies;

        [ReadOnly]
        public NativeArray<float3> samplePoints;

        [ReadOnly]
        public NativeArray<float> proxyCoverage;

        public float3 viewerPoint;
        public int layerMask;

        [WriteOnly]
        public NativeList<RaycastCommand> raycastCommands;

        [WriteOnly]
        public NativeList<int> raycastProxyIndices;

        public void Execute() {
            raycastCommands.Clear();
            raycastProxyIndices.Clear();

            for (var i = 0; i < proxies.Length; i++) {
                var coverage = proxyCoverage[i];

                if (coverage is <= 0f or > 1f) continue;

                var proxy = proxies[i];
                var pointStart = proxy.pointIndex;
                var pointEnd = pointStart + proxy.pointCount;

                for (var pointIdx = pointStart; pointIdx < pointEnd; pointIdx++) {
                    var targetPoint = samplePoints[pointIdx];
                    var direction = targetPoint - viewerPoint;
                    var distance = math.length(direction);

                    if (distance < 0.001f) continue;

                    raycastCommands.Add(new RaycastCommand(
                        viewerPoint,
                        math.normalize(direction),
                        new QueryParameters { layerMask = layerMask },
                        distance
                    ));

                    raycastProxyIndices.Add(i);
                }
            }
        }
    }

    [BurstCompile(FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct CleaverProxyRaycastResultJob : IJob {

        [ReadOnly]
        public NativeArray<RaycastHit> raycastResults;

        [ReadOnly]
        public NativeList<int> raycastProxyIndices;

        [ReadOnly]
        public NativeArray<CleaverProxyGroupData> proxyGroups;

        [ReadOnly]
        public NativeArray<CleaverProxyData> proxies;

        public NativeArray<ProxyGroupVisibility> groupVisibility;

        public void Execute() {
            if (raycastResults.Length == 0)
                return;

            var hitProxies = new NativeHashSet<int>(proxies.Length, Allocator.Temp);
            var queried = new NativeHashSet<int>(proxies.Length, Allocator.Temp);

            try {
                for (var i = 0; i < raycastProxyIndices.Length; i++) {
                    var hit = raycastResults[i];
                    var proxyIdx = raycastProxyIndices[i];
                    var proxy = proxies[proxyIdx];
                    queried.Add(proxyIdx);
                    if (!hit.colliderEntityId.IsValid() || proxy.query.ContainsPoint(hit.point)) {
                        hitProxies.Add(proxyIdx);
                    }
                }

                for (var groupIdx = 0; groupIdx < proxyGroups.Length; groupIdx++) {
                    var group = proxyGroups[groupIdx];
                    var hasMissed = false;
                    for (var i = 0; i < group.proxyCount; i++) {
                        var proxyIdx = group.proxyIndex + i;
                        if (hitProxies.Contains(proxyIdx)) {
                            groupVisibility[groupIdx] |= ProxyGroupVisibility.SampleVisible;
                            hasMissed = false;
                            break;
                        }

                        if (queried.Contains(proxyIdx)) hasMissed = true;
                    }

                    if (hasMissed) groupVisibility[groupIdx] |= ProxyGroupVisibility.Occluded;

                }

            } finally {
                hitProxies.Dispose();
                queried.Dispose();
            }
        }

    }

    [BurstCompile(FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct CleaverSphereSectionProxyBroadPhaseJob : IJobParallelFor {

        [ReadOnly]
        public NativeArray<CleaverProxyGroupData> proxyGroups;

        [ReadOnly]
        public NativeArray<CleaverProxyData> proxies;

        public SphereSectionQuery query;
        public float3 viewerPoint;
        public float viewerRadiusSq;
        public byte queryMask;

        [NativeDisableParallelForRestriction]
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
            var groupIntersectsSphere = viewerInsideGroup || query.IntersectsBroad(group.bounds);

            if (!groupIntersectsSphere) {
                groupVisibility[groupIndex] = ProxyGroupVisibility.Culled;
                ClearProxyRange(group);
                return;
            }

            groupVisibility[groupIndex] = viewerInsideGroup
                ? ProxyGroupVisibility.InBounds | ProxyGroupVisibility.VisibleFrustum
                : ProxyGroupVisibility.VisibleFrustum;

            var start = group.proxyIndex;
            var end = start + group.proxyCount;

            for (var i = start; i < end; i++) {
                var proxy = proxies[i];

                if (proxy.query.DistanceSqToPoint(viewerPoint) <= viewerRadiusSq) {
                    proxyCoverage[i] = 2f;
                    groupVisibility[groupIndex] |= ProxyGroupVisibility.SampleVisible | ProxyGroupVisibility.Contained;
                    continue;
                }

                if (query.IntersectsBroad(proxy.bounds)) {
                    proxyCoverage[i] = 1f;
                    continue;
                }

                proxyCoverage[i] = -1f;
            }
        }

        private void ClearProxyRange(CleaverProxyGroupData group) {
            var start = group.proxyIndex;
            var end = start + group.proxyCount;

            for (var i = start; i < end; i++) {
                proxyCoverage[i] = -1f;
            }
        }

    }
}