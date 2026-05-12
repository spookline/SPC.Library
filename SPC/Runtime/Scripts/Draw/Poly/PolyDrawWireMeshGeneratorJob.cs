using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Spookline.SPC.Draw {
    [BurstCompile]
    public struct PolyDrawWireMeshGeneratorJob : IJobParallelFor {

        [ReadOnly]
        public NativeArray<PolyDrawCommand> commands;

        [ReadOnly]
        public NativeArray<int> vertexOffsets;

        [NativeDisableParallelForRestriction, WriteOnly]
        public NativeArray<PolyDrawVertex> vertices;

        public void Execute(int commandIndex) {
            var cmd = commands[commandIndex];
            if (!PolyDrawCommandBitmask.HasFlag(cmd.flags, PolyDrawCommandFlags.Wire)) return;
            var vertexBase = vertexOffsets[commandIndex];

            switch (cmd.type) {
                case PolyDrawCommandType.Triangle:
                    WriteWireTriangle(cmd, vertexBase);
                    break;

                case PolyDrawCommandType.Quad:
                    WriteWireQuad(cmd, vertexBase);
                    break;

                case PolyDrawCommandType.Cube:
                    WriteWireCube(cmd, vertexBase);
                    break;

                case PolyDrawCommandType.Disc:
                    WriteWireDisc(cmd, vertexBase);
                    break;

                case PolyDrawCommandType.Arc:
                    WriteWireArc(cmd, vertexBase);
                    break;

                case PolyDrawCommandType.Sphere:
                    WriteWireSphere(cmd, vertexBase);
                    break;
            }
        }

        private void WriteWireTriangle(PolyDrawCommand cmd, int vertexBase) {
            var a = cmd.matrix.c0.xyz;
            var b = cmd.matrix.c1.xyz;
            var c = cmd.matrix.c2.xyz;

            var normal = math.normalizesafe(math.cross(b - a, c - a), new float3(0f, 1f, 0f));

            var i = vertexBase;
            WriteLine(ref i, a, b, cmd.color, normal);
            WriteLine(ref i, b, c, cmd.color, normal);
            WriteLine(ref i, c, a, cmd.color, normal);
        }

        private void WriteWireQuad(PolyDrawCommand cmd, int vertexBase) {
            var a = cmd.matrix.c0.xyz;
            var b = cmd.matrix.c1.xyz;
            var c = cmd.matrix.c2.xyz;
            var d = cmd.matrix.c3.xyz;

            var normal = math.normalizesafe(math.cross(b - a, c - a), new float3(0f, 1f, 0f));

            var i = vertexBase;
            WriteLine(ref i, a, b, cmd.color, normal);
            WriteLine(ref i, b, c, cmd.color, normal);
            WriteLine(ref i, c, d, cmd.color, normal);
            WriteLine(ref i, d, a, cmd.color, normal);
        }

        private void WriteWireCube(PolyDrawCommand cmd, int vertexBase) {
            var p0 = Transform(cmd.matrix, new float3(-0.5f, -0.5f, -0.5f));
            var p1 = Transform(cmd.matrix, new float3(+0.5f, -0.5f, -0.5f));
            var p2 = Transform(cmd.matrix, new float3(+0.5f, +0.5f, -0.5f));
            var p3 = Transform(cmd.matrix, new float3(-0.5f, +0.5f, -0.5f));

            var p4 = Transform(cmd.matrix, new float3(-0.5f, -0.5f, +0.5f));
            var p5 = Transform(cmd.matrix, new float3(+0.5f, -0.5f, +0.5f));
            var p6 = Transform(cmd.matrix, new float3(+0.5f, +0.5f, +0.5f));
            var p7 = Transform(cmd.matrix, new float3(-0.5f, +0.5f, +0.5f));

            var normal = TransformNormal(cmd.matrix, new float3(0f, 0f, 1f));

            var i = vertexBase;

            WriteLine(ref i, p0, p1, cmd.color, normal);
            WriteLine(ref i, p1, p2, cmd.color, normal);
            WriteLine(ref i, p2, p3, cmd.color, normal);
            WriteLine(ref i, p3, p0, cmd.color, normal);

            WriteLine(ref i, p4, p5, cmd.color, normal);
            WriteLine(ref i, p5, p6, cmd.color, normal);
            WriteLine(ref i, p6, p7, cmd.color, normal);
            WriteLine(ref i, p7, p4, cmd.color, normal);

            WriteLine(ref i, p0, p4, cmd.color, normal);
            WriteLine(ref i, p1, p5, cmd.color, normal);
            WriteLine(ref i, p2, p6, cmd.color, normal);
            WriteLine(ref i, p3, p7, cmd.color, normal);
        }

        private void WriteWireDisc(PolyDrawCommand cmd, int vertexBase) {
            var radius = math.max(0f, cmd.args.x);
            var segments = ResolveSegments(cmd.args.y, 32, 3);
            var normal = TransformNormal(cmd.matrix, new float3(0f, 0f, 1f));

            var i = vertexBase;

            for (var segment = 0; segment < segments; segment++) {
                var t0 = (float)segment / segments;
                var t1 = (float)(segment + 1) / segments;

                var a0 = t0 * math.PI * 2f;
                var a1 = t1 * math.PI * 2f;

                var p0 = new float3(math.cos(a0) * radius, math.sin(a0) * radius, 0f);
                var p1 = new float3(math.cos(a1) * radius, math.sin(a1) * radius, 0f);

                WriteLine(ref i, Transform(cmd.matrix, p0), Transform(cmd.matrix, p1), cmd.color, normal);
            }
        }

        private void WriteWireArc(PolyDrawCommand cmd, int vertexBase) {
            var radius = math.max(0f, cmd.args.x);
            var segments = ResolveSegments(cmd.args.y, 32, 1);
            var angleRadians = math.radians(cmd.args.z);
            var normal = TransformNormal(cmd.matrix, new float3(0f, 0f, 1f));

            var i = vertexBase;

            for (var segment = 0; segment < segments; segment++) {
                var t0 = (float)segment / segments;
                var t1 = (float)(segment + 1) / segments;

                var a0 = t0 * angleRadians;
                var a1 = t1 * angleRadians;

                var p0 = new float3(math.cos(a0) * radius, math.sin(a0) * radius, 0f);
                var p1 = new float3(math.cos(a1) * radius, math.sin(a1) * radius, 0f);

                WriteLine(ref i, Transform(cmd.matrix, p0), Transform(cmd.matrix, p1), cmd.color, normal);
            }
        }

        private void WriteWireSphere(PolyDrawCommand cmd, int vertexBase) {
            var radius = math.max(0f, cmd.args.x);
            var segments = ResolveSegments(cmd.args.y, 16, 3);
            var rings = math.max(2, segments / 2);

            var i = vertexBase;

            for (var ring = 0; ring <= rings; ring++) {
                var v = (float)ring / rings;
                var phi = v * math.PI;

                var y = math.cos(phi);
                var r = math.sin(phi);

                for (var segment = 0; segment < segments; segment++) {
                    var u0 = (float)segment / segments;
                    var u1 = (float)(segment + 1) / segments;

                    var t0 = u0 * math.PI * 2f;
                    var t1 = u1 * math.PI * 2f;

                    var p0 = new float3(math.cos(t0) * r, y, math.sin(t0) * r);
                    var p1 = new float3(math.cos(t1) * r, y, math.sin(t1) * r);

                    WriteLine(
                        ref i,
                        Transform(cmd.matrix, p0 * radius),
                        Transform(cmd.matrix, p1 * radius),
                        cmd.color,
                        TransformNormal(cmd.matrix, math.normalizesafe(p0, new float3(0f, 1f, 0f)))
                    );
                }
            }

            for (var segment = 0; segment < segments; segment++) {
                var u = (float)segment / segments;
                var theta = u * math.PI * 2f;

                for (var ring = 0; ring < rings; ring++) {
                    var v0 = (float)ring / rings;
                    var v1 = (float)(ring + 1) / rings;

                    var p0 = SpherePoint(theta, v0);
                    var p1 = SpherePoint(theta, v1);

                    WriteLine(
                        ref i,
                        Transform(cmd.matrix, p0 * radius),
                        Transform(cmd.matrix, p1 * radius),
                        cmd.color,
                        TransformNormal(cmd.matrix, math.normalizesafe(p0, new float3(0f, 1f, 0f)))
                    );
                }
            }
        }

        private static float3 SpherePoint(float theta, float v) {
            var phi = v * math.PI;

            var y = math.cos(phi);
            var r = math.sin(phi);

            return new float3(
                math.cos(theta) * r,
                y,
                math.sin(theta) * r
            );
        }

        private void WriteLine(ref int vertexIndex, float3 a, float3 b, float4 color, float3 normal) {
            vertices[vertexIndex++] = new PolyDrawVertex(a, color, normal, PolyDrawShaderFlags.DoubleSided);
            vertices[vertexIndex++] = new PolyDrawVertex(b, color, normal, PolyDrawShaderFlags.DoubleSided);
        }

        private static float3 Transform(float4x4 matrix, float3 point) {
            return math.transform(matrix, point);
        }

        private static float3 TransformNormal(float4x4 matrix, float3 normal) {
            var m = new float3x3(matrix.c0.xyz, matrix.c1.xyz, matrix.c2.xyz);
            return math.normalizesafe(math.mul(math.transpose(math.inverse(m)), normal), new float3(0f, 1f, 0f));
        }

        private static int ResolveSegments(float value, int fallback, int min) {
            var segments = (int)math.round(value);
            return math.max(min, segments > 0 ? segments : fallback);
        }

    }
}