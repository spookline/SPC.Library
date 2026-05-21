using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Spookline.SPC.Debugging;
using Spookline.SPC.Draw;
using Spookline.SPC.Geometry;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#endif

namespace Spookline.SPC.Cleaver.Points {
    [Serializable]
    public abstract class EditablePoint {

        public static string Filter;
        public static readonly EditablePointGuiFactory Gui = new();
        public static readonly EditablePointHandleFactory Handles = new();
        public static CleaverSection CurrentSection;

        [OdinSerialize]
        public Vector3 position;

#if UNITY_EDITOR
        public static Dictionary<string, MeshFilter[]> EditorMeshCache = new();

        [NonSerialized]
        public Dictionary<string, object> editorData = new();
        [NonSerialized]
        public bool editorHidden = false;
#endif

        public abstract void DrawEditor(AffineTransform transform, IDrawingAPI draw);

        public abstract void DrawHandles(AffineTransform transform);
        public abstract CleaverPoint Instantiate(AffineTransform transform);

        public virtual EditablePoint Clone() {
            var point = (EditablePoint)MemberwiseClone();
            point.CopyFrom(this);
            return point;
        }

        public abstract void CopyFrom(EditablePoint other);
        public virtual string TypeName => GetType().Name;

        public virtual void DrawOverlayGUI() { }

        protected static void DrawAddressableMesh<T>(IDrawingAPI draw, AssetReferenceT<T> reference)
            where T : Object {
            if (reference == null) return;
#if UNITY_EDITOR
            if (Application.isPlaying) return;
            var path = AssetDatabase.GUIDToAssetPath(reference.AssetGUID);
            if (!EditorMeshCache.TryGetValue(path, out var meshFilters)) {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefab) return;
                meshFilters = prefab.GetComponentsInChildren<MeshFilter>();
                EditorMeshCache[path] = meshFilters;
            }

