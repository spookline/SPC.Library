using Sirenix.OdinInspector;
using Spookline.SPC.Geometry;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Cleaver {
    [HideMonoScript]
    [DefaultExecutionOrder(-50)]
    [AddComponentMenu("Cleaver/Frustum Viewer")]
    public class CleaverFrustumViewer : CleaverViewerBase<CleaverFrustumViewer> {

        public LayerMask layerMask;
        public float detailThreshold = FrustumHelper.InverseRemapPerceptionScreenCoverage(0.25f);
        public float screenThreshold = FrustumHelper.InverseRemapPerceptionScreenCoverage(0.10f);
        public float nearDistance = 1f;

        public Camera trackedCamera;

        public Frustum6 frustum;
        public float4x4 worldToProjectionMatrix;

        private ulong _lastHash;


        private void Awake() {
            On<CleaverBatchedViewerRefreshEvt>().Do(OnBatchedRefresh);
            On<CleaverBatchedViewerRaycastEvt>().Do(OnBatchedRaycast);
        }


        public void Update() {
            if (!trackedCamera) return;
            // if (CleaverEnvironment.Instance.version == 0) return;
            // var lastPosition = position;
            // var lastRotation = rotation;
            // position = trackedCamera.transform.position;
            // rotation = trackedCamera.transform.rotation;
            //
            // if (!SpacialHash.PosRot(lastPosition, position, lastRotation, rotation)) {
            //     trackedCamera.CalculateFrustum6(ref frustum);
            //     worldToProjectionMatrix = trackedCamera.CalculateWorldToProjectionMatrix();
            //
            //     RefreshSections();
            // }
            for (var i = 0; i < groupVisibility.Length; i++) { Debug.Log($"Group {i}: {groupVisibility[i]}"); }
        }

        private void OnBatchedRefresh(ref CleaverBatchedViewerRefreshEvt args) {
            if (!isActiveAndEnabled) return;
            var lastPosition = position;
            var lastRotation = rotation;
            position = trackedCamera.transform.position;
            rotation = trackedCamera.transform.rotation;

            _raycastCommands.Clear();
            _raycastProxyIndices.Clear();
            RefitArrays(args.environment);

            if (!SpacialHash.PosRot(lastPosition, position, lastRotation, rotation)) {
                trackedCamera.CalculateFrustum6(ref frustum);
                worldToProjectionMatrix = trackedCamera.CalculateWorldToProjectionMatrix();

                var sectionJob = args.environment.QuerySections(position, (byte)cleaverMask, currentSections);
                args.batch.Add(sectionJob);

                var batchJob = BroadPhase(args.environment);
                args.batch.Add(batchJob);
            }
        }

        private void OnBatchedRaycast(ref CleaverBatchedViewerRaycastEvt args) {
            if (!isActiveAndEnabled) return;
            if (_raycastCommands.Length == 0) return;
            Debug.Log($"Scheduling { _raycastCommands.Length} raycasts");

            var batchHandle = RaycastCommand.ScheduleBatch(
                _raycastCommands.AsArray(),
                _raycastResults,
                32
                //, dependsOn: commandHandle
            );
            var resultJob = new CleaverFrustumRaycastResultJob {
                raycastResults = _raycastResults,
                raycastProxyIndices = _raycastProxyIndices,
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
                queryMask = (byte)cleaverMask,
                groupVisibility = groupVisibility,
                proxyCoverage = proxyCoverage
            };
            var commandJob = new CleaverProxyRaycastCommandJob {
                proxies = env.proxies,
                samplePoints = env.samplePoints,
                proxyCoverage = proxyCoverage,
                viewerPoint = position,
                layerMask = layerMask,
                raycastCommands = _raycastCommands,
                raycastProxyIndices = _raycastProxyIndices,
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