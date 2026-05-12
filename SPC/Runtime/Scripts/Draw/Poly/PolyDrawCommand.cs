using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;

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
}