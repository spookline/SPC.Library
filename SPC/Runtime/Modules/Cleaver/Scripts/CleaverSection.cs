using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Spookline.SPC.Common;
using Spookline.SPC.Ext;
using Spookline.SPC.Geometry;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.Serialization;

namespace Spookline.SPC.Cleaver {
    [HideMonoScript]
    [ExecuteInEditMode]
    public class CleaverSection : SpookBehaviour<CleaverSection> {

        [FormerlySerializedAs("region")]
        public CleaverProxyGroup proxyGroup;

        public bool closed;

        [EnumToggleButtons]
        public ByteMask mask = ByteMask.All;

        [HideInInspector]
        public OrientedBox[] volumes = Array.Empty<OrientedBox>();

        [NonSerialized]
        public readonly List<CleaverPortal> portals = new();

        public ulong Id { get; private set; }

        private void Awake() {
            Id = IdGenerator.NextId();
        }

        private void OnDrawGizmosSelected() {
            var color = Color.cyan;
            color.a = 0.1f;
            Gizmos.color = color;
            var virtualTransform = VirtualTransform.From(transform);
            foreach (var volume in volumes) {
                var wsBox = virtualTransform.Transform(volume);
                wsBox.DrawGizmos(false);
            }

            Gizmos.color = Color.cyan;
            foreach (var volume in volumes) {
                var wsBox = virtualTransform.Transform(volume);
                wsBox.DrawGizmos();
            }
        }

        public void LoadVolumesFromMeshes() {
            var boxes = GetComponentsInChildren<Renderer>()
                .Select(x => x.ToOrientedBox())
                .Where(x => math.length(x.Size) > 0.1f)
                .OrderByDescending(x => math.length(x.Size))
                .ToList();

            var next = new List<OrientedBox>();
            foreach (var box in boxes) {
                if (next.Any(x => x.Contains(box))) continue;
                next.Add(box);
            }

            var vt = VirtualTransform.From(transform);
            volumes = next.Select(vt.InverseTransform).ToArray();
        }

        public MinMaxAABB ComputeBounds() {
            if (volumes.Length == 0) return new MinMaxAABB();
            var virtualTransform = VirtualTransform.From(transform);
            var aabb = virtualTransform.Transform(volumes[0]).AABB();
            for (var i = 1; i < volumes.Length; i++) aabb.Encapsulate(virtualTransform.Transform(volumes[i]).AABB());

            return aabb;
        }

        public bool Contains(float3 point, float radius) {
            var virtualTransform = VirtualTransform.From(transform);
            foreach (var volume in volumes) {
                var wsBox = virtualTransform.Transform(volume);
                if (wsBox.OverlapsSphere(point, radius)) return true;
            }

            return false;
        }

        public void SampleVolumes(NativeArray<CleaverVolumeData> data, int startIndex) {
            var virtualTransform = VirtualTransform.From(transform);
            for (var i = 0; i < volumes.Length; i++) {
                var volume = volumes[i];
                var wsBox = virtualTransform.Transform(volume);
                data[startIndex + i] = new CleaverVolumeData {
                    query = wsBox
                };
            }
        }

    }
}