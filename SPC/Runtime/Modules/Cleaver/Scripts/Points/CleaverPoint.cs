using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using Spookline.SPC.Debugging;
using Spookline.SPC.Draw;
using Spookline.SPC.Geometry;
using Unity.Mathematics;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#endif

namespace Spookline.SPC.Cleaver.Points {
    [Serializable]
    public abstract class CleaverPoint {

        [OdinSerialize]
        public Vector3 position;

#if UNITY_EDITOR
        public Dictionary<string, object> editorData = new();
#endif

        public abstract void DrawEditor(AffineTransform transform, IDrawingAPI draw);

        public abstract void DrawHandles(AffineTransform transform, HandlesDrawingApi draw);
        public abstract void Gizmos(AffineTransform transform, ref GizmoEvt evt);

    }

    public interface IRotatablePoint {

        public Quaternion Rotation { get; set; }

    }

    public interface IScalablePoint {

        public Vector3 Scale { get; set; }

    }

    public interface IBoundingPoint {

        public Vector3 Extents { get; set; }

    }


    public static class DefaultHandleDrawers {

        public static void Position(AffineTransform transform, HandlesDrawingApi draw, CleaverPoint point) {
#if UNITY_EDITOR
            var current = math.transform(transform, point.position);
            var next = Handles.PositionHandle(current, Quaternion.identity);
            point.position = math.transform(math.inverse(transform), next);
#endif
        }

        public static void Rotation<T>(AffineTransform transform, HandlesDrawingApi draw, T point)
            where T : CleaverPoint, IRotatablePoint {
#if UNITY_EDITOR
            var pos = math.transform(transform, point.position);
            var current = math.rotation(transform.rs) * point.Rotation;
            var next = Handles.RotationHandle(point.Rotation, pos);
            point.Rotation = math.mul(math.inverse(math.rotation(transform.rs)), next);
#endif
        }

        public static void BoundingBox<T>(AffineTransform transform, HandlesDrawingApi draw, T point)
            where T : CleaverPoint, IBoundingPoint, IRotatablePoint {
#if UNITY_EDITOR

#endif
        }

    }


    [Serializable]
    public class OrientedBoxPoint : CleaverPoint, IRotatablePoint, IBoundingPoint {

        [OdinSerialize]
        private Quaternion _rotation;

        [OdinSerialize]
        private Vector3 _extents;

        public Quaternion Rotation {
            get => _rotation;
            set => _rotation = value;
        }

        public Vector3 Extents {
            get => _extents;
            set => _extents = value;
        }


        public override void DrawEditor(AffineTransform transform, IDrawingAPI draw) {
            var box = new OrientedBox(position, Extents, Rotation);
            using (draw.Scope((float4x4)transform)) {
                draw.OrientedBox(box);
                draw.Sphere(box.LocalGroundCenter, 0.5f);
            }
        }

        public override void DrawHandles(AffineTransform transform, HandlesDrawingApi draw) { }

        public override void Gizmos(AffineTransform transform, ref GizmoEvt evt) { }

    }
}