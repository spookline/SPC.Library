using Sirenix.OdinInspector;
using Spookline.SPC.Geometry;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Cleaver {
    [HideMonoScript]
    [DefaultExecutionOrder(-50)]
    [AddComponentMenu("Cleaver/Frustum Viewer")]
    public class CleaverFrustumViewer : CleaverViewerBase<CleaverFrustumViewer> {

        public float detailThreshold = FrustumHelper.InverseRemapPerceptionScreenCoverage(0.25f);
        public float screenThreshold = FrustumHelper.InverseRemapPerceptionScreenCoverage(0.10f);
        public float nearDistance = 1f;

        public Camera trackedCamera;

        public Frustum6 frustum;
        public float4x4 worldToProjectionMatrix;

        private ulong _lastHash;


        protected override void Awake() {
            base.Awake();
            On<CleaverBatchedViewerRefreshEvt>().Do(OnBatchedRefresh);
            On<CleaverBatchedViewerRaycastEvt>().Do(OnBatchedRaycast);
        }

        protected override void RefreshTransforms() {
            if (!trackedCamera) return;
            position = trackedCamera.transform.position;
            rotation = trackedCamera.transform.rotation;
        }


        public void Update() {
            if (!trackedCamera) return;
            // for (var i = 0; i < groupVisibility.Length; i++) { Debug.Log($"Group {i}: {groupVisibility[i]}"); }
        }

        private void OnBatchedRefresh(ref CleaverBatchedViewerRefreshEvt args) {
            if (!isActiveAndEnabled) return;
            if (!trackedCamera) return;
            var lastPosition = position;
            var lastRotation = rotation;
            position = trackedCamera.transform.position;
            rotation = trackedCamera.transform.rotation;

            raycastCommands.Clear();
            raycastProxyIndices.Clear();
            RefitArrays(args.environment);

            if (!SpacialHash.PosRot(lastPosition, position, lastRotation, rotation)) {
                trackedCamera.CalculateFrustum6(ref frustum);
                worldToProjectionMatrix = trackedCamera.CalculateWorldToProjectionMatrix();

                var sectionJob = args.environment.QuerySections(position, (byte)occlusionMask, currentSections);
                args.batch.Add(sectionJob);

                var batchJob = BroadPhase(args.environment);
                args.batch.Add(batchJob);
            }
        }

        private void OnBatchedRaycast(ref CleaverBatchedViewerRaycastEvt args) {
            if (!isActiveAndEnabled) return;
            if (raycastCommands.Length == 0) return;

            var batchHandle = RaycastCommand.ScheduleBatch(
                raycastCommands.AsArray(),
                raycastResults,
                32
            );
            var resultJob = new CleaverFrustumRaycastResultJob {
                raycastResults = raycastResults,
                raycastProxyIndices = raycastProxyIndices,
                raycastCommands = raycastCommands,
                proxyGroups = args.environment.proxyGroups,
                proxies = args.environment.proxies,
                groupVisibility = groupVisibility,
                frustum = frustum,

                queried = queriedProxies,
                hitProxies = visibleProxies,
                indirectHitProxies = indirectVisibleProxies,
            };
            var resultHandle = resultJob.Schedule(batchHandle);
            args.batch.Add(resultHandle);
        }

        private JobHandle BroadPhase(CleaverEnvironment env) {
            const int mainThreadBatchSize = 32;
            const int batchSize = 128;
            for (var i = 0; i < groupVisibility.Length; i++) { groupVisibility[i] = ProxyGroupVisibility.None; }

            var broadPhaseJob = new CleaverFrustumProxyBroadPhaseJob {
                proxyGroups = env.proxyGroups,
                proxies = env.proxies,
                frustum = frustum,
                viewMatrix = worldToProjectionMatrix,
                viewerPoint = position,
                viewerRadiusSq = nearDistance * nearDistance,
                queryMask = (byte)visibilityMask,
                groupVisibility = groupVisibility,
                proxyCoverage = proxyCoverage
            };
            var commandJob = new CleaverProxyRaycastCommandJob {
                proxies = env.proxies,
                samplePoints = env.samplePoints,
                proxyCoverage = proxyCoverage,
                viewerPoint = position,
                layerMask = env.GetMask(occlusionMask),
                raycastCommands = raycastCommands,
                raycastProxyIndices = raycastProxyIndices,
            };

            var groupsLength = env.proxyGroups.Length;
            if (groupsLength < mainThreadBatchSize) {
                broadPhaseJob.Run(groupsLength);
                commandJob.Run();
                return default;
            }

            var broadPhaseHandle = broadPhaseJob.Schedule(groupsLength, batchSize);
            return commandJob.Schedule(broadPhaseHandle);
        }

    }
}