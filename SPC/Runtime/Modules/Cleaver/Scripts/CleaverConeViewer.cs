using Sirenix.OdinInspector;
using Spookline.SPC.Debugging;
using Spookline.SPC.Draw;
using Spookline.SPC.Geometry;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Spookline.SPC.Cleaver {
    [HideMonoScript]
    [DefaultExecutionOrder(-50)]
    [AddComponentMenu("Cleaver/Cone Viewer")]
    public class CleaverConeViewer : CleaverViewerBase<CleaverConeViewer> {

        public float detailThreshold = FrustumHelper.InverseRemapPerceptionScreenCoverage(0.25f);
        public float screenThreshold = FrustumHelper.InverseRemapPerceptionScreenCoverage(0.10f);
        public float nearDistance = 1f;

        public Transform eyeTransform;
        public float viewDistance;
        public float viewAngleDegrees;

        public SphereSectionQuery viewCone;

        private ulong _lastHash;


        protected override void Awake() {
            base.Awake();
            On<CleaverBatchedViewerRefreshEvt>().Do(OnBatchedRefresh);
            On<CleaverBatchedViewerRaycastEvt>().Do(OnBatchedRaycast);
            On<GizmoEvt>().Do(OnGizmos);
        }

        protected override void RefreshTransforms() {
            if (!eyeTransform) return;
            position = eyeTransform.position;
            rotation = eyeTransform.rotation;
        }

        private void OnGizmos(ref GizmoEvt args) {
            var draw = args.drawer;
            using (draw.Scope(Color.red, Matrix4x4.TRS(position, rotation, Vector3.one))) {
                draw.SphereSection(Vector3.zero, Vector3.forward, viewDistance, viewAngleDegrees);
            }
        }

        public void Update() {
            if (!eyeTransform) return;
            // for (var i = 0; i < groupVisibility.Length; i++) { Debug.Log($"Group {i}: {groupVisibility[i]}"); }
        }

        private void OnBatchedRefresh(ref CleaverBatchedViewerRefreshEvt args) {
            if (!isActiveAndEnabled) return;
            if (!eyeTransform) return;
            var lastPosition = position;
            var lastRotation = rotation;
            position = eyeTransform.position;
            rotation = eyeTransform.rotation;

            raycastCommands.Clear();
            raycastProxyIndices.Clear();
            RefitArrays(args.environment);

            if (!SpacialHash.PosRot(lastPosition, position, lastRotation, rotation)) {
                viewCone = SphereSectionQuery.FromDegrees(position, transform.forward, viewAngleDegrees, viewDistance);

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
            var resultJob = new CleaverSphereSectionRaycastResultJob {
                raycastResults = raycastResults,
                raycastProxyIndices = raycastProxyIndices,
                raycastCommands = raycastCommands,
                proxyGroups = args.environment.proxyGroups,
                proxies = args.environment.proxies,
                groupVisibility = groupVisibility,
                query = viewCone,

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

            var broadPhaseJob = new CleaverSphereSectionProxyBroadPhaseJob() {
                proxyGroups = env.proxyGroups,
                proxies = env.proxies,
                query = viewCone,
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