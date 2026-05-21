using System.Collections.Generic;

namespace Spookline.SPC.Draw.Poly {
    public struct PolyDrawBufferWriter {

        public List<PolyDrawBuffer> buffers;

        public PolyDrawBufferWriter(List<PolyDrawBuffer> buffers) {
            this.buffers = buffers;
        }
        public int Count => buffers.Count;
        public void Clear() => buffers.Clear();

        public void Add(PolyDrawBuffer buffer) => buffers.Add(buffer);

    }
}