            foreach (var meshFilter in meshFilters) {
                var transform = float4x4.TRS(
                    meshFilter.transform.position,
                    meshFilter.transform.rotation,
                    meshFilter.transform.lossyScale
                );
                using (draw.ScopeTransformation(transform)) { draw.Mesh(meshFilter.sharedMesh); }
            }
#endif
        }

    }

    public class EditablePointHandleFactory { }

    public class EditablePointGuiFactory { }

    public interface IRotatablePoint {

        public Quaternion Rotation { get; set; }

    }

    public interface IScalablePoint {

        public Vector3 Scale { get; set; }

    }

    public interface IBoundingPoint {

        public Vector3 Extents { get; set; }

    }

    public abstract class CleaverPoint : IDisposable {

        public virtual void Gizmos(ref GizmoEvt evt) { }

        public virtual void Initialize(CleaverSection section) { }

        public virtual void Rebuild(CleaverSection section) { }

        public virtual void Dispose() { }

    }

    public abstract class CleaverPoint<T> : CleaverPoint where T : EditablePoint {

        [HideLabel]
        public T source;

        protected CleaverPoint() { }

        protected CleaverPoint(T source) {
            this.source = source;
        }

    }

    public static class DefaultHandleFactoryExtensions {

        public static void Position(this EditablePointHandleFactory _, AffineTransform transform, EditablePoint point) {
#if UNITY_EDITOR
            using (new Handles.DrawingScope((float4x4)transform)) {
                var next = Handles.PositionHandle(point.position, Quaternion.identity);
                point.position = next;
            }
#endif
        }

        public static void Transform<T>(this EditablePointHandleFactory _, AffineTransform transform, T point)
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

        public static void TransformScale<T>(this EditablePointHandleFactory _, AffineTransform transform, T point)
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

        public static void Position(
            this EditablePointHandleFactory _,
            AffineTransform transform,
            EditablePoint point,
            Quaternion rotation
        ) {
#if UNITY_EDITOR
            using (new Handles.DrawingScope((float4x4)transform)) {
                var next = Handles.PositionHandle(point.position, rotation);
                point.position = next;
            }
#endif
        }

        public static void Rotation<T>(this EditablePointHandleFactory _, AffineTransform transform, T point)
            where T : EditablePoint, IRotatablePoint {
#if UNITY_EDITOR
            using (new Handles.DrawingScope((float4x4)transform)) {
                var next = Handles.RotationHandle(point.Rotation, point.position);
                point.Rotation = next;
            }
#endif
        }

        public static void Scale<T>(this EditablePointHandleFactory _, AffineTransform transform, T point)
            where T : EditablePoint, IScalablePoint {
#if UNITY_EDITOR
            using (new Handles.DrawingScope((float4x4)transform)) {
                var next = Handles.ScaleHandle(point.Scale, point.position, Quaternion.identity, 1f);
                point.Scale = next;
            }
#endif
        }

        public static void Scale<T>(
            this EditablePointHandleFactory _,
            AffineTransform transform,
            T point,
            Quaternion rotation
        )
            where T : EditablePoint, IScalablePoint {
#if UNITY_EDITOR
            using (new Handles.DrawingScope((float4x4)transform)) {
                var next = Handles.ScaleHandle(point.Scale, point.position, rotation, 1f);
                point.Scale = next;
            }
#endif
        }

        public static void BoundingBox<T>(this EditablePointHandleFactory _, AffineTransform transform, T point)
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

    public static class DefaultGuiFactoryExtensions {

        public static Vector3 Vector3(this EditablePointGuiFactory _, string label, Vector3 value, float width = 60f) {
#if UNITY_EDITOR
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(width));
            var result = EditorGUILayout.Vector3Field(GUIContent.none, value);
            EditorGUILayout.EndHorizontal();
            return result;
#endif
            return value;
        }

        public static float Float(this EditablePointGuiFactory _, string label, float value) {
#if UNITY_EDITOR
            return EditorGUILayout.FloatField(label, value);
#endif
            return value;
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
            Handles.Transform(transform, this);
            Handles.BoundingBox(transform, this);
        }

        public override CleaverPoint Instantiate(AffineTransform transform) {
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
            position = Gui.Vector3("Position", position);
            Rotation = Quaternion.Euler(Gui.Vector3("Rotation", Rotation.eulerAngles));
            Extents = Gui.Vector3("Extents", Extents);
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
            Handles.TransformScale(transform, this);
        }

        public override CleaverPoint Instantiate(AffineTransform transform) {
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
            position = Gui.Vector3("Position", position);
            Rotation = Quaternion.Euler(Gui.Vector3("Rotation", Rotation.eulerAngles));
            Scale = Gui.Vector3("Scale", Scale);
        }

    }


    public class DeferredSpawnPoint : CleaverPoint<DeferredSpawnPoint.Authoring> {

        public const string Name = "Deferred Spawn";

        [Serializable, TypeRegistryItem(Name)]
        public class Authoring : EditableTransformPoint {

            [OdinSerialize]
            public AssetReferenceGameObject prefab;

            public override string TypeName => Name;

            public override EditablePoint Clone() {
                return new Authoring {
                    position = position,
                    Rotation = Rotation,
                    Scale = Scale,
                    prefab = prefab
                };
            }

            public override void CopyFrom(EditablePoint other) {
                if (other is not Authoring o) return;
                position = o.position;
                Rotation = o.Rotation;
                Scale = o.Scale;
                prefab = o.prefab;
            }

            public override void DrawEditor(AffineTransform transform, IDrawingAPI draw) {
                base.DrawEditor(transform, draw);

                var localTransform = new AffineTransform(position, Rotation, Scale);
                var worldTransform = math.mul(transform, localTransform);
                using (draw.Scope(Color.red, (float4x4)worldTransform)) { DrawAddressableMesh(draw, prefab); }
            }

            public override CleaverPoint Instantiate(AffineTransform transform) {
                var localTransform = new AffineTransform(position, Rotation, Scale);
                var worldTransform = math.mul(transform, localTransform);
                return new DeferredSpawnPoint(this, worldTransform, prefab);
            }
        }

        public AffineTransform Transform { get; }
        public AssetReferenceGameObject Prefab { get; }

        public GameObject SpawnedObject { get; private set; }

        public DeferredSpawnPoint(
            Authoring source,
            AffineTransform transform,
            AssetReferenceGameObject prefab
        ) : base(source) {
            Transform = transform;
            Prefab = prefab;
        }

        public override void Gizmos(ref GizmoEvt evt) {
            if (evt.DrawingPass(out var draw)) {
                var pos = math.transform(Transform, Vector3.zero);
                draw.Sphere(pos, 0.25f);
            }
        }

        public override void Initialize(CleaverSection section) {
            Load().Forget();
        }

        public async UniTaskVoid Load() {
            math.decompose(Transform, out var pos, out var rot, out var scale);
            var obj = await Addressables.InstantiateAsync(Prefab);
            obj.transform.localScale = scale;
            obj.transform.localPosition = pos;
            obj.transform.localRotation = rot;
            obj.hideFlags = HideFlags.DontSave;
            SpawnedObject = obj;
        }

        public override void Dispose() {
            base.Dispose();
            if (SpawnedObject) Addressables.ReleaseInstance(SpawnedObject);
        }

    }
}