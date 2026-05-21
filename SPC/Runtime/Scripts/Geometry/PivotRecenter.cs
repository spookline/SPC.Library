using System;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Geometry {
    public interface IPivotRecenter {

        Transform GetPivotRootTransform();
        OrientedBox GetPivotBounds();
        void ApplyPivotDeltas(AffineTransform delta);

    }

    [Serializable, InlineProperty, HideLabel, TitleGroup("Pivot", "Utilities for modifying the pivot point of this object.", alignment: TitleAlignments.Split)]
    public struct PivotRecenter {

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
            public void Center() => PivotHelper.Recenter(target);

            [ButtonGroup, Button, PropertyOrder(0)]
            public void GroundCenter() => PivotHelper.RecenterGroundAligned(target);

            [ButtonGroup, Button, PropertyOrder(0)]
            public void ZForward() => PivotHelper.RecenterZForward(target);


            [TitleGroup("Clear", alignment: TitleAlignments.Centered)]
            [ButtonGroup("Clear/Buttons"), Button("Position"), PropertyOrder(1)]
            public void ClearPosition() => PivotHelper.RemovePosition(target);

            [ButtonGroup("Clear/Buttons"), Button("Rotation"), PropertyOrder(1)]
            public void ClearRotation() => PivotHelper.RemoveRotation(target);

            [ButtonGroup("Clear/Buttons"), Button("Scale"), PropertyOrder(1)]
            public void ClearScale() => PivotHelper.RemoveScale(target);

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
            public void AlignProportional() => PivotHelper.RecenterProportional(target, proportionalScale);


            [BoxGroup("Normalized", showLabel: false), HideLabel]
            public float3 normalizedScale;

            [BoxGroup("Normalized"), Button]
            public void AlignNormalized() => PivotHelper.RecenterNormalized(target, normalizedScale);

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
            public void Set() => PivotHelper.Repivot(target, custom, local);

            [ButtonGroup, Button]
            public void Move() => PivotHelper.MovePivot(target, custom, local);

            [ButtonGroup, Button]
            public void MoveData() => PivotHelper.MoveData(target, custom, local);

        }

    }

    public static class PivotHelper {

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

    }
}