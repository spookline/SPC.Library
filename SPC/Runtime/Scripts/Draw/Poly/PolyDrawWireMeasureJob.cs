using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Spookline.SPC.Draw {
    [BurstCompile]
    public struct PolyDrawWireMeasureJob : IJobParallelFor {

        [ReadOnly]
        public NativeArray<PolyDrawCommand> commands;

        [WriteOnly]
        public NativeArray<int> vertexCounts;

        public void Execute(int index) {
            var cmd = commands[index];

            if (!PolyDrawCommandBitmask.HasFlag(cmd.flags, PolyDrawCommandFlags.Wire)) {
                vertexCounts[index] = 0;
                return;
            }

            var lineCount = 0;
            switch (cmd.type) {
                case PolyDrawCommandType.Triangle:
                    lineCount = 3;
                    break;

                case PolyDrawCommandType.Quad:
                    lineCount = 4;
                    break;

                case PolyDrawCommandType.Cube:
                    lineCount = 12;
                    break;

                case PolyDrawCommandType.Disc: {
                    var segments = ResolveSegments(cmd.args.y, 32, 3);
                    lineCount = segments;
                    break;
                }

                case PolyDrawCommandType.Arc: {
                    var segments = ResolveSegments(cmd.args.y, 32, 1);
                    lineCount = segments;
                    break;
                }

                case PolyDrawCommandType.Sphere: {
                    var segments = ResolveSegments(cmd.args.y, 16, 3);
                    var rings = math.max(2, segments / 2);

                    lineCount = (rings + 1) * segments + segments * rings;
                    break;
                }
            }

            vertexCounts[index] = lineCount * 2;
        }

        private static int ResolveSegments(float value, int fallback, int min) {
            var segments = (int)math.round(value);
            return math.max(min, segments > 0 ? segments : fallback);
        }

    }
}