using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Spookline.SPC.Cleaver {

    [BurstCompile(FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct CleaverSectionPointContainmentJob : IJobParallelFor {

        [ReadOnly]
        public NativeArray<CleaverSectionData> sections;
        [ReadOnly]
        public NativeArray<CleaverVolumeData> volumes;

        public float3 point;
        public byte queryMask;

        public NativeHashSet<ulong> result;

        public void Execute(int index) {
            var section = sections[index];

            if ((section.mask & queryMask) == 0) return;
            if (!section.bounds.Contains(point)) return;
            var contained = section.volumeCount == 0;

            for (var i = 0; i < section.volumeCount; i++) {
                var volume = volumes[section.volumeIndex + i];

                if (!volume.query.ContainsPoint(point)) continue;
                contained = true;
                break;
            }

            if (contained) result.Add(section.id);
        }

    }
}