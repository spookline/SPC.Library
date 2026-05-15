using System;
using Sirenix.OdinInspector;
using Spookline.SPC.Common;
using Spookline.SPC.Ext;
using Spookline.SPC.Geometry;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;

namespace Spookline.SPC.Cleaver {
    [HideMonoScript]
    [ExecuteInEditMode]
    [AddComponentMenu("Cleaver/Proxy Group")]
    public class CleaverProxyGroup : SpookBehaviour<CleaverProxyGroup> {

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
                    var virtualTransform = VirtualTransform.From(transform);
                    aabb = virtualTransform.Transform(_proxies[0].box).AABB();
                    for (var i = 1; i < _proxies.Length; i++)
                        aabb.Encapsulate(virtualTransform.Transform(_proxies[i].box).AABB());
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

    }

    public enum CleaverRegionBoundsType {

        Proxies,
        Global,
        Fixed,
        Renderers

    }
}