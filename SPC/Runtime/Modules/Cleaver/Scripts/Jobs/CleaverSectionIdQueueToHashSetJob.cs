using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Spookline.SPC.Cleaver {
    [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
    public struct CleaverSectionIdQueueToHashSetJob : IJob {

        public NativeQueue<ulong> containedSectionIds;
        public NativeHashSet<ulong> result;

        public void Execute() {
            while (containedSectionIds.TryDequeue(out var id)) result.Add(id);
        }

    }
}