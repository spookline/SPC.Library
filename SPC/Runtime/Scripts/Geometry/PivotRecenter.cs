using System;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Geometry {
    public interface IPivotRecenter : IPivotDependent {

        Transform GetPivotRootTransform();
        OrientedBox GetPivotBounds();

    }

    public interface IPivotDependent {

        public void ApplyPivotDeltas(AffineTransform delta);

    }

    [Serializable, InlineProperty, HideLabel]
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
            public void Center() => GeometryHelper.Recenter(target);

            [ButtonGroup, Button, PropertyOrder(0)]
            public void GroundCenter() => GeometryHelper.RecenterGroundAligned(target);

            [ButtonGroup, Button, PropertyOrder(0)]
            public void ZForward() => GeometryHelper.RecenterZForward(target);


            [TitleGroup("Clear", alignment: TitleAlignments.Centered)]
            [ButtonGroup("Clear/Buttons"), Button("Position"), PropertyOrder(1)]
            public void ClearPosition() => GeometryHelper.RemovePosition(target);

            [ButtonGroup("Clear/Buttons"), Button("Rotation"), PropertyOrder(1)]
            public void ClearRotation() => GeometryHelper.RemoveRotation(target);

            [ButtonGroup("Clear/Buttons"), Button("Scale"), PropertyOrder(1)]
            public void ClearScale() => GeometryHelper.RemoveScale(target);

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
            public void AlignProportional() => GeometryHelper.RecenterProportional(target, proportionalScale);


            [BoxGroup("Normalized", showLabel: false), HideLabel]
            public float3 normalizedScale;

            [BoxGroup("Normalized"), Button]
            public void AlignNormalized() => GeometryHelper.RecenterNormalized(target, normalizedScale);

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
            public void Set() => GeometryHelper.Repivot(target, custom, local);

            [ButtonGroup, Button]
            public void Move() => GeometryHelper.MovePivot(target, custom, local);

            [ButtonGroup, Button]
            public void MoveData() => GeometryHelper.MoveData(target, custom, local);

        }

    }
}