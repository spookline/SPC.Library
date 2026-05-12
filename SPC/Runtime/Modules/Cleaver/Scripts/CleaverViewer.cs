using System;
using System.Text;
using Sirenix.OdinInspector;
using Spookline.SPC.Ext;
using Spookline.SPC.Geometry;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Cleaver {
    [HideMonoScript]
    [DefaultExecutionOrder(-100)]
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
        public NativeArray<ProxyGroupVisibility> regionVisibility;

        [NonSerialized]
        public NativeArray<SectionVisibility> sectionVisibility;

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