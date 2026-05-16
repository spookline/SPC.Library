using System;
using System.Text;
using Sirenix.OdinInspector;
using Spookline.SPC.Ext;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Cleaver {
    public abstract class CleaverViewerBase<T> : SpookBehaviour<T> where T : CleaverViewerBase<T> {

        [EnumToggleButtons]
        public ByteMask cleaverMask = ByteMask.All;

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

        [NonSerialized]
        protected NativeArray<RaycastHit> _raycastResults;

        [NonSerialized]
        protected NativeList<RaycastCommand> _raycastCommands;

        [NonSerialized]
        protected NativeList<int> _raycastProxyIndices;

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

        protected override void OnEnable() {
            base.OnEnable();
            currentSections = new NativeHashSet<ulong>(8, Allocator.Persistent);
            _raycastCommands = new NativeList<RaycastCommand>(Allocator.Persistent);
            _raycastProxyIndices = new NativeList<int>(Allocator.Persistent);
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
            if (_raycastResults.IsCreated) _raycastResults.Dispose();
            if (_raycastCommands.IsCreated) _raycastCommands.Dispose();
            if (_raycastProxyIndices.IsCreated) _raycastProxyIndices.Dispose();
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

            if (!_raycastResults.IsCreated || _raycastResults.Length != env.samplePoints.Length) {
                if (_raycastResults.IsCreated) _raycastResults.Dispose();
                _raycastResults = new NativeArray<RaycastHit>(env.samplePoints.Length, Allocator.Persistent);
            }
        }

        public void RefreshSections() {
            CleaverEnvironment.Instance.QuerySectionsImmediate(position, (byte)cleaverMask, currentSections);
        }

    }
}