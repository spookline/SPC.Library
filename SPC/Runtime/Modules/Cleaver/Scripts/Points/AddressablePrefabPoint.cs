using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Spookline.SPC.Debugging;
using Spookline.SPC.Draw;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Spookline.SPC.Cleaver.Points {

    public class AddressablePrefabPoint : CleaverPoint<AddressablePrefabPoint.Authoring> {

        public const string Name = "Addressable Prefab";

        [Serializable, TypeRegistryItem(Name, Icon = SdfIconType.Box)]
        public class Authoring : EditableTransformPoint<Authoring, AddressablePrefabPoint> {

            [OdinSerialize]
            public AssetReferenceGameObject prefab;

            public override string TypeName => Name;
            public override Authoring InstantiateAuthoring() => new();

            public override void CopyFromAuthoring(Authoring other) {
                base.CopyFromAuthoring(other);
                prefab = other.prefab;
            }

            public override void DrawEditor(AffineTransform transform, IDrawingAPI draw) {
                base.DrawEditor(transform, draw);

                var worldTransform = WorldTransform(transform);
                using (draw.Scope(Color.red, (float4x4)worldTransform)) { DrawAddressableMesh(draw, prefab); }
            }

            public override AddressablePrefabPoint InstantiateRuntime(
                AffineTransform transform,
                AffineTransform worldTransform
            ) {
                return new AddressablePrefabPoint(this, worldTransform, prefab);
            }

        }

        public AffineTransform Transform { get; }
        public AssetReferenceGameObject Prefab { get; }

        public GameObject SpawnedObject { get; private set; }

        public AddressablePrefabPoint(
            Authoring source,
            AffineTransform transform,
            AssetReferenceGameObject prefab
        ) : base(source) {
            Transform = transform;
            Prefab = prefab;
        }

        public override void Gizmos(ref GizmoEvt evt) {
            // if (evt.DrawingPass(out var draw)) {
            //     var pos = math.transform(Transform, Vector3.zero);
            //     draw.Sphere(pos, 0.25f);
            // }
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