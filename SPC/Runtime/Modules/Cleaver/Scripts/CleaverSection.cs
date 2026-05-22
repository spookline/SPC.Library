using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Spookline.SPC.Cleaver.Points;
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
    public class CleaverSection : SpookBehaviour<CleaverSection>, IPivotRecenter, ICustomBoundsProvider,
        IBoundModificationReceiver {

        [FormerlySerializedAs("region")]
        public CleaverProxyGroup proxyGroup;

        public bool closed;

        [EnumToggleButtons]
        public ByteMask mask = ByteMask.All;

        [HideInInspector]
        public OrientedBox[] volumes = Array.Empty<OrientedBox>();

        [NonSerialized]
        public readonly List<CleaverPortal> portals = new();

        [
            HideInPlayMode,
            PropertySpace(20f),
            PolymorphicDrawerSettings(ShowBaseType = false),
            Searchable,
            ListDrawerSettings(
                ShowIndexLabels = true,
                ListElementLabelName = "TypeName",
                ShowPaging = true,
                NumberOfItemsPerPage = 5,
                ShowFoldout = false
            ),
        ]
        public readonly List<EditablePoint> points = new();

        [
            NonSerialized, ShowInInspector, HideInEditorMode,
            PropertySpace(20f),
            Searchable,
            PolymorphicDrawerSettings(ShowBaseType = false),
        ]
        public List<CleaverPoint> runtimePoints;

        public ulong Id { get; private set; }

        private void Awake() {
            Id = IdGenerator.NextId();
            On<GizmoEvt>().Do(OnGizmos);
            On<CleaverEnvironmentRebuiltEvt>().Do(OnEnvironmentRebuild);
        }

        private void OnEnvironmentRebuild(ref CleaverEnvironmentRebuiltEvt args) {
            // Just make sure we are actually registered
            if (args.Tracks(this)) { RebuildPoints(); } else {
                Debug.LogWarning("Section not tracked by CleaverEnvironment.", this);
            }
        }

        private void RebuildPoints() {
            if (runtimePoints == null) {
                runtimePoints = new List<CleaverPoint>();
                var affine = new AffineTransform(transform.position, transform.rotation, transform.lossyScale);
                foreach (var point in points) {
                    var materialized = point.Instantiate(affine);
                    runtimePoints.Add(materialized);
                }

                foreach (var point in runtimePoints) {
                    try {
                        point.Initialize(this); //
                    } catch (Exception e) {
                        Debug.LogException(e, this); //
                    }
                }
            }

            for (var i = 0; i < runtimePoints.Count; i++) {
                var currentPoint = runtimePoints[i];
                try {
                    currentPoint.Rebuild(this); //
                } catch (Exception e) {
                    Debug.LogException(e, this); //
                }
            }
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            if (runtimePoints != null) {
                foreach (var point in runtimePoints) {
                    try {
                        point.Dispose(); //
                    } catch (Exception e) {
                        Debug.LogException(e, this); //
                    }
                }
            }
        }

        private void OnGizmos(ref GizmoEvt args) {
            // Editor Gizmos
            if (args.DrawingPass(GizmoType.GizmosSelected, out var draw)) {
                var color = Color.cyan;
                var affine = transform.Affine();
                using (draw.Scope(color)) {
                    foreach (var volume in volumes) {
                        var wsBox = affine.Transform(volume);
                        draw.OrientedBox(wsBox);
                    }
                }

                color.a = 0.1f;
                using (draw.Scope(color)) {
                    foreach (var volume in volumes) {
                        var wsBox = affine.Transform(volume);
                        draw.OrientedBox(wsBox, false);
                    }
                }

#if UNITY_EDITOR
                if (points != null) {
                    foreach (var point in points) {
                        if (point == null) continue;
                        if (!string.IsNullOrEmpty(EditablePoint.Filter)) {
                            if (!point.TypeName.Contains(EditablePoint.Filter)) continue;
                        }

                        if (!point.editorHidden) point.DrawEditor(affine, draw);
                    }
                }
#endif

                return;
            }

            // Runtime Gizmos

            if (runtimePoints != null) {
                foreach (var point in runtimePoints) { point.Gizmos(ref args); }
            }

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
            if (!GizmosHelper.IsSelected(gameObject)) return;
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

            var vt = transform.Affine().Inverse();
            volumes = next.Select(x => vt.Transform(x)).ToArray();
        }


#if UNITY_EDITOR
        [NonSerialized]
        private PivotRecenter _pivot;

        [ShowInInspector, HideInPlayMode]
        private PivotRecenter Pivot {
            get {
                if (_pivot.target == null) _pivot = new PivotRecenter(this);
                return _pivot;
            }
            set => _pivot = value;
        }
#endif

        public MinMaxAABB ComputeBounds() {
            if (volumes.Length == 0) return new MinMaxAABB();
            var affine = transform.Affine();
            var aabb = affine.Transform(volumes[0]).AABB();
            for (var i = 1; i < volumes.Length; i++) aabb.Encapsulate(affine.Transform(volumes[i]).AABB());

            return aabb;
        }

        public bool Contains(float3 point, float radius) {
            var affine = transform.Affine();
            foreach (var volume in volumes) {
                var wsBox = affine.Transform(volume);
                if (wsBox.OverlapsSphere(point, radius)) return true;
            }

            return false;
        }

        public void SampleVolumes(NativeArray<CleaverVolumeData> data, int startIndex) {
            var affine = transform.Affine();
            for (var i = 0; i < volumes.Length; i++) {
                var volume = volumes[i];
                var wsBox = affine.Transform(volume);
                data[startIndex + i] = new CleaverVolumeData {
                    query = wsBox
                };
            }
        }

        public Transform GetPivotRootTransform() {
            return transform;
        }

        public OrientedBox GetPivotBounds() {
            return ComputeBounds();
        }

        public void ApplyPivotDeltas(AffineTransform delta) {
            for (var i = 0; i < points.Count; i++) {
                var point = points[i];
                point.position = delta.TransformPoint(point.position);
                if (point is IRotatablePoint rotatable) rotatable.Rotation = delta.Rotate(rotatable.Rotation);
                if (point is IScalablePoint scalable) scalable.Scale = delta.Scale(scalable.Scale);
                if (point is IBoundingPoint bounded) bounded.Extents = delta.Scale(bounded.Extents);
            }

            for (var i = 0; i < volumes.Length; i++) {
                var volume = volumes[i];
                volumes[i] = delta.Transform(volume);
            }
        }

        public OrientedBox GetBounds() {
            return ComputeBounds();
        }

        public MinMaxAABB GetAABB() {
            return ComputeBounds();
        }

        public OrientedBox EncapsulateIn(OrientedBox original) {
            return original.Encapsulate(GetBounds());
        }

        public string BoundsGroup => "Cleaver Sections";

        public void ContributeBoundingBoxes(List<IBoundsContributor> contributors) {
            var affine = transform.Affine();
            foreach (var box in volumes) {
                var wsBox = affine.Transform(box);
                contributors.Add(new CleaverSectionVolume(wsBox));
            }
        }

        public void ReceiveBounds(OrientedBox box) {
            var affine = transform.Affine().Inverse();
            var newVolume = affine.Transform(box);
            volumes = new[] { newVolume };
        }

    }

    public struct CleaverSectionVolume : IBoundsContributor {

        public OrientedBox box;

        public CleaverSectionVolume(OrientedBox box) {
            this.box = box;
        }

        public OrientedBox GetBounds() => box;

        public MinMaxAABB GetAABB() => box.AABB();

        public OrientedBox EncapsulateIn(OrientedBox original) {
            return original.Encapsulate(box);
        }

        public string BoundsGroup => "Cleaver Section Volumes";

    }
}