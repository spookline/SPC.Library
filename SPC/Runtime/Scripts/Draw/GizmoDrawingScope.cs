using System;
using UnityEngine;

namespace Spookline.SPC.Draw {
    public readonly struct GizmoDrawingScope : IDisposable {

        private readonly Color _color;
        private readonly Matrix4x4 _matrix;

        public GizmoDrawingScope(Color color, Matrix4x4 matrix) {
            _color = Gizmos.color;
            _matrix = Gizmos.matrix;
            Gizmos.color = color;
            Gizmos.matrix = matrix;
        }


        public void Dispose() {
            Gizmos.color = _color;
            Gizmos.matrix = _matrix;
        }

    }
}