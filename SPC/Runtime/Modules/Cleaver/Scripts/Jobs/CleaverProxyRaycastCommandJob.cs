using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Cleaver {
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

                    raycastCommands.Add(
                        new RaycastCommand(
                            viewerPoint,
                            math.normalize(direction),
                            new QueryParameters { layerMask = layerMask },
                            distance
                        )
                    );

                    raycastProxyIndices.Add(i);
                }
            }
        }

    }
}