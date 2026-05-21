using Unity.Collections;

namespace Spookline.SPC.Draw.Poly {
    /// <summary>
    /// Simple writer for adding primitive draw commands to a collection.
    /// Use PrimitiveCommandFactory to create commands with builder patterns.
    /// </summary>
    public struct PolyDrawCommandWriter {

        private NativeList<PolyDrawCommand> _commands;
        public int Count => _commands.Length;

        public PolyDrawCommandWriter(NativeList<PolyDrawCommand> commands) {
            _commands = commands;
        }

        public void Clear() => _commands.Clear();

        public NativeArray<PolyDrawCommand> AsArray() => _commands.AsArray();

        public void Add(PolyDrawCommand command) => _commands.Add(command);

    }
}