using System;
using UnityEngine;

namespace Spookline.SPC.Draw {
    public readonly struct DrawingScopeHandle<T> : IDisposable where T : IDrawingAPI {

        private readonly Color _color;
        private readonly Matrix4x4 _matrix;
        private readonly T _api;

        public DrawingScopeHandle(Color color, Matrix4x4 matrix, T api) {
            _color = color;
            _matrix = matrix;
            _api = api;
        }

        public void Dispose() {
            _api.Color = _color;
            _api.Matrix = _matrix;
        }

    }

    public static class DrawingScope {

        public static DrawingScopeHandle<T> Scope<T>(this T api, Color color, Matrix4x4 matrix) where T : IDrawingAPI {
            var originalColor = api.Color;
            var originalMatrix = api.Matrix;
            api.Color = color;
            api.Matrix = matrix;
            return new DrawingScopeHandle<T>(originalColor, originalMatrix, api);
        }

        public static DrawingScopeHandle<T> Scope<T>(this T api, Color color) where T : IDrawingAPI {
            var originalColor = api.Color;
            api.Color = color;
            return new DrawingScopeHandle<T>(originalColor, api.Matrix, api);
        }

        public static DrawingScopeHandle<T> Scope<T>(this T api, Matrix4x4 matrix) where T : IDrawingAPI {
            var originalMatrix = api.Matrix;
            api.Matrix = matrix;
            return new DrawingScopeHandle<T>(api.Color, originalMatrix, api);
        }

        public static DrawingScopeHandle<T> ScopeTransformation<T>(this T api, Matrix4x4 matrix) where T : IDrawingAPI {
            var originalMatrix = api.Matrix;
            return originalMatrix.isIdentity ? api.Scope(matrix) : api.Scope(originalMatrix * matrix);
        }
    }
}