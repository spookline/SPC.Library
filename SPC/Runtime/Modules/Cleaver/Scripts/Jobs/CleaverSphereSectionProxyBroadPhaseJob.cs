using Spookline.SPC.Geometry;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Spookline.SPC.Cleaver {
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

            if ((group.mask & queryMask) == 0) {
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
                ? ProxyGroupVisibility.Bounds | ProxyGroupVisibility.Frustum
                : ProxyGroupVisibility.Frustum;

            var start = group.proxyIndex;
            var end = start + group.proxyCount;

            for (var i = start; i < end; i++) {
                var proxy = proxies[i];

                if (proxy.query.DistanceSqToPoint(viewerPoint) <= viewerRadiusSq) {
                    proxyCoverage[i] = 2f;
                    groupVisibility[groupIndex] |= ProxyGroupVisibility.AllPositive;
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

            for (var i = start; i < end; i++) { proxyCoverage[i] = -1f; }
        }

    }
}