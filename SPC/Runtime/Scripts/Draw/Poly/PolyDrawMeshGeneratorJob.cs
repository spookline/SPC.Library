using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Spookline.SPC.Draw.Poly {
    [BurstCompile]
    public struct PolyDrawMeshGeneratorJob : IJobParallelFor {

        [ReadOnly]
        public NativeArray<PolyDrawCommand> commands;

        [ReadOnly]
        public NativeArray<int> vertexOffsets;

        [ReadOnly]
        public NativeArray<int> indexOffsets;

        [NativeDisableParallelForRestriction, WriteOnly]
        public NativeArray<PolyDrawVertex> vertices;

        [NativeDisableParallelForRestriction, WriteOnly]
        public NativeArray<int> indices;

        public void Execute(int commandIndex) {
            var cmd = commands[commandIndex];

            if (PolyDrawCommandBitmask.HasFlag(cmd.flags, PolyDrawCommandFlags.Wire))
                return;

            var vertexBase = vertexOffsets[commandIndex];
            var indexBase = indexOffsets[commandIndex];

            switch (cmd.type) {
                case PolyDrawCommandType.Triangle:
                    WriteTriangle(cmd, vertexBase, indexBase);
                    break;

                case PolyDrawCommandType.Quad:
                    WriteQuad(cmd, vertexBase, indexBase);
                    break;

                case PolyDrawCommandType.Cube:
                    WriteCube(cmd, vertexBase, indexBase);
                    break;

                case PolyDrawCommandType.Sphere:
                    WriteSphere(cmd, vertexBase, indexBase);
                    break;

                case PolyDrawCommandType.Disc:
                    WriteDisc(cmd, vertexBase, indexBase);
                    break;

                case PolyDrawCommandType.Arc:
                    WriteArc(cmd, vertexBase, indexBase);
                    break;
            }
        }

        private void WriteTriangle(PolyDrawCommand cmd, int vertexBase, int indexBase) {
            var a = cmd.matrix.c0.xyz;
            var b = cmd.matrix.c1.xyz;
            var c = cmd.matrix.c2.xyz;

            // If normals are ~0, calculate a flat normal for the triangle.
            var normal = cmd.args;
            if (math.all(math.abs(normal) < 0.0001f)) {
                math.normalizesafe(math.cross(b - a, c - a), new float3(0f, 1f, 0f));
            }

            vertices[vertexBase + 0] = new PolyDrawVertex(a, cmd.color, normal, PolyDrawShaderFlags.DoubleSided);
            vertices[vertexBase + 1] = new PolyDrawVertex(b, cmd.color, normal, PolyDrawShaderFlags.DoubleSided);
            vertices[vertexBase + 2] = new PolyDrawVertex(c, cmd.color, normal, PolyDrawShaderFlags.DoubleSided);

            WriteTri(indexBase, vertexBase + 0, vertexBase + 1, vertexBase + 2);
        }

        private void WriteQuad(PolyDrawCommand cmd, int vertexBase, int indexBase) {
            var a = cmd.matrix.c0.xyz;
            var b = cmd.matrix.c1.xyz;
            var c = cmd.matrix.c2.xyz;
            var d = cmd.matrix.c3.xyz;

            var normal = cmd.args;
            if (math.all(math.abs(normal) < 0.0001f)) {
                math.normalizesafe(math.cross(b - a, c - a), new float3(0f, 1f, 0f));
            }

            vertices[vertexBase + 0] = new PolyDrawVertex(a, cmd.color, normal, PolyDrawShaderFlags.DoubleSided);
            vertices[vertexBase + 1] = new PolyDrawVertex(b, cmd.color, normal, PolyDrawShaderFlags.DoubleSided);
            vertices[vertexBase + 2] = new PolyDrawVertex(c, cmd.color, normal, PolyDrawShaderFlags.DoubleSided);
            vertices[vertexBase + 3] = new PolyDrawVertex(d, cmd.color, normal, PolyDrawShaderFlags.DoubleSided);

            WriteQuadIndices(indexBase, vertexBase + 0, vertexBase + 1, vertexBase + 2, vertexBase + 3);
        }

        private void WriteCube(PolyDrawCommand cmd, int vertexBase, int indexBase) {
            var i = vertexBase;
            var ti = indexBase;

            // -Z
            WriteCubeFace(
                cmd,
                ref i,
                ref ti,
                new float3(+0.5f, -0.5f, -0.5f),
                new float3(-0.5f, -0.5f, -0.5f),
                new float3(-0.5f, +0.5f, -0.5f),
                new float3(+0.5f, +0.5f, -0.5f),
                new float3(0f, 0f, -1f)
            );

            // +Z
            WriteCubeFace(
                cmd,
                ref i,
                ref ti,
                new float3(-0.5f, -0.5f, +0.5f),
                new float3(+0.5f, -0.5f, +0.5f),
                new float3(+0.5f, +0.5f, +0.5f),
                new float3(-0.5f, +0.5f, +0.5f),
                new float3(0f, 0f, +1f)
            );

            // -X
            WriteCubeFace(
                cmd,
                ref i,
                ref ti,
                new float3(-0.5f, -0.5f, -0.5f),
                new float3(-0.5f, -0.5f, +0.5f),
                new float3(-0.5f, +0.5f, +0.5f),
                new float3(-0.5f, +0.5f, -0.5f),
                new float3(-1f, 0f, 0f)
            );

            // +X
            WriteCubeFace(
                cmd,
                ref i,
                ref ti,
                new float3(+0.5f, -0.5f, +0.5f),
                new float3(+0.5f, -0.5f, -0.5f),
                new float3(+0.5f, +0.5f, -0.5f),
                new float3(+0.5f, +0.5f, +0.5f),
                new float3(+1f, 0f, 0f)
            );

            // -Y
            WriteCubeFace(
                cmd,
                ref i,
                ref ti,
                new float3(-0.5f, -0.5f, -0.5f),
                new float3(+0.5f, -0.5f, -0.5f),
                new float3(+0.5f, -0.5f, +0.5f),
                new float3(-0.5f, -0.5f, +0.5f),
                new float3(0f, -1f, 0f)
            );

            // +Y
            WriteCubeFace(
                cmd,
                ref i,
                ref ti,
                new float3(-0.5f, +0.5f, +0.5f),
                new float3(+0.5f, +0.5f, +0.5f),
                new float3(+0.5f, +0.5f, -0.5f),
                new float3(-0.5f, +0.5f, -0.5f),
                new float3(0f, +1f, 0f)
            );
        }

        private void WriteCubeFace(
            PolyDrawCommand cmd,
            ref int vertexIndex,
            ref int indexIndex,
            float3 a,
            float3 b,
            float3 c,
            float3 d,
            float3 localNormal
        ) {
            var baseIndex = vertexIndex;
            var normal = TransformNormal(cmd.matrix, localNormal);

            vertices[vertexIndex++] = new PolyDrawVertex(Transform(cmd.matrix, a), cmd.color, normal);
            vertices[vertexIndex++] = new PolyDrawVertex(Transform(cmd.matrix, b), cmd.color, normal);
            vertices[vertexIndex++] = new PolyDrawVertex(Transform(cmd.matrix, c), cmd.color, normal);
            vertices[vertexIndex++] = new PolyDrawVertex(Transform(cmd.matrix, d), cmd.color, normal);

            WriteQuadIndices(indexIndex, baseIndex + 0, baseIndex + 1, baseIndex + 2, baseIndex + 3);
            indexIndex += 6;
        }

        private void WriteSphere(PolyDrawCommand cmd, int vertexBase, int indexBase) {
            var radius = math.max(0f, cmd.args.x);
            var segments = ResolveSegments(cmd.args.y, 16, 3);
            var rings = math.max(2, segments / 2);

            for (var ring = 0; ring <= rings; ring++) {
                var v = (float)ring / rings;
                var phi = v * math.PI;

                var y = math.cos(phi);
                var r = math.sin(phi);

                for (var segment = 0; segment <= segments; segment++) {
                    var u = (float)segment / segments;
                    var theta = u * math.PI * 2f;

                    var localNormal = new float3(
                        math.cos(theta) * r,
                        y,
                        math.sin(theta) * r
                    );

                    var localPosition = localNormal * radius;
                    var normal = TransformNormal(cmd.matrix, localNormal);

                    var vertexIndex = vertexBase + ring * (segments + 1) + segment;
                    vertices[vertexIndex] = new PolyDrawVertex(
                        Transform(cmd.matrix, localPosition),
                        cmd.color,
                        normal
                    );
                }
            }

            var i = indexBase;

            for (var ring = 0; ring < rings; ring++) {
                var row0 = vertexBase + ring * (segments + 1);
                var row1 = vertexBase + (ring + 1) * (segments + 1);

                for (var segment = 0; segment < segments; segment++) {
                    var a = row0 + segment;
                    var b = row0 + segment + 1;
                    var c = row1 + segment + 1;
                    var d = row1 + segment;

                    WriteQuadIndices(i, a, b, c, d);
                    i += 6;
                }
            }
        }

        private void WriteDisc(PolyDrawCommand cmd, int vertexBase, int indexBase) {
            var radius = math.max(0f, cmd.args.x);
            var segments = ResolveSegments(cmd.args.y, 32, 3);
            var normal = TransformNormal(cmd.matrix, new float3(0f, 0f, 1f));

            vertices[vertexBase] = new PolyDrawVertex(
                Transform(cmd.matrix, float3.zero),
                cmd.color,
                normal,
                PolyDrawShaderFlags.DoubleSided
            );

            for (var segment = 0; segment < segments; segment++) {
                var t = (float)segment / segments;
                var angle = t * math.PI * 2f;

                var local = new float3(math.cos(angle) * radius, math.sin(angle) * radius, 0f);
                vertices[vertexBase + 1 + segment] = new PolyDrawVertex(
                    Transform(cmd.matrix, local),
                    cmd.color,
                    normal,
                    PolyDrawShaderFlags.DoubleSided
                );
            }

            var i = indexBase;

            for (var segment = 0; segment < segments; segment++) {
                var a = vertexBase;
                var b = vertexBase + 1 + segment;
                var c = vertexBase + 1 + ((segment + 1) % segments);

                WriteTri(i, a, b, c);
                i += 3;
            }
        }

        private void WriteArc(PolyDrawCommand cmd, int vertexBase, int indexBase) {
            var radius = math.max(0f, cmd.args.x);
            var segments = ResolveSegments(cmd.args.y, 32, 1);
            var angleRadians = math.radians(cmd.args.z);
            var normal = TransformNormal(cmd.matrix, new float3(0f, 0f, 1f));

            vertices[vertexBase] = new PolyDrawVertex(
                Transform(cmd.matrix, float3.zero),
                cmd.color,
                normal,
                PolyDrawShaderFlags.DoubleSided
            );

            for (var segment = 0; segment <= segments; segment++) {
                var t = (float)segment / segments;
                var angle = t * angleRadians;

                var local = new float3(math.cos(angle) * radius, math.sin(angle) * radius, 0f);
                vertices[vertexBase + 1 + segment] = new PolyDrawVertex(
                    Transform(cmd.matrix, local),
                    cmd.color,
                    normal,
                    PolyDrawShaderFlags.DoubleSided
                );
            }

            var i = indexBase;

            for (var segment = 0; segment < segments; segment++) {
                var a = vertexBase;
                var b = vertexBase + 1 + segment;
                var c = vertexBase + 1 + segment + 1;

                WriteTri(i, a, b, c);
                i += 3;
            }
        }

        private void WriteQuadIndices(int indexBase, int a, int b, int c, int d) {
            indices[indexBase + 0] = a;
            indices[indexBase + 1] = b;
            indices[indexBase + 2] = c;

            indices[indexBase + 3] = a;
            indices[indexBase + 4] = c;
            indices[indexBase + 5] = d;
        }

        private void WriteTri(int indexBase, int a, int b, int c) {
            indices[indexBase + 0] = a;
            indices[indexBase + 1] = b;
            indices[indexBase + 2] = c;
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