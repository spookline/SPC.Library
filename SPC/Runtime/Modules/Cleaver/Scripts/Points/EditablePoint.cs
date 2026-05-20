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
    public abstract class EditablePoint {

        public static CleaverSection CurrentSection;

        [OdinSerialize]
        public Vector3 position;

#if UNITY_EDITOR
        [NonSerialized]
        public Dictionary<string, object> editorData = new();
#endif

        public abstract void DrawEditor(AffineTransform transform, IDrawingAPI draw);

        public abstract void DrawHandles(AffineTransform transform);
        public abstract CleaverPoint Materialize(AffineTransform transform);

        public abstract EditablePoint Clone();
        public abstract void CopyFrom(EditablePoint other);
        public virtual string TypeName => "Point";

        public virtual void DrawOverlayGUI() { }

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

    public abstract class CleaverPoint {

        public virtual void Gizmos(ref GizmoEvt evt) { }

    }

    public abstract class CleaverPoint<T> : CleaverPoint where T : EditablePoint {

        public T source;

        protected CleaverPoint() { }

        protected CleaverPoint(T source) {
            this.source = source;
        }

    }


    public static class DefaultHandleDrawers {

        public static void Position(AffineTransform transform, EditablePoint point) {
#if UNITY_EDITOR
            using (new Handles.DrawingScope((float4x4)transform)) {
                var next = Handles.PositionHandle(point.position, Quaternion.identity);
                point.position = next;
            }
#endif
        }

        public static void Transform<T>(AffineTransform transform, T point)
            where T : EditablePoint, IRotatablePoint {
#if UNITY_EDITOR
            using (new Handles.DrawingScope((float4x4)transform)) {
                var p = point.position;
                var r = point.Rotation;
                Handles.TransformHandle(ref p, ref r);
                point.position = p;
                point.Rotation = r;
            }
#endif
        }

        public static void TransformScale<T>(AffineTransform transform, T point)
            where T : EditablePoint, IRotatablePoint, IScalablePoint {
#if UNITY_EDITOR
            using (new Handles.DrawingScope((float4x4)transform)) {
                var p = point.position;
                var r = point.Rotation;
                var s = point.Scale;
                Handles.TransformHandle(ref p, ref r, ref s);
                point.position = p;
                point.Rotation = r;
                point.Scale = s;
            }
#endif
        }

        public static void Position(AffineTransform transform, EditablePoint point, Quaternion rotation) {
#if UNITY_EDITOR
            using (new Handles.DrawingScope((float4x4)transform)) {
                var next = Handles.PositionHandle(point.position, rotation);
                point.position = next;
            }
#endif
        }

        public static void Rotation<T>(AffineTransform transform, T point)
            where T : EditablePoint, IRotatablePoint {
#if UNITY_EDITOR
            using (new Handles.DrawingScope((float4x4)transform)) {
                var next = Handles.RotationHandle(point.Rotation, point.position);
                point.Rotation = next;
            }
#endif
        }

        public static void Scale<T>(AffineTransform transform, T point)
            where T : EditablePoint, IScalablePoint {
#if UNITY_EDITOR
            using (new Handles.DrawingScope((float4x4)transform)) {
                var next = Handles.ScaleHandle(point.Scale, point.position, Quaternion.identity, 1f);
                point.Scale = next;
            }
#endif
        }

        public static void Scale<T>(AffineTransform transform, T point, Quaternion rotation)
            where T : EditablePoint, IScalablePoint {
#if UNITY_EDITOR
            using (new Handles.DrawingScope((float4x4)transform)) {
                var next = Handles.ScaleHandle(point.Scale, point.position, rotation, 1f);
                point.Scale = next;
            }
#endif
        }

        public static void BoundingBox<T>(AffineTransform transform, T point)
            where T : EditablePoint, IBoundingPoint, IRotatablePoint {
#if UNITY_EDITOR
            const string handleKey = "boundsHandle";

            point.editorData ??= new Dictionary<string, object>();

            if (!point.editorData.TryGetValue(handleKey, out var handleObj) ||
                handleObj is not BoxBoundsHandle boundsHandle) {
                boundsHandle = new BoxBoundsHandle();
                point.editorData[handleKey] = boundsHandle;
            }

            var localMatrix = Matrix4x4.TRS(point.position, point.Rotation, Vector3.one);
            var transformMatrix = (Matrix4x4)(float4x4)transform;
            var worldMatrix = transformMatrix * localMatrix;

            var worldPos = worldMatrix.MultiplyPoint3x4(Vector3.zero);
            var wordRot = worldMatrix.rotation;
            var drawingMatrix = Matrix4x4.TRS(worldPos, wordRot, Vector3.one);
            using (new Handles.DrawingScope(drawingMatrix)) {
                boundsHandle.center = Vector3.zero;
                boundsHandle.size = point.Extents;

                EditorGUI.BeginChangeCheck();
                boundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck()) {
                    point.Extents = boundsHandle.size;
                    var newWorldPos = drawingMatrix.MultiplyPoint3x4(boundsHandle.center);
                    point.position = math.transform(math.inverse(transform), newWorldPos);
                }
            }
#endif
        }

    }

    public class OrientedBoxPoint : CleaverPoint<EditableOrientedBoxPoint> {

        public OrientedBox Box { get; }

        public OrientedBoxPoint(EditableOrientedBoxPoint source, OrientedBox box) : base(source) {
            Box = box;
        }

        public override void Gizmos(ref GizmoEvt evt) {
            if (evt.DrawingPass(out var draw)) {
                draw.OrientedBox(Box);
                draw.Sphere(Box.LocalGroundCenter, 0.5f);
            }
        }

    }

    [Serializable]
    public class EditableOrientedBoxPoint : EditablePoint, IRotatablePoint, IBoundingPoint {

        [OdinSerialize]
        private Quaternion _rotation = Quaternion.identity;

        [OdinSerialize]
        private Vector3 _extents = Vector3.one;

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

        public override void DrawHandles(AffineTransform transform) {
            DefaultHandleDrawers.Transform(transform, this);
            DefaultHandleDrawers.BoundingBox(transform, this);
        }

        public override CleaverPoint Materialize(AffineTransform transform) {
            math.decompose(transform, out _, out var rotation, out var scale);
            var p = math.transform(transform, position);
            var r = rotation * Rotation;
            var e = Extents * scale;
            return new OrientedBoxPoint(this, new OrientedBox(p, e, r));
        }

        public override EditablePoint Clone() =>
            new EditableOrientedBoxPoint {
                position = position,
                Rotation = Rotation,
                Extents = Extents,
            };

        public override void CopyFrom(EditablePoint other) {
            if (other is not EditableOrientedBoxPoint o) return;
            position = o.position;
            Rotation = o.Rotation;
            Extents = o.Extents;
        }

        public override string TypeName => "Box";

        public override void DrawOverlayGUI() {
#if UNITY_EDITOR
            position = EditorGUILayout.Vector3Field("Position", position);
            _rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", Rotation.eulerAngles));
            _extents = EditorGUILayout.Vector3Field("Extents", Extents);
#endif
        }

    }

    public class TransformPoint : CleaverPoint<EditableTransformPoint> {

        public AffineTransform Transform { get; }

        public TransformPoint(EditableTransformPoint source, AffineTransform transform) : base(source) {
            Transform = transform;
        }

        public override void Gizmos(ref GizmoEvt evt) {
            if (evt.DrawingPass(out var draw)) {
                var pos = math.transform(Transform, Vector3.zero);
                draw.Sphere(pos, 0.25f);
            }
        }

    }

    [Serializable]
    public class EditableTransformPoint : EditablePoint, IRotatablePoint, IScalablePoint {

        [OdinSerialize]
        private Quaternion _rotation = Quaternion.identity;

        [OdinSerialize]
        private Vector3 _scale = Vector3.one;

        public Quaternion Rotation {
            get => _rotation;
            set => _rotation = value;
        }

        public Vector3 Scale {
            get => _scale;
            set => _scale = value;
        }

        public override void DrawEditor(AffineTransform transform, IDrawingAPI draw) {
            var localTransform = new AffineTransform(position, Rotation, Scale);
            var worldTransform = math.mul(transform, localTransform);
            using (draw.Scope((float4x4)worldTransform)) { draw.Sphere(Vector3.zero, 0.25f); }
        }

        public override void DrawHandles(AffineTransform transform) {
            DefaultHandleDrawers.TransformScale(transform, this);
        }

        public override CleaverPoint Materialize(AffineTransform transform) {
            var localTransform = new AffineTransform(position, Rotation, Scale);
            var worldTransform = math.mul(transform, localTransform);
            return new TransformPoint(this, worldTransform);
        }

        public override EditablePoint Clone() =>
            new EditableTransformPoint {
                position = position,
                Rotation = Rotation,
                Scale = Scale
            };

        public override void CopyFrom(EditablePoint other) {
            if (other is not EditableTransformPoint o) return;
            position = o.position;
            Rotation = o.Rotation;
            Scale = o.Scale;
        }

        public override string TypeName => "Transform";


        public override void DrawOverlayGUI() {
#if UNITY_EDITOR
            position = EditorGUILayout.Vector3Field("Position", position);
            Rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", Rotation.eulerAngles));
            Scale = EditorGUILayout.Vector3Field("Scale", Scale);
#endif
        }

    }
}