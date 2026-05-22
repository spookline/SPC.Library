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
                receiver ??= transform.GetComponent<IBoundsReceiver>();
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
            public IBoundsReceiver receiver;

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

        [Serializable, InlineProperty, HideLabel]
        private struct PivotRecenter {

            [HideInInspector]
            public IPivotRecenter target;

            [TabGroup("Quick"), HideLabel, ShowInInspector, InlineProperty]
            public Quick quick;

            [TabGroup("Align"), HideLabel, ShowInInspector, InlineProperty]
            public Align align;

            [TabGroup("Custom"), HideLabel, ShowInInspector, InlineProperty]
            public Custom custom;

            public PivotRecenter(IPivotRecenter target) {
                this.target = target;
                align = new Align(target);
                custom = new Custom(target);
                quick = new Quick(target);
            }


            [Serializable]
            public struct Quick {

                [HideInInspector]
                public IPivotRecenter target;

                public Quick(IPivotRecenter target) {
                    this.target = target;
                }

                [ButtonGroup, Button, PropertyOrder(0)]
                public void Center() => BoundsHelper.PivotRecenter(target);

                [ButtonGroup, Button, PropertyOrder(0)]
                public void GroundCenter() => BoundsHelper.PivotRecenterGroundAligned(target);

                [ButtonGroup, Button, PropertyOrder(0)]
                public void ZForward() => BoundsHelper.PivotRecenterZForward(target);


                [TitleGroup("Clear", alignment: TitleAlignments.Centered)]
                [ButtonGroup("Clear/Buttons"), Button("Position"), PropertyOrder(1)]
                public void ClearPosition() => BoundsHelper.PivotRemovePosition(target);

                [ButtonGroup("Clear/Buttons"), Button("Rotation"), PropertyOrder(1)]
                public void ClearRotation() => BoundsHelper.PivotRemoveRotation(target);

                [ButtonGroup("Clear/Buttons"), Button("Scale"), PropertyOrder(1)]
                public void ClearScale() => BoundsHelper.PivotRemoveScale(target);

            }

            [Serializable]
            public struct Align {

                [HideInInspector]
                public IPivotRecenter target;

                public Align(IPivotRecenter target) {
                    this.target = target;
                    proportionalScale = new float3(0.5f, 0.5f, 0.5f);
                    normalizedScale = float3.zero;
                }

                [BoxGroup("Proportional", showLabel: false), HideLabel]
                public float3 proportionalScale;

                [BoxGroup("Proportional"), Button]
                public void AlignProportional() => BoundsHelper.PivotRecenterProportional(target, proportionalScale);


                [BoxGroup("Normalized", showLabel: false), HideLabel]
                public float3 normalizedScale;

                [BoxGroup("Normalized"), Button]
                public void AlignNormalized() => BoundsHelper.PivotRecenterNormalized(target, normalizedScale);

            }

            [Serializable]
            public struct Custom {

                [HideInInspector]
                public IPivotRecenter target;

                public Custom(IPivotRecenter target) {
                    this.target = target;
                    custom = TRS.Identity;
                    local = true;
                }

                [HideLabel, InlineProperty]
                public TRS custom;

                public bool local;

                [ButtonGroup, Button]
                public void Set() => BoundsHelper.PivotRepivot(target, custom, local);

                [ButtonGroup, Button]
                public void Move() => BoundsHelper.PivotMovePivot(target, custom, local);

                [ButtonGroup, Button]
                public void MoveData() => BoundsHelper.PivotMoveData(target, custom, local);

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