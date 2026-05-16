using System;
using System.Text;
using Sirenix.OdinInspector;
using Spookline.SPC.Common;
using Spookline.SPC.Draw;
using Spookline.SPC.Ext;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Spookline.SPC.Cleaver {
    public abstract class CleaverViewerBase<T> : SpookBehaviour<T> where T : CleaverViewerBase<T> {

        [EnumToggleButtons]
        public ByteMask visibilityMask = ByteMask.All;

        [FormerlySerializedAs("collisionMask")]
        [FormerlySerializedAs("cleaverMask")]
        [EnumToggleButtons]
        public ByteMask occlusionMask = ByteMask.World;


        public float3 position;
        public quaternion rotation;

        [NonSerialized]
        public NativeHashSet<ulong> currentSections;

        [NonSerialized]
        public NativeArray<ProxyGroupVisibility> groupVisibility;

        [NonSerialized]
        public NativeArray<SectionVisibility> sectionVisibility;

        [NonSerialized]
        public NativeArray<float> proxyCoverage;


        // Internal variables for implementors
        [NonSerialized]
        protected NativeArray<RaycastHit> raycastResults;
        [NonSerialized]
        protected NativeList<RaycastCommand> raycastCommands;
        [NonSerialized]
        protected NativeList<int> raycastProxyIndices;
        [NonSerialized]
        protected NativeHashSet<int> queriedProxies;
        [NonSerialized]
        protected NativeHashSet<int> visibleProxies;
        [NonSerialized]
        protected NativeHashSet<int> indirectVisibleProxies;

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

        protected virtual void Awake() {
            On<DebugDrawEvt>().Do(OnDebugDrawViewerBase);
        }

        private void OnDebugDrawViewerBase(ref DebugDrawEvt args) {
            if (!isActiveAndEnabled) return;
            if (!args.HasFlag("cleaver_viewers")) return;
            var draw = args.drawer;
            var env = CleaverEnvironment.Instance;
            for (var i = 0; i < env.proxyGroups.Length; i++) {
                var proxyGroup = env.proxyGroups[i];
                if (!groupVisibility.TryGetIndex(i, out var group)) return;
                using (draw.Scope(group.ToDebugColor()))
                    draw.Line(proxyGroup.bounds.Center, position + new float3(0, -0.5f, 0));
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            currentSections = new NativeHashSet<ulong>(8, Allocator.Persistent);
            raycastCommands = new NativeList<RaycastCommand>(Allocator.Persistent);
            raycastProxyIndices = new NativeList<int>(Allocator.Persistent);
            queriedProxies = new NativeHashSet<int>(8, Allocator.Persistent);
            visibleProxies = new NativeHashSet<int>(8, Allocator.Persistent);
            indirectVisibleProxies = new NativeHashSet<int>(8, Allocator.Persistent);
        }

        protected override void OnDisable() {
            base.OnDisable();
            if (currentSections.IsCreated) currentSections.Dispose();
            if (proxyCoverage.IsCreated) proxyCoverage.Dispose();
            if (groupVisibility.IsCreated) groupVisibility.Dispose();
            if (sectionVisibility.IsCreated) sectionVisibility.Dispose();
            if (raycastResults.IsCreated) raycastResults.Dispose();
            if (raycastCommands.IsCreated) raycastCommands.Dispose();
            if (raycastProxyIndices.IsCreated) raycastProxyIndices.Dispose();
            if (queriedProxies.IsCreated) queriedProxies.Dispose();
            if (visibleProxies.IsCreated) visibleProxies.Dispose();
            if (indirectVisibleProxies.IsCreated) indirectVisibleProxies.Dispose();
        }

        protected void RefitArrays(CleaverEnvironment env) {
            if (!groupVisibility.IsCreated || groupVisibility.Length != env.proxyGroups.Length) {
                if (groupVisibility.IsCreated) groupVisibility.Dispose();
                groupVisibility = new NativeArray<ProxyGroupVisibility>(env.proxyGroups.Length, Allocator.Persistent);
            }

            if (!proxyCoverage.IsCreated || proxyCoverage.Length != env.proxies.Length) {
                if (proxyCoverage.IsCreated) proxyCoverage.Dispose();
                proxyCoverage = new NativeArray<float>(env.proxies.Length, Allocator.Persistent);
            }

            if (!raycastResults.IsCreated || raycastResults.Length != env.samplePoints.Length) {
                if (raycastResults.IsCreated) raycastResults.Dispose();
                raycastResults = new NativeArray<RaycastHit>(env.samplePoints.Length, Allocator.Persistent);
            }
        }

        public void RefreshSections() {
            CleaverEnvironment.Instance.QuerySectionsImmediate(position, (byte)occlusionMask, currentSections);
        }

    }
}