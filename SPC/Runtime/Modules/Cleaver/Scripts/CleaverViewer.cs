using System;
using System.Text;
using Sirenix.OdinInspector;
using Spookline.SPC.Ext;
using Spookline.SPC.Geometry;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Cleaver {
    [HideMonoScript]
    [DefaultExecutionOrder(-50)]
    [AddComponentMenu("Cleaver/Viewer")]
    public class CleaverViewer : CleaverViewerBase {

        public LayerMask layerMask;
        public float detailThreshold = FrustumHelper.InverseRemapPerceptionScreenCoverage(0.25f);
        public float screenThreshold = FrustumHelper.InverseRemapPerceptionScreenCoverage(0.10f);
        public float nearDistance = 1f;

        public Camera trackedCamera;

        public Frustum6 frustum;
        public float4x4 worldToProjectionMatrix;

        private ulong _lastHash;

        [NonSerialized]
        public NativeArray<ProxyGroupVisibility> groupVisibility;

        [NonSerialized]
        public NativeArray<SectionVisibility> sectionVisibility;

        [NonSerialized]
        public NativeArray<float> proxyCoverage;

        private void Awake() {
            On<CleaverBatchedViewerRefreshEvt>().Do(OnBatchedRefresh);
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
        }

        private void OnBatchedRefresh(ref CleaverBatchedViewerRefreshEvt args) {
            var lastPosition = position;
            var lastRotation = rotation;
            position = trackedCamera.transform.position;
            rotation = trackedCamera.transform.rotation;

            if (!SpacialHash.PosRot(lastPosition, position, lastRotation, rotation)) {
                trackedCamera.CalculateFrustum6(ref frustum);
                worldToProjectionMatrix = trackedCamera.CalculateWorldToProjectionMatrix();

                var job = args.environment.QuerySections(position, (byte)cleaverMask, currentSections);
                args.batch.Add(job);
            }
        }

        [Button]
        public void RefineProxyVisibilityWithRaycasts() {
            var env = CleaverEnvironment.Instance;
            if (env == null || env.proxyGroups.Length == 0) {
                Debug.LogWarning("CleaverEnvironment not available or no proxy groups");
                return;
            }

            if (!groupVisibility.IsCreated || groupVisibility.Length != env.proxyGroups.Length) {
                if (groupVisibility.IsCreated) groupVisibility.Dispose();
                groupVisibility = new NativeArray<ProxyGroupVisibility>(env.proxyGroups.Length, Allocator.Persistent);
            } else {
                for (var i = 0; i < groupVisibility.Length; i++) { groupVisibility[i] = ProxyGroupVisibility.None; }
            }

            if (!proxyCoverage.IsCreated || proxyCoverage.Length != env.proxies.Length) {
                if (proxyCoverage.IsCreated) proxyCoverage.Dispose();
                proxyCoverage = new NativeArray<float>(env.proxies.Length, Allocator.Persistent);
            }

            var raycastCommands = new NativeList<RaycastCommand>(Allocator.TempJob);
            var raycastProxyIndices = new NativeList<int>(Allocator.TempJob);

            try {
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

                var broadPhaseHandle = broadPhaseJob.Schedule(env.proxyGroups.Length, 16);
                broadPhaseHandle.Complete();

                var commandJob = new CleaverFrustumProxyRaycastCommandJob {
                    proxies = env.proxies,
                    samplePoints = env.samplePoints,
                    proxyCoverage = proxyCoverage,
                    viewerPoint = position,
                    layerMask = layerMask,
                    raycastCommands = raycastCommands,
                    raycastProxyIndices = raycastProxyIndices
                };

                var commandHandle = commandJob.Schedule();
                commandHandle.Complete();

                if (raycastCommands.Length == 0) {
                    Debug.Log("Broad phase complete, no raycasts needed.");
                    return;
                }

                var raycastResults = new NativeArray<RaycastHit>(raycastCommands.Length, Allocator.TempJob);
                try {
                    var batchHandle = RaycastCommand.ScheduleBatch(raycastCommands.AsArray(), raycastResults, 32);
                    batchHandle.Complete();

                    var resultJob = new CleaverFrustumProxyRaycastResultJob {
                        raycastResults = raycastResults,
                        raycastProxyIndices = raycastProxyIndices.AsArray(),
                        proxyGroups = env.proxyGroups,
                        proxies = env.proxies,
                        groupVisibility = groupVisibility
                    };

                    var resultHandle = resultJob.Schedule();
                    resultHandle.Complete();

                    Debug.Log(
                        $"Raycast refinement complete: {raycastCommands.Length} raycasts executed, {raycastResults.Length} results processed."
                    );

                    // Print group results
                    for (var i = 0; i < groupVisibility.Length; i++) { Debug.Log($"Group {i}: {groupVisibility[i]}"); }
                } finally { raycastResults.Dispose(); }
            } finally {
                raycastCommands.Dispose();
                raycastProxyIndices.Dispose();
            }
        }

        protected override void OnDisable() {
            base.OnDisable();
            if (proxyCoverage.IsCreated) proxyCoverage.Dispose();
            if (groupVisibility.IsCreated) groupVisibility.Dispose();
            if (sectionVisibility.IsCreated) sectionVisibility.Dispose();
        }

    }

    public abstract class CleaverViewerBase : SpookBehaviour<CleaverViewerBase> {

        [EnumToggleButtons]
        public ByteMask cleaverMask = ByteMask.All;

        public float3 position;
        public quaternion rotation;

        [NonSerialized]
        public NativeHashSet<ulong> currentSections;

        [ShowInInspector]
        public string DebugSections {
            get {
                if (!currentSections.IsCreated) return "Not Created";

                var builder = new StringBuilder();
                using var enumerator = currentSections.GetEnumerator();
                while (enumerator.MoveNext()) {
                    var sectionId = enumerator.Current;
                    builder.Append(sectionId);
                    builder.Append(", ");
                }

                return builder.ToString();
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            currentSections = new NativeHashSet<ulong>(8, Allocator.Persistent);
        }

        protected override void OnDisable() {
            base.OnDisable();
            currentSections.Dispose();
        }

        public void RefreshSections() {
            CleaverEnvironment.Instance.QuerySectionsImmediate(position, (byte)cleaverMask, currentSections);
        }

    }
}