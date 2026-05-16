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
        public NativeList<RaycastCommand> raycastCommands;

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
            if (raycastProxyIndices.Length == 0) return;
            queried.Clear();
            hitProxies.Clear();
            indirectHitProxies.Clear();

            for (var i = 0; i < raycastCommands.Length; i++) {
                var hit = raycastResults[i];
                var proxyIdx = raycastProxyIndices[i];
                var proxy = proxies[proxyIdx];
                queried.Add(proxyIdx);

                var command = raycastCommands[i];
                var target = command.from + command.direction * command.distance;

                if (!hit.colliderEntityId.IsValid() || proxy.query.ContainsPoint(hit.point)) {
                    if (query.ContainsPoint(target)) {
                        hitProxies.Add(proxyIdx);
                    } else {
                        indirectHitProxies.Add(proxyIdx);
                    }
                }
            }

            for (var groupIdx = 0; groupIdx < proxyGroups.Length; groupIdx++) {
                var group = proxyGroups[groupIdx];
                var hasMissed = true;
                var hasQueried = false;
                for (var i = 0; i < group.proxyCount; i++) {
                    var proxyIdx = group.proxyIndex + i;
                    if (queried.Contains(proxyIdx)) hasQueried = true;

                    if (hitProxies.Contains(proxyIdx)) {
                        groupVisibility[groupIdx] |= ProxyGroupVisibility.Raycast;
                        hasMissed = false;
                        break;
                    }

                    if (indirectHitProxies.Contains(proxyIdx)) {
                        groupVisibility[groupIdx] |= ProxyGroupVisibility.Raycast;
                        // Missed is still false here
                    }
                }
                if (hasMissed && hasQueried) groupVisibility[groupIdx] |= ProxyGroupVisibility.Occluded;
            }
        }

    }
}