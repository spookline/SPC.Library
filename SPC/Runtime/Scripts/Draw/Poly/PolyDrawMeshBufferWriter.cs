using System.Collections.Generic;

namespace Spookline.SPC.Draw {
    public struct PolyDrawMeshBufferWriter {

        public List<PolyDrawMeshBuffer> buffers;

        public PolyDrawMeshBufferWriter(List<PolyDrawMeshBuffer> buffers) {
            this.buffers = buffers;
        }
        public int Count => buffers.Count;
        public void Clear() => buffers.Clear();

        public void Add(PolyDrawMeshBuffer buffer) => buffers.Add(buffer);

    }
}