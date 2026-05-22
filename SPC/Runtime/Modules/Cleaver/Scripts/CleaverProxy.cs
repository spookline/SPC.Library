using System;
using Sirenix.OdinInspector;
using Spookline.SPC.Common;
using Spookline.SPC.Ext;
using Spookline.SPC.Geometry;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Spookline.SPC.Cleaver {
    [HideMonoScript]
    [ExecuteInEditMode]
    [AddComponentMenu("Cleaver/Proxy")]
    public class CleaverProxy : SpookBehaviour<CleaverProxy>, IPivotDependent, IBoundsReceiver {

        [InlineProperty]
        [HideLabel]
        public IProxySampler sampler = new CenterSampler();

        [Tooltip("Raycasts that land outside with a distance less than this value will be considered valid")]
        [LabelText("Expansion"), Unit(Units.Meter)]
        public float radius = 0.1f;

        [HideLabel, TitleGroup("Volume")]
        public OrientedBox box = new(0, 1, quaternion.identity);

        public ulong Id { get; private set; }

        private void Awake() {
            Id = IdGenerator.NextId();
        }

        private void OnDrawGizmosSelected() {
            if (!GizmosHelper.IsSelected(gameObject)) return;
            using var array = new NativeArray<float3>(GetPointCount(), Allocator.Temp);
            SamplePoints(array, 0);

            // Higher Tier: Red, Lower tier: Green.
            var count = array.Length;
            var tier0Count = sampler?.GetTier0PointCount() ?? 0;
            var tier1Count = sampler?.GetTier1PointCount() ?? 0;
            var tier2Count = sampler?.GetTier2PointCount() ?? 0;

            for (var i = 0; i < count; i++) {
                if (i < tier0Count) Gizmos.color = Color.cyan;
                else if (i < tier1Count) Gizmos.color = Color.green;
                else if (i < tier2Count) Gizmos.color = Color.yellow;
                else Gizmos.color = Color.red;

                Gizmos.DrawSphere(array[i], 0.1f);
            }

            var boundsColor = Color.magenta;
            boundsColor.a = 0.1f;
            Gizmos.color = boundsColor;
            var wsBox = transform.Affine().Transform(box);
            wsBox = wsBox.Grow(0.0001f);
            wsBox.DrawGizmos(false);

            Gizmos.color = Color.magenta;
            var radiusBox = wsBox.Grow(radius);
            radiusBox.DrawGizmos();
        }

        public int GetPointCount() {
            return sampler?.GetPointCount() ?? 0;
        }

        public void SamplePoints(NativeArray<float3> points, int startIndex) {
            var wsBox = transform.Affine().Transform(box);
            sampler?.SamplePoints(wsBox, points, startIndex);
        }

        public CleaverProxyData InitializeProxyData() {
            var wsBox = transform.Affine().Transform(box);
            return new CleaverProxyData {
                radius = radius,
                query = wsBox.Grow(radius),
                bounds = wsBox.AABB(),
                pointIndex = 0,
                pointCount = (ushort)GetPointCount()
            };
        }

        public void ApplyPivotDeltas(AffineTransform delta) {
            box = delta.Transform(box);
        }

        public void ReceiveBounds(OrientedBox newBox) {
            box = transform.Affine().InverseTransform(newBox);
        }

    }

    public interface IProxySampler {

        int GetPointCount();

        int GetTier0PointCount() {
            return GetPointCount();
        }

        int GetTier1PointCount() {
            return GetPointCount();
        }

        int GetTier2PointCount() {
            return GetPointCount();
        }

        void SamplePoints(OrientedBox wsBox, NativeArray<float3> points, int startIndex);

    }

    [Serializable]
    public class CenterSampler : IProxySampler {

        public int GetPointCount() {
            return 1;
        }

        public void SamplePoints(OrientedBox wsBox, NativeArray<float3> points, int startIndex) {
            points[startIndex] = wsBox.center;
        }

    }

    [Serializable]
    public class BoxSampler : IProxySampler {

        public int GetPointCount() {
            return 1 + 6 + 8 + 12;
        }

        public int GetTier0PointCount() {
            return 1;
        }

        public int GetTier1PointCount() {
            return 1 + 6;
        }

        public int GetTier2PointCount() {
            return 1 + 6 + 8;
        }

        public void SamplePoints(OrientedBox wsBox, NativeArray<float3> points, int startIndex) {
            var halfExtents = wsBox.halfExtent;

            points[startIndex] = wsBox.center;

            // Sample Plane Centers
            points[startIndex + 1] = wsBox.TransformPoint(new float3(halfExtents.x, 0f, 0f));
            points[startIndex + 2] = wsBox.TransformPoint(new float3(-halfExtents.x, 0f, 0f));
            points[startIndex + 3] = wsBox.TransformPoint(new float3(0f, 0f, halfExtents.z));
            points[startIndex + 4] = wsBox.TransformPoint(new float3(0f, 0f, -halfExtents.z));
            points[startIndex + 5] = wsBox.TransformPoint(new float3(0f, halfExtents.y, 0f));
            points[startIndex + 6] = wsBox.TransformPoint(new float3(0f, -halfExtents.y, 0f));

            // Sample Corners
            points[startIndex + 7] = wsBox.TransformPoint(new float3(-halfExtents.x, -halfExtents.y, -halfExtents.z));
            points[startIndex + 8] = wsBox.TransformPoint(new float3(halfExtents.x, -halfExtents.y, -halfExtents.z));
            points[startIndex + 9] = wsBox.TransformPoint(new float3(-halfExtents.x, halfExtents.y, -halfExtents.z));
            points[startIndex + 10] = wsBox.TransformPoint(new float3(halfExtents.x, halfExtents.y, -halfExtents.z));
            points[startIndex + 11] = wsBox.TransformPoint(new float3(-halfExtents.x, -halfExtents.y, halfExtents.z));
            points[startIndex + 12] = wsBox.TransformPoint(new float3(halfExtents.x, -halfExtents.y, halfExtents.z));
            points[startIndex + 13] = wsBox.TransformPoint(new float3(-halfExtents.x, halfExtents.y, halfExtents.z));
            points[startIndex + 14] = wsBox.TransformPoint(new float3(halfExtents.x, halfExtents.y, halfExtents.z));

            // Sample Edges
            points[startIndex + 15] = wsBox.TransformPoint(new float3(-halfExtents.x, 0f, -halfExtents.z));
            points[startIndex + 16] = wsBox.TransformPoint(new float3(halfExtents.x, 0f, -halfExtents.z));
            points[startIndex + 17] = wsBox.TransformPoint(new float3(-halfExtents.x, 0f, halfExtents.z));
            points[startIndex + 18] = wsBox.TransformPoint(new float3(halfExtents.x, 0f, halfExtents.z));

            points[startIndex + 19] = wsBox.TransformPoint(new float3(0f, -halfExtents.y, -halfExtents.z));
            points[startIndex + 20] = wsBox.TransformPoint(new float3(0f, halfExtents.y, -halfExtents.z));
            points[startIndex + 21] = wsBox.TransformPoint(new float3(0f, -halfExtents.y, halfExtents.z));
            points[startIndex + 22] = wsBox.TransformPoint(new float3(0f, halfExtents.y, halfExtents.z));

            points[startIndex + 23] = wsBox.TransformPoint(new float3(-halfExtents.x, -halfExtents.y, 0f));
            points[startIndex + 24] = wsBox.TransformPoint(new float3(halfExtents.x, -halfExtents.y, 0f));
            points[startIndex + 25] = wsBox.TransformPoint(new float3(-halfExtents.x, halfExtents.y, 0f));
            points[startIndex + 26] = wsBox.TransformPoint(new float3(halfExtents.x, halfExtents.y, 0f));
        }

    }

    [Serializable]
    public class PlaneSampler : IProxySampler {

        [FormerlySerializedAs("sampleCounts")]
        public int3 subdivisions = new(1, 1, 1);

        public int GetPointCount() {
            // Box structure: 1 center + 6 face centers + 8 corners + 12 edge centers
            const int boxPoints = 1 + 6 + 8 + 12;

            var xSamples = math.max(1, subdivisions.x);
            var ySamples = math.max(1, subdivisions.y);
            var zSamples = math.max(1, subdivisions.z);

            // Each face is subdivided into 2x2 grid (4 subdivided faces)
            // ±X faces: sampled with ySamples * zSamples points
            // ±Y faces: sampled with xSamples * zSamples points
            // ±Z faces: sampled with xSamples * ySamples points
            var xFacePoints = 2 * 4 * ySamples * zSamples;
            var yFacePoints = 2 * 4 * xSamples * zSamples;
            var zFacePoints = 2 * 4 * xSamples * ySamples;

            return boxPoints + xFacePoints + yFacePoints + zFacePoints;
        }

        public int GetTier0PointCount() {
            return 1;
        }

        public int GetTier1PointCount() {
            return 1 + 6;
        }

        public int GetTier2PointCount() {
            return 1 + 6 + 8 + 12;
        }

        public void SamplePoints(OrientedBox wsBox, NativeArray<float3> points, int startIndex) {
            var halfExtents = wsBox.halfExtent;
            var index = startIndex;

            // First, sample the box structure (center, face centers, corners, edge centers)
            // Center
            points[index++] = wsBox.center;

            // Face Centers
            points[index++] = wsBox.TransformPoint(new float3(halfExtents.x, 0f, 0f));
            points[index++] = wsBox.TransformPoint(new float3(-halfExtents.x, 0f, 0f));
            points[index++] = wsBox.TransformPoint(new float3(0f, 0f, halfExtents.z));
            points[index++] = wsBox.TransformPoint(new float3(0f, 0f, -halfExtents.z));
            points[index++] = wsBox.TransformPoint(new float3(0f, halfExtents.y, 0f));
            points[index++] = wsBox.TransformPoint(new float3(0f, -halfExtents.y, 0f));

            // Corners
            points[index++] = wsBox.TransformPoint(new float3(-halfExtents.x, -halfExtents.y, -halfExtents.z));
            points[index++] = wsBox.TransformPoint(new float3(halfExtents.x, -halfExtents.y, -halfExtents.z));
            points[index++] = wsBox.TransformPoint(new float3(-halfExtents.x, halfExtents.y, -halfExtents.z));
            points[index++] = wsBox.TransformPoint(new float3(halfExtents.x, halfExtents.y, -halfExtents.z));
            points[index++] = wsBox.TransformPoint(new float3(-halfExtents.x, -halfExtents.y, halfExtents.z));
            points[index++] = wsBox.TransformPoint(new float3(halfExtents.x, -halfExtents.y, halfExtents.z));
            points[index++] = wsBox.TransformPoint(new float3(-halfExtents.x, halfExtents.y, halfExtents.z));
            points[index++] = wsBox.TransformPoint(new float3(halfExtents.x, halfExtents.y, halfExtents.z));

            // Edge Centers
            points[index++] = wsBox.TransformPoint(new float3(-halfExtents.x, 0f, -halfExtents.z));
            points[index++] = wsBox.TransformPoint(new float3(halfExtents.x, 0f, -halfExtents.z));
            points[index++] = wsBox.TransformPoint(new float3(-halfExtents.x, 0f, halfExtents.z));
            points[index++] = wsBox.TransformPoint(new float3(halfExtents.x, 0f, halfExtents.z));
            points[index++] = wsBox.TransformPoint(new float3(0f, -halfExtents.y, -halfExtents.z));
            points[index++] = wsBox.TransformPoint(new float3(0f, halfExtents.y, -halfExtents.z));
            points[index++] = wsBox.TransformPoint(new float3(0f, -halfExtents.y, halfExtents.z));
            points[index++] = wsBox.TransformPoint(new float3(0f, halfExtents.y, halfExtents.z));
            points[index++] = wsBox.TransformPoint(new float3(-halfExtents.x, -halfExtents.y, 0f));
            points[index++] = wsBox.TransformPoint(new float3(halfExtents.x, -halfExtents.y, 0f));
            points[index++] = wsBox.TransformPoint(new float3(-halfExtents.x, halfExtents.y, 0f));
            points[index++] = wsBox.TransformPoint(new float3(halfExtents.x, halfExtents.y, 0f));

            // Now sample subdivided faces with axis-specific sample counts
            var xSamples = math.max(1, subdivisions.x);
            var ySamples = math.max(1, subdivisions.y);
            var zSamples = math.max(1, subdivisions.z);

            // ±X faces: samples along Y (ySamples) and Z (zSamples)
            SampleSubdividedFace(
                wsBox,
                halfExtents,
                1f,
                0f,
                0f,
                0f,
                1f,
                0f,
                0f,
                0f,
                1f,
                points,
                ref index,
                ySamples,
                zSamples
            );
            SampleSubdividedFace(
                wsBox,
                halfExtents,
                -1f,
                0f,
                0f,
                0f,
                1f,
                0f,
                0f,
                0f,
                1f,
                points,
                ref index,
                ySamples,
                zSamples
            );

            // ±Y faces: samples along X (xSamples) and Z (zSamples)
            SampleSubdividedFace(
                wsBox,
                halfExtents,
                0f,
                1f,
                0f,
                1f,
                0f,
                0f,
                0f,
                0f,
                1f,
                points,
                ref index,
                xSamples,
                zSamples
            );
            SampleSubdividedFace(
                wsBox,
                halfExtents,
                0f,
                -1f,
                0f,
                1f,
                0f,
                0f,
                0f,
                0f,
                1f,
                points,
                ref index,
                xSamples,
                zSamples
            );

            // ±Z faces: samples along X (xSamples) and Y (ySamples)
            SampleSubdividedFace(
                wsBox,
                halfExtents,
                0f,
                0f,
                1f,
                1f,
                0f,
                0f,
                0f,
                1f,
                0f,
                points,
                ref index,
                xSamples,
                ySamples
            );
            SampleSubdividedFace(
                wsBox,
                halfExtents,
                0f,
                0f,
                -1f,
                1f,
                0f,
                0f,
                0f,
                1f,
                0f,
                points,
                ref index,
                xSamples,
                ySamples
            );
        }

        private void SampleSubdividedFace(
            OrientedBox wsBox,
            float3 halfExtents,
            float normalX,
            float normalY,
            float normalZ,
            float rightX,
            float rightY,
            float rightZ,
            float upX,
            float upY,
            float upZ,
            NativeArray<float3> points,
            ref int index,
            int rightSamples,
            int upSamples
        ) {
            var normal = new float3(normalX, normalY, normalZ);
            var right = new float3(rightX, rightY, rightZ);
            var up = new float3(upX, upY, upZ);

            var normalScale = math.dot(halfExtents, math.abs(normal));
            var rightScale = math.dot(halfExtents, math.abs(right));
            var upScale = math.dot(halfExtents, math.abs(up));

            // Subdivide the face into 2x2 grid (4 smaller faces)
            for (var faceY = 0; faceY < 2; faceY++) {
                for (var faceX = 0; faceX < 2; faceX++) {
                    // Calculate bounds of this subdivided face in range [-1, 1]
                    var minU = faceX - 1f;
                    var maxU = faceX;
                    var minV = faceY - 1f;
                    var maxV = faceY;

                    // Sample within this subdivided face with spacing to avoid edges
                    for (var i = 0; i < rightSamples; i++) {
                        for (var j = 0; j < upSamples; j++) {
                            // Spacing: for n samples, place them at (1/(n+1)), (2/(n+1)), ..., (n/(n+1))
                            // This keeps them away from edges (0 and 1)
                            var u = (float)(i + 1) / (rightSamples + 1);
                            var v = (float)(j + 1) / (upSamples + 1);

                            // Lerp within the subdivided face bounds
                            var localU = math.lerp(minU, maxU, u);
                            var localV = math.lerp(minV, maxV, v);

                            var localPos = normal * normalScale +
                                           right * localU * rightScale +
                                           up * localV * upScale;

                            points[index++] = wsBox.TransformPoint(localPos);
                        }
                    }
                }
            }
        }

    }
}