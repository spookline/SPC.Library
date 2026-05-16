using Spookline.SPC.Geometry;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Spookline.SPC.Cleaver {
    [BurstCompile(FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct CleaverSphereSectionRaycastResultJob : IJob {

        [ReadOnly]
        public NativeArray<RaycastHit> raycastResults;

        [ReadOnly]
        public NativeList<int> raycastProxyIndices;

        [ReadOnly]
        public NativeArray<CleaverProxyGroupData> proxyGroups;

        [ReadOnly]
        public NativeArray<CleaverProxyData> proxies;

        [NativeDisableParallelForRestriction]
        public NativeArray<ProxyGroupVisibility> groupVisibility;

        public SphereSectionQuery query;
        public NativeHashSet<int> queried;
        public NativeHashSet<int> hitProxies;
        public NativeHashSet<int> indirectHitProxies;

        public void Execute() {
            if (raycastResults.Length == 0) return;
            queried.Clear();
            hitProxies.Clear();
            indirectHitProxies.Clear();

            for (var i = 0; i < raycastProxyIndices.Length; i++) {
                var hit = raycastResults[i];
                var proxyIdx = raycastProxyIndices[i];
                var proxy = proxies[proxyIdx];
                queried.Add(proxyIdx);

                if (!hit.colliderEntityId.IsValid() || proxy.query.ContainsPoint(hit.point)) {
                    if (query.ContainsPointOverlap(hit.point)) { hitProxies.Add(proxyIdx); } else {
                        indirectHitProxies.Add(proxyIdx);
                    }
                }
            }

            for (var groupIdx = 0; groupIdx < proxyGroups.Length; groupIdx++) {
                var group = proxyGroups[groupIdx];
                var hasMissed = false;
                for (var i = 0; i < group.proxyCount; i++) {
                    var proxyIdx = group.proxyIndex + i;
                    if (hitProxies.Contains(proxyIdx)) {
                        groupVisibility[groupIdx] |= ProxyGroupVisibility.Raycast;
                        hasMissed = false;
                        break;
                    }

                    if (indirectHitProxies.Contains(proxyIdx)) {
                        groupVisibility[groupIdx] |= ProxyGroupVisibility.Raycast;
                        // Missed is still false here
                    }

                    if (queried.Contains(proxyIdx)) hasMissed = true;
                }

                if (hasMissed) groupVisibility[groupIdx] |= ProxyGroupVisibility.Occluded;
            }
        }

    }
}