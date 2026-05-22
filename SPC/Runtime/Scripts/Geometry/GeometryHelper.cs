using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sirenix.OdinInspector;
using Spookline.SPC.Draw;
using Spookline.SPC.Ext;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Geometry {
    [HideMonoScript]
    [AddComponentMenu("Miscellaneous/Geometry Helper")]
    [ExecuteAlways]
    public class GeometryHelper : SerializedMonoBehaviour, IPivotRecenter, IHideScriptSceneIcon {

#if UNITY_EDITOR
        [NonSerialized]
        private PivotRecenter _pivot;

        [InfoBox("Using the previously computed bounds for alignment", visibleIfMemberName: "@_bounds.hasBounds")]
        [ShowInInspector, HideInPlayMode, TabGroup("Pivot")]
        private PivotRecenter Pivot {
            get {
                if (_pivot.target == null) _pivot = new PivotRecenter(this);
                return _pivot;
            }
            set => _pivot = value;
        }

        [NonSerialized]
        private BoundsModification _bounds;

        [ShowInInspector, HideInPlayMode, TabGroup("Bounds")]
        private BoundsModification Bounds {
            get {
                if (_bounds.transform == null) _bounds = new BoundsModification(transform);
                return _bounds;
            }
            set => _bounds = value;
        }

        private void OnDrawGizmos() {
            if (_bounds.hasBounds) {
                var draw = Drawing.Gizmos;
                var magenta = Color.white;
                magenta.a = 0.8f;
                using (draw.Scope(magenta)) draw.OrientedBox(_bounds.cachedBounds, false);
            }
        }


#endif

        public Transform GetPivotRootTransform() {
            return transform;
        }

        public OrientedBox GetPivotBounds() {
#if UNITY_EDITOR
            if (_bounds.hasBounds) return _bounds.cachedBounds;
#endif
            var children = BoundsHelper.Collect(transform);
            return BoundsHelper.ComputeFor(transform, children);
        }

        public void ApplyPivotDeltas(AffineTransform delta) {
            foreach (var child in GetComponents<IPivotDependent>()) {
                if (child is GeometryHelper) continue;
                child.ApplyPivotDeltas(delta);
            }
        }

        public static void Recenter<T>(T pivot) where T : IPivotRecenter {
            var bounds = pivot.GetPivotBounds();
            var transform = pivot.GetPivotRootTransform();
            var target = transform.Decompose();
            target.position = bounds.center;
            Repivot(pivot, target);
        }

        public static void RecenterGroundAligned<T>(T pivot) where T : IPivotRecenter {
            var bounds = pivot.GetPivotBounds();
            var transform = pivot.GetPivotRootTransform();
            var target = transform.Decompose();
            target.position = bounds.LocalGroundCenter;
            Repivot(pivot, target);
        }

        public static void RecenterZForward<T>(T pivot) where T : IPivotRecenter {
            RecenterNormalized(pivot, new float3(0f, 0f, -1f));
        }

        public static void RecenterProportional<T>(T pivot, float3 scale) where T : IPivotRecenter {
            var bounds = pivot.GetPivotBounds();
            var transform = pivot.GetPivotRootTransform();

            var local = math.lerp(-bounds.halfExtent, bounds.halfExtent, scale);
            var newCenter = bounds.TransformPoint(local);

            var target = transform.Decompose();
            target.position = newCenter;
            Repivot(pivot, target);
        }

        public static void RecenterNormalized<T>(T pivot, float3 scale) where T : IPivotRecenter {
            var bounds = pivot.GetPivotBounds();
            var transform = pivot.GetPivotRootTransform();
            var newCenter = bounds.TransformPoint(bounds.halfExtent * scale);
            var target = transform.Decompose();
            target.position = newCenter;
            Repivot(pivot, target);
        }

        public static void RemoveRotation<T>(T pivot) where T : IPivotRecenter {
            var transform = pivot.GetPivotRootTransform();
            var target = transform.Decompose();
            target.rotation = Quaternion.identity;
            Repivot(pivot, target);
        }

        public static void RemoveScale<T>(T pivot) where T : IPivotRecenter {
            var transform = pivot.GetPivotRootTransform();
            var target = transform.Decompose();
            target.scale = Vector3.one;
            Repivot(pivot, target);
        }

        public static void RemovePosition<T>(T pivot) where T : IPivotRecenter {
            var transform = pivot.GetPivotRootTransform();
            var target = transform.Decompose();
            target.position = Vector3.zero;
            Repivot(pivot, target);
        }

        public static void Repivot<T>(T pivot, AffineTransform target, bool local = false) where T : IPivotRecenter {
            var transform = pivot.GetPivotRootTransform();
            var current = transform.Affine();

            if (local) target = current.Transform(target);

            var children = new Transform[transform.childCount];
            var positions = new AffineTransform[children.Length];
            for (var i = 0; i < children.Length; i++) {
                var child = transform.GetChild(i);
                children[i] = child;
                positions[i] = child.Affine();
            }

            var deltas = Transforms.Deltas(current, target).Inverse();
            target.Apply(transform);

            pivot.ApplyPivotDeltas(deltas);

            for (var i = 0; i < children.Length; i++) {
                var child = children[i];
                positions[i].Apply(child);
            }
        }

        public static void MovePivot<T>(T pivot, AffineTransform by, bool local = false) where T : IPivotRecenter {
            var transform = pivot.GetPivotRootTransform();
            var current = transform.Affine();
            var target = local ? current.Transform(by) : by.Transform(current);
            Repivot(pivot, target);
        }

        public static void MoveData<T>(T pivot, AffineTransform by, bool local = false) where T : IPivotRecenter {
            var transform = pivot.GetPivotRootTransform();
            var current = transform.Affine();
            var target = local ? current.Transform(by) : by.Transform(current);
            var deltas = Transforms.Deltas(current, target);
            pivot.ApplyPivotDeltas(deltas);
        }

        [Serializable, InlineProperty, HideLabel]
        private struct BoundsModification {

            [NonSerialized]
            public Transform transform;


            [LabelText("Filter Bounds Contributing Groups")]
            [ListDrawerSettings(
                HideRemoveButton = true,
                HideAddButton = true,
                ShowFoldout = false,
                DraggableItems = false,
                ElementColor = nameof(ElementColor)
            )]
            [HideIf("@groups == null || groups.Count == 0")]
            [OnStateUpdate(nameof(UpdateBounds))]
            [ShowInInspector, NonSerialized]
            public List<GroupFilter> groups;

            [NonSerialized, ShowInInspector, ShowIf("@hasBounds"), EnumToggleButtons, HideLabel]
            public BoundsType boundsType;

            [NonSerialized]
            private List<IBoundsContributor> _boundsContributors;

            [NonSerialized]
            public OrientedBox cachedBounds;

            [NonSerialized]
            public bool hasBounds;

            [HorizontalGroup("Data"), ShowInInspector, ShowIf("@hasBounds"), NonSerialized]
            public OrientedBoxDisplay bounds;

            public BoundsModification(Transform transform) : this() {
                this.transform = transform;
                receiver ??= transform.GetComponent<IBoundModificationReceiver>();
            }

            [Button("Compute", ButtonSizes.Medium, Icon = SdfIconType.ArrowRepeat), HideIf("@hasBounds")]
            public void CollectBounds() {
                _boundsContributors = BoundsHelper.Collect(transform);
                groups = _boundsContributors.Select(x => x.BoundsGroup).Distinct().Select(x => new GroupFilter {
                        include = GroupFilter.DefaultIncludes.Contains(x),
                        name = x
                    }
                ).ToList();
                hasBounds = true;
                boundsType = BoundsType.OrientedBox;
                UpdateBounds();
            }


            [TitleGroup("Actions"), ShowIf("@hasBounds"), HideLabel]
            public IBoundModificationReceiver receiver;

            [ButtonGroup("Actions/Buttons"), ShowIf("@hasBounds"),
             Button("Send", ButtonSizes.Medium, Icon = SdfIconType.Send)]
            public void SendTo() => receiver.ReceiveBounds(cachedBounds);

            [ButtonGroup("Actions/Buttons"), ShowIf("@hasBounds"), Button("Clear", Icon = SdfIconType.Trash)]
            public void ClearBounds() {
                _boundsContributors = null;
                groups = null;
                hasBounds = false;
            }


            private void UpdateBounds() {
                if (!hasBounds) return;
                var includes = groups.Where(x => x.include).Select(x => x.name).ToHashSet();
                var contributors = _boundsContributors.Where(x => includes.Contains(x.BoundsGroup)).ToList();

                if (boundsType == BoundsType.AxisAligned) {
                    if (contributors.Count == 0) {
                        cachedBounds = new OrientedBox(transform.position, float3.zero, quaternion.identity);
                    } else {
                        cachedBounds = BoundsHelper.AABBFor(contributors); //
                    }
                } else if (boundsType == BoundsType.OrientedBox) {
                    cachedBounds = BoundsHelper.ComputeFor(transform, contributors);
                } else if (boundsType == BoundsType.LocalBounds) {
                    var basis = new OrientedBox(transform.position, float3.zero, transform.rotation);
                    foreach (var contributor in contributors) {
                        var encapsulated = contributor.EncapsulateIn(basis);
                        basis = basis.EncapsulateFixedCenter(encapsulated);
                    }
                    cachedBounds = basis;
                }

                bounds = new OrientedBoxDisplay(cachedBounds);
            }

            private Color ElementColor(int index, Color defaultColor) {
                var basis = new Color(0.235f, 0.235f, 0.235f);
                if (index < 0 || index >= groups.Count) return basis;
                var selected = groups[index].include;
                var target = selected ? Color.green : Color.red;
                return Color.Lerp(basis, target, 0.025f);
            }

        }

        [Serializable, InlineProperty, HideLabel]
        private struct OrientedBoxDisplay {

            [TitleGroup("Bounds"), ReadOnly]
            public Vector3 center;
            [TitleGroup("Bounds"), ReadOnly]
            public Vector3 size;
            [TitleGroup("Bounds"), ReadOnly]
            public Vector3 rotation;

            public OrientedBoxDisplay(OrientedBox box) {
                center = box.center;
                size = box.Size;
                rotation = ((Quaternion)box.rotation).eulerAngles;
            }

            [VerticalGroup("Bounds/Column")]
            [Button("Position", Icon = SdfIconType.Files), ButtonGroup("Bounds/Column/Buttons")]
            public void CopyPosition() => CopyVector3(center);

            [Button("Size", Icon = SdfIconType.Files), ButtonGroup("Bounds/Column/Buttons")]
            public void CopySize() => CopyVector3(size);

            [Button("Rotation", Icon = SdfIconType.Files), ButtonGroup("Bounds/Column/Buttons")]
            public void CopyRotation() => CopyVector3(rotation);

            private void CopyVector3(Vector3 value) {
                var copyString =
                    $"Vector3({value.x.ToString(CultureInfo.InvariantCulture)}," +
                    $"{value.y.ToString(CultureInfo.InvariantCulture)}," +
                    $"{value.z.ToString(CultureInfo.InvariantCulture)})";
                GUIUtility.systemCopyBuffer = copyString;
            }

        }


        [Serializable]
        private struct GroupFilter {

            public static string[] DefaultIncludes = { "Colliders", "Renderers" };

            [HorizontalGroup, HideLabel, DisplayAsString]
            public string name;

            [HorizontalGroup(width: 85, marginLeft: 10)]
            public bool include;

        }

        private enum BoundsType {
            [Tooltip("Grows an anchored box around the current pivot")]
            LocalBounds,
            [Tooltip("Computes a non restricted oriented box")]
            OrientedBox,
            [Tooltip("Computes a non restricted axis aligned bounding box")]
            AxisAligned

        }

    }
}