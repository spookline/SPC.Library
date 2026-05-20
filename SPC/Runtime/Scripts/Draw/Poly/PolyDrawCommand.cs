using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Draw {
    [StructLayout(LayoutKind.Sequential)]
    public struct PolyDrawCommand {

        public float4x4 matrix;
        public float4 color;
        public float3 args;
        public PolyDrawCommandType type;
        public ushort flags;

    }

    public enum PolyDrawCommandType : ushort {

        None = 0,

        Triangle = 1, // matrix.c0.xyz, matrix.c1.xyz, matrix.c2.xyz, args normal
        Quad = 2, // matrix.c0.xyz, matrix.c1.xyz, matrix.c2.xyz, matrix.c3.xyz, args normal

        Cube = 10, // matrix
        Sphere = 11, // matrix -> center; args.x radius, args.y segments
        Disc = 12, // matrix -> center/orientation; args.x radius, args.y segments
        Arc = 13, // matrix -> oriented center; args.x radius, args.y segments, args.z angle degrees

    }

    [Flags]
    public enum PolyDrawCommandFlags : ushort {

        None = 0,

        Wire = 1 << 0,

    }

    public static class PolyDrawCommandBitmask {

        public const uint TypeMask = 0x0000_FFFFu;

        public static uint Pack(PolyDrawCommandType type, PolyDrawCommandFlags flags = PolyDrawCommandFlags.None) {
            return ((uint)type & TypeMask) | ((uint)flags << 16);
        }

        public static PolyDrawCommandType GetType(uint typeAndFlags) {
            return (PolyDrawCommandType)(typeAndFlags & TypeMask);
        }

        public static PolyDrawCommandFlags GetFlags(uint typeAndFlags) {
            return (PolyDrawCommandFlags)(typeAndFlags >> 16);
        }

        public static bool HasFlag(ushort type, PolyDrawCommandFlags flag) {
            return (type & (ushort)flag) != 0;
        }

    }

    public static class PolyDrawShaderFlags {

        public const float DoubleSided = 1f;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PolyDrawVertex {

        public float3 position;
        public float3 normal;
        public float4 color;
        public float primitiveFlags;

        public PolyDrawVertex(float3 position, float4 color) {
            this.position = position;
            normal = float3.zero;
            this.color = color;
            primitiveFlags = 0f;
        }

        public PolyDrawVertex(float3 position, float4 color, float3 normal) {
            this.position = position;
            this.normal = normal;
            this.color = color;
            primitiveFlags = 0f;
        }

        public PolyDrawVertex(float3 position, float4 color, float3 normal, float primitiveFlags) {
            this.position = position;
            this.normal = normal;
            this.color = color;
            this.primitiveFlags = primitiveFlags;
        }

    }


    public struct PolyDrawBuffer : IDisposable {

        public NativeArray<PolyDrawVertex> vertices;
        public NativeArray<int> indices;
        public AffineTransform transform;
        public float4 color;

        public static Color KeepColor = new(-1f, -1f, -1f, -1f);


        public static PolyDrawBuffer From(Vector3[] vertices, int[] indices, Color color, bool doubleSided = false) {
            var buffer = new PolyDrawBuffer {
                vertices = new NativeArray<PolyDrawVertex>(vertices.Length, Allocator.Persistent),
                indices = new NativeArray<int>(indices, Allocator.Persistent),
                color = PolyDrawCommandFactory.Color(color),
                transform = AffineTransform.identity
            };

            var basis = new PolyDrawVertex {
                color = PolyDrawCommandFactory.Color(color),
                primitiveFlags = doubleSided ? PolyDrawShaderFlags.DoubleSided : 0f
            };
            for (var i = 0; i < vertices.Length; i++) {
                var v = basis;
                v.position = vertices[i];
                buffer.vertices[i] = v;
            }

            return buffer;
        }

        public static PolyDrawBuffer FromMesh(Mesh mesh, Color color, bool doubleSided = false) {
            var vertices = mesh.vertices;
            var indices = mesh.triangles;
            var normals = mesh.normals;

            var buffer = new PolyDrawBuffer {
                vertices = new NativeArray<PolyDrawVertex>(vertices.Length, Allocator.Persistent),
                indices = new NativeArray<int>(indices, Allocator.Persistent),
                color = new float4(-1f),
                transform = AffineTransform.identity
            };

            var basis = new PolyDrawVertex {
                color = PolyDrawCommandFactory.Color(color),
                primitiveFlags = doubleSided ? PolyDrawShaderFlags.DoubleSided : 0f
            };
            for (var i = 0; i < vertices.Length; i++) {
                var v = basis;
                v.position = vertices[i];
                v.normal = normals[i];
                buffer.vertices[i] = v;
            }

            return buffer;
        }

        public static PolyDrawBuffer FromMeshWire(Mesh mesh, Color color) {
            var vertices = mesh.vertices;
            var indices = mesh.triangles;
            var buffer = new PolyDrawBuffer {
                vertices = new NativeArray<PolyDrawVertex>(vertices.Length, Allocator.Persistent),
                indices = new NativeArray<int>(indices.Length * 2, Allocator.Persistent),
                color = new float4(-1f),
                transform = AffineTransform.identity
            };

            for (var i = 0; i < vertices.Length; i++) {
                var v = new PolyDrawVertex {
                    position = vertices[i],
                    color = PolyDrawCommandFactory.Color(color),
                    primitiveFlags = PolyDrawShaderFlags.DoubleSided
                };
                buffer.vertices[i] = v;
            }

            var offset = 0;
            for (var i = 0; i < indices.Length; i += 3) {
                var i0 = indices[i];
                var i1 = indices[i + 1];
                var i2 = indices[i + 2];

                buffer.indices[offset++] = i0;
                buffer.indices[offset++] = i1;

                buffer.indices[offset++] = i1;
                buffer.indices[offset++] = i2;

                buffer.indices[offset++] = i2;
                buffer.indices[offset++] = i0;
            }

            return buffer;
        }

        public static PolyDrawBuffer FromMeshVertexColors(Mesh mesh, bool doubleSided = false) {
            var vertices = mesh.vertices;
            var indices = mesh.triangles;
            var normals = mesh.normals;
            var colors = mesh.colors;

            var buffer = new PolyDrawBuffer {
                vertices = new NativeArray<PolyDrawVertex>(vertices.Length, Allocator.Persistent),
                indices = new NativeArray<int>(indices, Allocator.Persistent),
                color = new float4(-1f),
                transform = AffineTransform.identity
            };

            for (var i = 0; i < vertices.Length; i++) {
                var v = new PolyDrawVertex {
                    position = vertices[i],
                    normal = normals[i],
                    color = PolyDrawCommandFactory.Color(colors[i]),
                    primitiveFlags = doubleSided ? PolyDrawShaderFlags.DoubleSided : 0f
                };
                buffer.vertices[i] = v;
            }

            return buffer;
        }

        public void Dispose() {
            if (vertices.IsCreated) vertices.Dispose();
            if (indices.IsCreated) indices.Dispose();
        }

        public bool IsCreated => vertices.IsCreated && indices.IsCreated;
    }
}