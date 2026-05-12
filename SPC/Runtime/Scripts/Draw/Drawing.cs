using UnityEngine;

namespace Spookline.SPC.Draw {
    public static class Drawing {

        public static readonly GizmosDrawingApi Gizmos = new();
        public static readonly HandlesDrawingApi Handles = new();

        private static readonly PolyDrawingApi _poly = new();
        private static readonly ImmediateGlDrawingApi _immediateGl = new();
        private static readonly BufferedGlDrawingApi _glBuffered = new();

        public static PolyDrawingApi Poly(Color? color = null, Matrix4x4? matrix = null, float duration = -1f) {
            _poly.Color = color ?? Color.white;
            _poly.Matrix = matrix ?? Matrix4x4.identity;
            _poly.Duration = duration;
            return _poly;
        }

        public static BufferedGlDrawingApi BufferedGL(Color? color = null, Matrix4x4? matrix = null) {
            _glBuffered.Color = color ?? Color.white;
            _glBuffered.Matrix = matrix ?? Matrix4x4.identity;
            return _glBuffered;
        }

        public static ImmediateGlDrawingApi ImmediateGL(Color? color = null, Matrix4x4? matrix = null) {
            _immediateGl.Color = color ?? Color.white;
            _immediateGl.Matrix = matrix ?? Matrix4x4.identity;
            return _immediateGl;
        }

    }
}