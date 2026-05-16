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

        [NonSerialized]
        private NativeArray<RaycastHit> _raycastResults;

        [NonSerialized]
        private NativeList<RaycastCommand> _raycastCommands;

        [NonSerialized]
        private NativeList<int> _raycastProxyIndices;

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
            if (_raycastCommands.Length == 0) return;
            Debug.Log($"Scheduling { _raycastCommands.Length} raycasts");

            var batchHandle = RaycastCommand.ScheduleBatch(
                _raycastCommands.AsArray(),
                _raycastResults,
                32
                //, dependsOn: commandHandle
            );
            var resultJob = new CleaverProxyRaycastResultJob {
                raycastResults = _raycastResults,
                raycastProxyIndices = _raycastProxyIndices,
                proxyGroups = args.environment.proxyGroups,
                proxies = args.environment.proxies,
                groupVisibility = groupVisibility
            };
            var resultHandle = resultJob.Schedule(batchHandle);
            args.batch.Add(resultHandle);
        }

        private void RefitArrays(CleaverEnvironment env) {
            if (!groupVisibility.IsCreated || groupVisibility.Length != env.proxyGroups.Length) {
                if (groupVisibility.IsCreated) groupVisibility.Dispose();
                groupVisibility = new NativeArray<ProxyGroupVisibility>(env.proxyGroups.Length, Allocator.Persistent);
            }

            if (!proxyCoverage.IsCreated || proxyCoverage.Length != env.proxies.Length) {
                if (proxyCoverage.IsCreated) proxyCoverage.Dispose();
                proxyCoverage = new NativeArray<float>(env.proxies.Length, Allocator.Persistent);
            }

            if (!_raycastResults.IsCreated || _raycastResults.Length != env.samplePoints.Length) {
                if (_raycastResults.IsCreated) _raycastResults.Dispose();
                _raycastResults = new NativeArray<RaycastHit>(env.samplePoints.Length, Allocator.Persistent);
            }
        }

        private JobHandle BroadPhase(CleaverEnvironment env) {
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

            var broadPhaseHandle = broadPhaseJob.Schedule(env.proxyGroups.Length, 128);

            var commandJob = new CleaverProxyRaycastCommandJob {
                proxies = env.proxies,
                samplePoints = env.samplePoints,
                proxyCoverage = proxyCoverage,
                viewerPoint = position,
                layerMask = layerMask,
                raycastCommands = _raycastCommands,
                raycastProxyIndices = _raycastProxyIndices,
            };

            return commandJob.Schedule(broadPhaseHandle);
        }

        protected override void OnEnable() {
            base.OnEnable();
            _raycastCommands = new NativeList<RaycastCommand>(Allocator.Persistent);
            _raycastProxyIndices = new NativeList<int>(Allocator.Persistent);
        }

        protected override void OnDisable() {
            base.OnDisable();
            if (proxyCoverage.IsCreated) proxyCoverage.Dispose();
            if (groupVisibility.IsCreated) groupVisibility.Dispose();
            if (sectionVisibility.IsCreated) sectionVisibility.Dispose();
            if (_raycastResults.IsCreated) _raycastResults.Dispose();
            if (_raycastCommands.IsCreated) _raycastCommands.Dispose();
            if (_raycastProxyIndices.IsCreated) _raycastProxyIndices.Dispose();
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