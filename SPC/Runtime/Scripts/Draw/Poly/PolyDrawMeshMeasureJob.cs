using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Spookline.SPC.Draw {
    [BurstCompile]
    public struct PolyDrawMeshMeasureJob : IJobParallelFor {

        [ReadOnly]
        public NativeArray<PolyDrawCommand> commands;

        // x = vertex count, y = index count
        [WriteOnly]
        public NativeArray<int2> sizes;

        public void Execute(int index) {
            var cmd = commands[index];

            if (PolyDrawCommandBitmask.HasFlag(cmd.flags, PolyDrawCommandFlags.Wire)) {
                sizes[index] = int2.zero;
                return;
            }

            var vertexCount = 0;
            var indexCount = 0;

            switch (cmd.type) {
                case PolyDrawCommandType.Triangle:
                    vertexCount = 3;
                    indexCount = 3;
                    break;

                case PolyDrawCommandType.Quad:
                    vertexCount = 4;
                    indexCount = 6;
                    break;

                case PolyDrawCommandType.Cube:
                    // 4 vertices per face, so each face has a crisp flat normal.
                    vertexCount = 24;
                    indexCount = 36;
                    break;

                case PolyDrawCommandType.Sphere: {
                    var segments = ResolveSegments(cmd.args.y, 16, 3);
                    var rings = math.max(2, segments / 2);

                    vertexCount = (rings + 1) * (segments + 1);
                    indexCount = rings * segments * 6;
                    break;
                }

                case PolyDrawCommandType.Disc: {
                    var segments = ResolveSegments(cmd.args.y, 32, 3);

                    vertexCount = 1 + segments;
                    indexCount = segments * 3;
                    break;
                }

                case PolyDrawCommandType.Arc: {
                    var segments = ResolveSegments(cmd.args.y, 32, 1);

                    vertexCount = 1 + segments + 1;
                    indexCount = segments * 3;
                    break;
                }
            }

            sizes[index] = new int2(vertexCount, indexCount);
        }

        private static int ResolveSegments(float value, int fallback, int min) {
            var segments = (int)math.round(value);
            return math.max(min, segments > 0 ? segments : fallback);
        }

    }
}