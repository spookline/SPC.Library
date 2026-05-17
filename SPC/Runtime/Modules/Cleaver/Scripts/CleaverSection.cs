using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Spookline.SPC.Common;
using Spookline.SPC.Debugging;
using Spookline.SPC.Draw;
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
    [AddComponentMenu("Cleaver/Section")]
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
            On<GizmoEvt>().Do(OnGizmos);
        }

        private void OnGizmos(ref GizmoEvt args) {
            // Editor Gizmos
            if (args.DrawingPass(GizmoType.GizmosSelected, out var draw)) {
                var color = Color.cyan;
                var virtualTransform = VirtualTransform.From(transform);
                using (draw.Scope(color)) {
                    foreach (var volume in volumes) {
                        var wsBox = virtualTransform.Transform(volume);
                        draw.OrientedBox(wsBox);
                    }
                }

                color.a = 0.1f;
                using (draw.Scope(color)) {
                    foreach (var volume in volumes) {
                        var wsBox = virtualTransform.Transform(volume);
                        draw.OrientedBox(wsBox, false);
                    }
                }
                return;
            }

            // Runtime Gizmos
            if (!args.HasFlag("cleaver_sections")) return;
            var env = CleaverEnvironment.Instance;
            if (!env.sectionLookup.TryGetValue(Id, out var idx)) return;
            if (!env.sections.TryGetIndex(idx, out var section)) return;
            if (args.Cull(section.bounds)) return;

            if (args.DrawingPass(out draw)) {
                var color = Color.cyan;
                using (draw.Scope(color)) draw.OrientedBox(section.bounds);

                color.a = 0.2f;
                using (draw.Scope(color))
                    for (int i = 0; i < section.volumeCount; i++) {
                        if (!env.volumes.TryGetIndex(section.volumeIndex + i, out var volume)) continue;
                        draw.OrientedBox(volume.query, false);
                    }
            }

            if (args.WorldOverlayPass(out var world)) {
                world.Box(Id, "Section", section.bounds.Center, idx.FormatNonAlloc())
                    .Field("Bounds", section.bounds.Extents, color: Color.cyan)
                    .Field("Volumes", section.volumeCount, color: Color.cyan);
            }
        }

        private void OnDrawGizmosSelected() {
            var evt = GizmoEvt.EditorGizmosSelected;
            OnGizmos(ref evt);
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