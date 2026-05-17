using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Spookline.SPC.Draw {
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public abstract class PolyDrawCommandRenderer : MonoBehaviour {

        [SerializeField]
        public Material material;

        [SerializeField]
        public bool rebuildEveryFrame = true;

        [SerializeField]
        public double refreshIntervalMs = 16.66f;

        public bool skipRefresh = false;

        private NativeList<PolyDrawCommand> _commands;
        private NativeList<PolyDrawVertex> _directLineVertices;

        private Mesh _mesh;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Material _fallbackMaterial;
        private DateTime _lastRefreshTime;

        private static readonly VertexAttributeDescriptor[] _bufferDescriptor = {
            new(VertexAttribute.Position),
            new(VertexAttribute.Normal),
            new(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
            new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 1)
        };

        private static readonly MeshUpdateFlags _meshUpdateFlags =
            MeshUpdateFlags.DontValidateIndices |
            MeshUpdateFlags.DontRecalculateBounds |
            MeshUpdateFlags.DontResetBoneBounds |
            MeshUpdateFlags.DontNotifyMeshUsers;

        protected virtual Allocator CommandAllocator => Allocator.Persistent;

        protected virtual void OnEnable() {
            EnsureCreated();
            RebuildCommandsAndMesh();
        }

        protected virtual void OnDisable() {
            DisposeNativeData();
            DestroyOwnedRenderingObjects();
        }

        protected virtual void OnDestroy() {
            DisposeNativeData();
            DestroyOwnedRenderingObjects();
        }

        protected virtual void Update() {
            if (rebuildEveryFrame) RebuildCommandsAndMesh();
        }

#if UNITY_EDITOR
        protected virtual void OnValidate() {
            if (!isActiveAndEnabled) return;

            EnsureCreated();
            ApplyRendererSettings();

            if (!Application.isPlaying && rebuildEveryFrame) RebuildCommandsAndMesh();
        }

        protected virtual void OnDrawGizmos() {
            if (!Application.isPlaying && rebuildEveryFrame) RebuildCommandsAndMesh();
        }
#endif


        public void RebuildCommandsAndMesh() {
            EnsureCreated();
            _commands.Clear();
            _directLineVertices.Clear();

            var offscreenFrame = false;
            if (!skipRefresh) {
                var currentTimeMillis = math.abs((DateTime.Now - _lastRefreshTime).TotalMilliseconds);
                offscreenFrame = refreshIntervalMs > 0 && currentTimeMillis < refreshIntervalMs;
            }

            var primitiveWriter = new PolyDrawCommandWriter(_commands);
            var lineWriter = new PolyDrawLineWriter(_directLineVertices);
            BuildCommands(ref primitiveWriter, ref lineWriter, offscreenFrame);

            if (offscreenFrame) return;

            ApplyCommandsToMesh();
            if (!skipRefresh) _lastRefreshTime = DateTime.Now;
        }

        protected abstract void BuildCommands(
            ref PolyDrawCommandWriter writer,
            ref PolyDrawLineWriter lines,
            bool isOffscreenFrame
        );


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private NativeArray<T> GetCommandArray<T>() where T : struct {
            var count = _commands.Length;
            return count > 0
                ? new NativeArray<T>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory)
                : default;
        }

        private void ApplyCommandsToMesh() {
            EnsureCreated();

            var commandCount = _commands.Length;
            var lineVertexCount = _directLineVertices.Length;

            if (commandCount == 0 && lineVertexCount == 0) {
                _mesh.Clear();
                return;
            }

            var primitiveSizes = GetCommandArray<int2>();
            var meshVertexOffsets = GetCommandArray<int>();
            var meshVertexIndices = GetCommandArray<int>();
            var wireSizes = GetCommandArray<int>();
            var wireVertexOffsets = GetCommandArray<int>();

            var meshVertexCountRef = commandCount > 0 ? new NativeReference<int>(Allocator.TempJob) : default;
            var meshIndexCountRef = commandCount > 0 ? new NativeReference<int>(Allocator.TempJob) : default;
            var wireVertexCountRef = commandCount > 0 ? new NativeReference<int>(Allocator.TempJob) : default;

            try {
                var meshVertexCount = 0;
                var meshIndexCount = 0;
                var wireVertexCount = 0;

                if (commandCount > 0) {
                    var primitiveSizeJob = new PolyDrawMeshMeasureJob {
                        commands = _commands.AsArray(),
                        sizes = primitiveSizes
                    };

                    var wireSizeJob = new PolyDrawWireMeasureJob {
                        commands = _commands.AsArray(),
                        vertexCounts = wireSizes
                    };

                    var primitiveSizeHandle = primitiveSizeJob.Schedule(commandCount, 64);
                    var wireSizeHandle = wireSizeJob.Schedule(commandCount, 64);

                    var offsetScanHandle = new CombinedOffsetScanJob {
                        primitiveSizes = primitiveSizes,
                        wireSizes = wireSizes,
                        meshVertexOffsets = meshVertexOffsets,
                        meshIndexOffsets = meshVertexIndices,
                        meshVertexCount = meshVertexCountRef,
                        meshIndexCount = meshIndexCountRef,
                        wireVertexOffsets = wireVertexOffsets,
                        wireVertexCount = wireVertexCountRef,
                    }.Schedule(JobHandle.CombineDependencies(primitiveSizeHandle, wireSizeHandle));

                    offsetScanHandle.Complete();

                    meshVertexCount = meshVertexCountRef.Value;
                    meshIndexCount = meshIndexCountRef.Value;
                    wireVertexCount = wireVertexCountRef.Value;
                }

                var totalLineVertexCount = wireVertexCount + lineVertexCount;
                var totalVertexCount = meshVertexCount + totalLineVertexCount;
                var totalIndexCount = meshIndexCount + totalLineVertexCount;

                if (totalVertexCount == 0 || totalIndexCount == 0) {
                    _mesh.Clear();
                    return;
                }

                var vertices = new NativeArray<PolyDrawVertex>(
                    totalVertexCount,
                    Allocator.TempJob,
                    NativeArrayOptions.UninitializedMemory
                );

                var indices = new NativeArray<int>(
                    totalIndexCount,
                    Allocator.TempJob,
                    NativeArrayOptions.UninitializedMemory
                );

                try {
                    var commandsArray = _commands.AsArray();
                    if (meshVertexCount > 0 && meshIndexCount > 0) {
                        new PolyDrawMeshGeneratorJob {
                            commands = commandsArray,
                            vertexOffsets = meshVertexOffsets,
                            indexOffsets = meshVertexIndices,
                            vertices = vertices,
                            indices = indices
                        }.Schedule(commandCount, 32).Complete();
                    }

                    if (wireVertexCount > 0) {
                        var wireVertices = vertices.GetSubArray(
                            meshVertexCount,
                            wireVertexCount
                        );

                        new PolyDrawWireMeshGeneratorJob {
                            commands = commandsArray,
                            vertexOffsets = wireVertexOffsets,
                            vertices = wireVertices
                        }.Schedule(commandCount, 32).Complete();
                    }

                    // I just can't run those two jobs in parallel, I tried and failed multiple times, this must do.

                    new LineCopyAndIndexWriteJob {
                        source = _directLineVertices.AsArray(),
                        vertices = vertices,
                        indices = indices,

                        indexBase = meshIndexCount,
                        vertexBase = meshVertexCount,

                        totalLineVertexCount = totalLineVertexCount,
                        directLineDestinationOffset = meshVertexCount + wireVertexCount
                    }.Run();

                    UploadMesh(
                        vertices,
                        indices,
                        meshVertexCount,
                        meshIndexCount,
                        totalLineVertexCount,
                        totalVertexCount,
                        totalIndexCount
                    );
                } finally {
                    vertices.Dispose();
                    indices.Dispose();
                }
            } finally {
                primitiveSizes.Dispose();
                meshVertexOffsets.Dispose();
                meshVertexIndices.Dispose();
                wireSizes.Dispose();
                wireVertexOffsets.Dispose();
                meshVertexCountRef.Dispose();
                meshIndexCountRef.Dispose();
                wireVertexCountRef.Dispose();
            }
        }

        private void UploadMesh(
            NativeArray<PolyDrawVertex> vertices,
            NativeArray<int> indices,
            int triangleVertexCount,
            int triangleIndexCount,
            int lineVertexCount,
            int totalVertexCount,
            int totalIndexCount
        ) {
            _mesh.Clear();
            _mesh.SetVertexBufferParams(totalVertexCount, _bufferDescriptor);
            _mesh.SetIndexBufferParams(totalIndexCount, IndexFormat.UInt32);

            _mesh.SetVertexBufferData(
                vertices,
                0,
                0,
                totalVertexCount,
                0,
                _meshUpdateFlags
            );

            _mesh.SetIndexBufferData(
                indices,
                0,
                0,
                totalIndexCount,
                _meshUpdateFlags
            );

            var subMeshCount = 0;
            if (triangleIndexCount > 0) subMeshCount++;
            if (lineVertexCount > 0) subMeshCount++;
            _mesh.subMeshCount = subMeshCount;

            var subMeshIndex = 0;
            if (triangleIndexCount > 0) {
                _mesh.SetSubMesh(
                    subMeshIndex,
                    new SubMeshDescriptor(0, triangleIndexCount) {
                        firstVertex = 0,
                        vertexCount = triangleVertexCount
                    },
                    _meshUpdateFlags
                );

                subMeshIndex++;
            }

            if (lineVertexCount > 0) {
                _mesh.SetSubMesh(
                    subMeshIndex,
                    new SubMeshDescriptor(triangleIndexCount, lineVertexCount, MeshTopology.Lines) {
                        firstVertex = triangleVertexCount,
                        vertexCount = lineVertexCount
                    },
                    _meshUpdateFlags
                );
            }

            _mesh.bounds = new Bounds(float3.zero, new float3(float.MaxValue));
        }

        private void EnsureCreated() {
            if (!_commands.IsCreated)
                _commands = new NativeList<PolyDrawCommand>(64, CommandAllocator);
            if (!_directLineVertices.IsCreated)
                _directLineVertices = new NativeList<PolyDrawVertex>(128, CommandAllocator);

            if (!_meshFilter) _meshFilter = GetComponent<MeshFilter>();
            if (!_meshRenderer) _meshRenderer = GetComponent<MeshRenderer>();

            if (!_mesh) {
                _mesh = new Mesh {
                    name = $"{GetType().Name} Mesh",
                    indexFormat = IndexFormat.UInt32,
                    hideFlags = HideFlags.DontSave
                };

                _mesh.MarkDynamic();
            }

            if (_meshFilter.sharedMesh != _mesh) _meshFilter.sharedMesh = _mesh;
            ApplyRendererSettings();
        }

        private void ApplyRendererSettings() {
            if (!_meshRenderer) return;

            _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;

            var resolvedMaterial = material ? material : GetFallbackMaterial();

            if (resolvedMaterial && _meshRenderer.sharedMaterial != resolvedMaterial)
                _meshRenderer.sharedMaterials = new[] { resolvedMaterial, resolvedMaterial };
        }

        private Material GetFallbackMaterial() {
            if (_fallbackMaterial) return _fallbackMaterial;

            _fallbackMaterial = Resources.Load<Material>("PolyDrawURP");
            if (_fallbackMaterial) {
                _fallbackMaterial = new Material(_fallbackMaterial);
                return _fallbackMaterial;
            }

            var shader = Shader.Find("Sprites/Default") ??
                         Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Unlit/Color") ??
                         Shader.Find("Standard");

            if (!shader) return null;

            _fallbackMaterial = new Material(shader) {
                name = "Primitive Draw Fallback Material",
                hideFlags = HideFlags.DontSave
            };

            return _fallbackMaterial;
        }

        private void DisposeNativeData() {
            if (_commands.IsCreated) _commands.Dispose();
            if (_directLineVertices.IsCreated) _directLineVertices.Dispose();
        }

        private void DestroyOwnedRenderingObjects() {
            if (_meshFilter != null && _meshFilter.sharedMesh == _mesh)
                _meshFilter.sharedMesh = null;

            DestroyUnityObject(_mesh);
            DestroyUnityObject(_fallbackMaterial);

            _mesh = null;
            _fallbackMaterial = null;
        }

        private static void DestroyUnityObject(Object obj) {
            if (!obj) return;
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(obj, false);
            else Destroy(obj);
#else
            Destroy(obj);
#endif
        }

        [BurstCompile]
        private struct CombinedOffsetScanJob : IJob {

            [ReadOnly]
            public NativeArray<int2> primitiveSizes;
            [ReadOnly]
            public NativeArray<int> wireSizes;

            public NativeArray<int> meshVertexOffsets;
            public NativeArray<int> meshIndexOffsets;
            public NativeArray<int> wireVertexOffsets;

            public NativeReference<int> meshVertexCount;
            public NativeReference<int> meshIndexCount;
            public NativeReference<int> wireVertexCount;

            public void Execute() {
                var tv = 0;
                var ti = 0;
                var wv = 0;

                for (var i = 0; i < primitiveSizes.Length; i++) {
                    meshVertexOffsets[i] = tv;
                    meshIndexOffsets[i] = ti;
                    wireVertexOffsets[i] = wv;

                    var primitiveSize = primitiveSizes[i];

                    tv += primitiveSize.x;
                    ti += primitiveSize.y;
                    wv += wireSizes[i];
                }

                meshVertexCount.Value = tv;
                meshIndexCount.Value = ti;
                wireVertexCount.Value = wv;
            }

        }

        [BurstCompile]
        private struct LineCopyAndIndexWriteJob : IJob {

            [ReadOnly]
            public NativeArray<PolyDrawVertex> source;

            public NativeArray<PolyDrawVertex> vertices;
            public NativeArray<int> indices;

            public int indexBase;
            public int vertexBase;

            public int totalLineVertexCount;
            public int directLineDestinationOffset;

            public void Execute() {
                for (var i = 0; i < totalLineVertexCount; i++)
                    indices[indexBase + i] = vertexBase + i;

                for (var i = 0; i < source.Length; i++)
                    vertices[directLineDestinationOffset + i] = source[i];
            }

        }

    }
}