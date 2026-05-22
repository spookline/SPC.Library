using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Cinemachine;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;

namespace Spookline.SPC.Geometry {
    public static class BoundsHelper {

        public static MinMaxAABB ToMinMaxAABB(this Bounds bounds) {
            return new MinMaxAABB(bounds.min, bounds.max);
        }

        public static Bounds ComputeBounds(IReadOnlyList<Renderer> renderers) {
            if (renderers == null || renderers.Count == 0) { return new Bounds(); }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Count; i++) {
                var localBounds = renderers[i].bounds;
                bounds.Encapsulate(localBounds);
            }

            return bounds;
        }

        public static Bounds ComputeBounds(IReadOnlyList<Collider> colliders) {
            if (colliders == null || colliders.Count == 0) { return new Bounds(); }

            var bounds = colliders[0].bounds;
            for (var i = 1; i < colliders.Count; i++) { bounds.Encapsulate(colliders[i].bounds); }

            return bounds;
        }

        public static OrientedBox ToOrientedBox(this Renderer renderer) {
            return GetBoundsForRenderer(renderer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ClosestPoint(this MinMaxAABB aabb, float3 point) {
            return math.clamp(point, aabb.Min, aabb.Max);
        }

        public static List<IBoundsContributor> Collect(Transform transform) {
            var list = new List<IBoundsContributor>();
            list.Collect(transform.GetComponentsInChildren<Collider>());
            list.Collect(transform.GetComponentsInChildren<Renderer>());
            foreach (var provider in transform.GetComponentsInChildren<ICustomBoundsProvider>()) {
                provider.ContributeBoundingBoxes(list);
            }

            return list;
        }

        public static void Collect(this List<IBoundsContributor> contributors, IReadOnlyList<Collider> colliders) {
            foreach (var collider in colliders) {
                if (collider is SphereCollider sphere) {
                    if (!collider.transform.lossyScale.IsUniformScale()) continue;
                    var other = new AffineTransform(
                        sphere.center,
                        quaternion.identity,
                        new float3(sphere.radius)
                    );
                    collider.transform.Affine().Transform(other).Decompose(out var pos, out _, out var scale);
                    contributors.Add(new UnitSphereColliderContributor(pos, scale.x, sphere));
                    continue;
                }

                contributors.Add(new GeneralColliderContributor(GetBoundsForCollider(collider), collider));
            }
        }

        public static void Collect(this List<IBoundsContributor> contributors, IReadOnlyList<Renderer> renderers) {
            foreach (var renderer in renderers) {
                contributors.Add(new RendererBoundsContributor(GetBoundsForRenderer(renderer), renderer));
            }
        }

        public static OrientedBox ComputeFor(List<IBoundsContributor> contributors) {
            if (contributors == null || contributors.Count == 0) return OrientedBox.zero;
            var firstPass = contributors[0].GetBounds();
            for (var i = 1; i < contributors.Count; i++)
                firstPass = firstPass.Encapsulate(contributors[i].GetBounds());
            return firstPass;
        }

        public static OrientedBox ComputeFor(Transform pivot, List<IBoundsContributor> contributors) {
            if (contributors == null || contributors.Count == 0) return new OrientedBox(pivot.position, float3.zero, pivot.rotation);

            var firstPass = ComputeFor(contributors);
            var firstPassLength = math.length(firstPass.halfExtent);

            if (firstPass.ContainsPoint(pivot.position)) {
                var secondPass = new OrientedBox(firstPass.center, float3.zero, pivot.rotation);
                foreach (var contributor in contributors) {
                    secondPass = secondPass.Encapsulate(contributor.GetBounds()); // This corrects sphere colliders
                }

                var secondPassLength = math.length(secondPass.halfExtent);
                if (secondPassLength < firstPassLength || Mathf.Approximately(secondPassLength, firstPassLength))
                    return secondPass;
            }

            return firstPass;
        }

        public static MinMaxAABB AABBFor(List<IBoundsContributor> contributors) {
            if (contributors == null || contributors.Count == 0) return new MinMaxAABB();
            var bounds = contributors[0].GetAABB();
            for (var i = 1; i < contributors.Count; i++) {
                bounds.Encapsulate(contributors[i].GetAABB());
            }
            return bounds;
        }

        public static OrientedBox GetBoundsForColliders(Transform pivot, IReadOnlyList<Collider> colliders) {
            if (colliders == null || colliders.Count == 0) return OrientedBox.zero;
            var firstPass = GetBoundsForCollider(colliders[0]);
            for (var i = 1; i < colliders.Count; i++)
                firstPass = firstPass.Encapsulate(GetBoundsForCollider(colliders[i]));

            var firstPassLength = math.length(firstPass.halfExtent);

            if (firstPass.ContainsPoint(pivot.position)) {
                var secondPass = new OrientedBox(firstPass.center, float3.zero, pivot.rotation);
                foreach (var collider in colliders) {
                    secondPass =
                        secondPass.Encapsulate(GetBoundsForCollider(collider)); // This corrects sphere colliders
                }

                var secondPassLength = math.length(secondPass.halfExtent);
                if (secondPassLength < firstPassLength || Mathf.Approximately(secondPassLength, firstPassLength))
                    return secondPass;
                Debug.Log(
                    "BoundsHelper: Pivot is inside bounds, but encapsulating with pivot rotation produced larger bounds. This may be due to non-uniform scale on sphere colliders. Returning unrotated bounds.",
                    pivot
                );
            }

            return firstPass;
        }

        public static OrientedBox GetBoundsForRenderer(Renderer renderer) {
            var affine = renderer.transform.Affine();
            var bounds = renderer.localBounds;
            var other = new AffineTransform(bounds.center, quaternion.identity, bounds.size);
            return affine.Transform(other).Decompose().ToOrientedBox();
        }

        public static OrientedBox GetBoundsForCollider(Collider collider) {
            var affine = collider.transform.Affine();
            switch (collider) {
                case BoxCollider box: {
                    var other = new AffineTransform(box.center, quaternion.identity, box.size);
                    return affine.Transform(other).Decompose().ToOrientedBox();
                }
                case SphereCollider sphere: {
                    var other = new AffineTransform(
                        sphere.center,
                        quaternion.identity,
                        new float3(sphere.radius * 2f, sphere.radius * 2f, sphere.radius * 2f)
                    );
                    return affine.Transform(other).Decompose().ToOrientedBox();
                }
                case CapsuleCollider capsule: {
                    var other = new AffineTransform(
                        capsule.center,
                        quaternion.identity,
                        new float3(capsule.radius * 2f, capsule.height, capsule.radius * 2f)
                    );
                    return affine.Transform(other).Decompose().ToOrientedBox();
                }
                case MeshCollider meshCollider when meshCollider.sharedMesh != null: {
                    var bounds = meshCollider.sharedMesh.bounds;
                    var other = new AffineTransform(bounds.center, quaternion.identity, bounds.size);
                    return affine.Transform(other).Decompose().ToOrientedBox();
                }
                default:
                    return new OrientedBox(collider.bounds.center, collider.bounds.extents, Quaternion.identity);
            }
        }

        public static OrientedBox EncapsulateCollider(OrientedBox previous, Collider collider) {
            if (collider is SphereCollider sphere) {
                if (!collider.transform.lossyScale.IsUniformScale()) {
                    Debug.LogWarning(
                        $"Non-uniform scale on sphere collider {collider.name} may produce inaccurate bounds encapsulation",
                        collider
                    );
                } else {
                    var other = new AffineTransform(
                        sphere.center,
                        quaternion.identity,
                        new float3(sphere.radius)
                    );
                    collider.transform.Affine().Transform(other).Decompose(out var pos, out _, out var scale);
                    return previous.Encapsulate(pos, scale.x);
                }
            }

            return previous.Encapsulate(GetBoundsForCollider(collider));
        }

    }

    public interface IBoundsContributor {

        OrientedBox GetBounds();
        MinMaxAABB GetAABB();
        OrientedBox EncapsulateIn(OrientedBox original);
        public string BoundsGroup { get; }

    }

    public interface ICustomBoundsProvider {

        public void ContributeBoundingBoxes(List<IBoundsContributor> contributors);

    }

    public interface IColliderBoundsContributor : IBoundsContributor { }

    public interface IBoundModificationReceiver {

        public void ReceiveBounds(OrientedBox box);

    }

    public struct UnitSphereColliderContributor : IColliderBoundsContributor {

        public readonly float3 position;
        public readonly float radius;
        public SphereCollider collider;

        public UnitSphereColliderContributor(float3 position, float radius, SphereCollider collider) {
            this.position = position;
            this.radius = radius;
            this.collider = collider;
        }


        public OrientedBox GetBounds() => new(position, new float3(radius), quaternion.identity);
        public MinMaxAABB GetAABB() => collider.bounds.ToMinMaxAABB();

        public OrientedBox EncapsulateIn(OrientedBox original) {
            return original.Encapsulate(position, radius);
        }

        public string BoundsGroup => "Colliders";

    }

    public struct GeneralColliderContributor : IColliderBoundsContributor {

        public readonly OrientedBox box;
        public readonly Collider collider;

        public GeneralColliderContributor(OrientedBox box, Collider collider) {
            this.box = box;
            this.collider = collider;
        }

        public OrientedBox GetBounds() => box;

        public MinMaxAABB GetAABB() => collider.bounds.ToMinMaxAABB();

        public OrientedBox EncapsulateIn(OrientedBox original) {
            return original.Encapsulate(box);
        }

        public string BoundsGroup => "Colliders";
    }

    public struct RendererBoundsContributor : IBoundsContributor {

        public readonly OrientedBox box;
        public Renderer renderer;

        public RendererBoundsContributor(OrientedBox box, Renderer renderer) {
            this.box = box;
            this.renderer = renderer;
        }

        public OrientedBox GetBounds() => box;
        public MinMaxAABB GetAABB() => renderer.bounds.ToMinMaxAABB();

        public OrientedBox EncapsulateIn(OrientedBox original) {
            return original.Encapsulate(box);
        }

        public string BoundsGroup => "Renderers";

    }
}