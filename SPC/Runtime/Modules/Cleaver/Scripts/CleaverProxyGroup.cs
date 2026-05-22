using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Spookline.SPC.Common;
using Spookline.SPC.Debugging;
using Spookline.SPC.Draw;
using Spookline.SPC.Ext;
using Spookline.SPC.Geometry;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;

namespace Spookline.SPC.Cleaver {
    [HideMonoScript]
    [ExecuteInEditMode]
    [AddComponentMenu("Cleaver/Proxy Group")]
    public class CleaverProxyGroup : SpookBehaviour<CleaverProxyGroup>, IBoundsReceiver {

        public CleaverProxyGroup parent;

        [EnumToggleButtons]
        public ByteMask mask = ByteMask.All;

        [LabelText("Bounds")]
        [EnumToggleButtons]
        [ValidateInput(nameof(ValidateBoundsType))]
        public CleaverRegionBoundsType boundsType = CleaverRegionBoundsType.Proxies;

        [ShowIf(nameof(boundsType), CleaverRegionBoundsType.Renderers)]
        public Renderer[] renderers;

        [ShowIf(nameof(boundsType), CleaverRegionBoundsType.Fixed)]
        public Vector3 boundsSize = Vector3.one;
        public bool updateProxiesOnDirty = true;

        [NonSerialized]
        private CleaverProxy[] _proxies;

        public ulong Id { get; private set; }

        public bool Dirty { get; private set; }

        private void Awake() {
            _proxies = GetComponents<CleaverProxy>();

            Id = IdGenerator.NextId();
            Dirty = true;
            On<CleaverCheckForUpdateEvt>().Do(OnCheckForUpdate);
            On<GizmoEvt>().Do(OnGizmos);
        }

        private void OnGizmos(ref GizmoEvt args) {
            if (!args.HasFlag("cleaver_proxies")) return;
            var env = CleaverEnvironment.Instance;
            if (!env.groupLookup.TryGetValue(Id, out var idx)) return;
            if (!env.proxyGroups.TryGetIndex(idx, out var group)) return;
            if (args.Cull(group.bounds)) return;

            if (args.DrawingPass(out var draw)) {
                var magenta = Color.magenta;
                using (draw.Scope(magenta)) draw.OrientedBox(group.bounds);
                magenta.a = 0.33f;
                using (draw.Scope(magenta))
                    for (var i = 0; i < group.proxyCount; i++) {
                        var id = group.proxyIndex + i;
                        var proxyData = env.proxies[id];
                        draw.OrientedBox(proxyData.query, false);
                    }
            }

            if (args.WorldOverlayPass(out var world)) {
                var count = 0;
                for (var i = 0; i < group.proxyCount; i++) {
                    var id = group.proxyIndex + i;
                    var proxyData = env.proxies[id];
                    count += proxyData.pointCount;
                }

                world.Box(Id, "ProxyGroup", group.bounds.Center, idx.FormatNonAlloc())
                    .Field("Bounds", group.bounds.Extents, color: Color.magenta)
                    .Field("Proxies", group.proxyCount, color: Color.magenta)
                    .Field("Points", count, color: Color.orange);
            }
        }

        [Button]
        [HideInEditorMode]
        public void SetDirty() {
            Dirty = true;
        }

        private void OnCheckForUpdate(ref CleaverCheckForUpdateEvt args) {
            if (!Dirty) return;

            args.UpdateProxyGroup(this);
            if (updateProxiesOnDirty)
                foreach (var proxy in GetProxies())
                    args.UpdateProxy(proxy);

            Dirty = false;
        }

        public int GetProxyCount() {
            return GetProxies().Length;
        }

        public int GetProxySamplePointCount() {
            var count = 0;
            foreach (var proxy in GetProxies()) count += proxy.GetPointCount();
            return count;
        }

        public CleaverProxy[] GetProxies() {
            return _proxies ??= GetComponents<CleaverProxy>();
        }

        public MinMaxAABB ComputeBounds() {
            var aabb = new MinMaxAABB(float3.zero, float3.zero);
            switch (boundsType) {
                case CleaverRegionBoundsType.Global:
                    return MinMaxAABB.CreateFromCenterAndExtents(float3.zero, float.MaxValue);
                case CleaverRegionBoundsType.Proxies:
                    if (_proxies == null || _proxies.Length == 0) return aabb;
                    var affine = transform.Affine();
                    aabb = affine.Transform(_proxies[0].box).AABB();
                    for (var i = 1; i < _proxies.Length; i++)
                        aabb.Encapsulate(affine.Transform(_proxies[i].box).AABB());
                    return aabb;
                case CleaverRegionBoundsType.Fixed:
                    return MinMaxAABB.CreateFromCenterAndExtents(transform.position, boundsSize);
                case CleaverRegionBoundsType.Renderers:
                    if (renderers == null || renderers.Length == 0) return aabb;

                    aabb = renderers[0].bounds.ToMinMaxAABB();
                    for (var i = 1; i < renderers.Length; i++) aabb.Encapsulate(renderers[i].bounds.ToMinMaxAABB());

                    return aabb;

                default:
                    throw new NotImplementedException($"Bounds type {boundsType} not implemented");
            }
        }

        public bool ValidateBoundsType(
            CleaverRegionBoundsType type,
            ref string errorMessage,
            ref InfoMessageType? messageType
        ) {
            if (type == CleaverRegionBoundsType.Proxies && GetProxyCount() == 0) {
                errorMessage = "No proxies in region";
                messageType = InfoMessageType.Warning;
                return false;
            }

            if (type == CleaverRegionBoundsType.Renderers && (renderers == null || renderers.Length == 0)) {
                errorMessage = "No renderers in region";
                messageType = InfoMessageType.Warning;
                return false;
            }

            return true;
        }

        public void ReceiveBounds(OrientedBox box) {
            var self = transform.ToOrientedBox();
            self = self.Encapsulate(box);
            boundsSize = self.Size;
        }

    }

    public enum CleaverRegionBoundsType {

        Proxies,
        Global,
        Fixed,
        Renderers

    }
}