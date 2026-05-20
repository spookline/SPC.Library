using System;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Draw {
    public class PolyDrawingApi : IDrawingAPI, IDisposable {

        public void Line(Vector3 a, Vector3 b) {
            if (isIdentity) {
                PolyDrawRenderer.Instance.AddLine(a, b, PolyDrawCommandFactory.Color(Color), Duration);
            } else {
                PolyDrawRenderer.Instance.AddLine(
                    Matrix.MultiplyPoint(a),
                    Matrix.MultiplyPoint(b),
                    PolyDrawCommandFactory.Color(Color),
                    Duration
                );
            }
        }

        public void Lines(ReadOnlySpan<Vector3> points) {
            PolyDrawRenderer.Instance.AddLines(
                points,
                PolyDrawCommandFactory.Color(Color),
                Duration,
                _matrix,
                !isIdentity
            );
        }

        public void Strip(ReadOnlySpan<Vector3> points, bool closed = false) {
            PolyDrawRenderer.Instance.AddStrip(
                points,
                PolyDrawCommandFactory.Color(Color),
                closed,
                Duration,
                _matrix,
                !isIdentity
            );
        }

        public void WireCube(Vector3 center, Vector3 size) {
            if (isIdentity) {
                PolyDrawRenderer.Instance.AddCommand(
                    PolyDrawCommandFactory.Cube(
                        center,
                        size,
                        PolyDrawCommandFactory.Color(Color),
                        PolyDrawCommandFlags.Wire
                    ),
                    Duration
                );
            } else {
                PolyDrawRenderer.Instance.AddCommand(
                    PolyDrawCommandFactory.Cube(
                        Matrix,
                        center,
                        size,
                        PolyDrawCommandFactory.Color(Color),
                        PolyDrawCommandFlags.Wire
                    ),
                    Duration
                );
            }
        }

        public void Cube(Vector3 center, Vector3 size) {
            PolyDrawRenderer.Instance.AddCommand(
                isIdentity
                    ? PolyDrawCommandFactory.Cube(center, size, PolyDrawCommandFactory.Color(Color))
                    : PolyDrawCommandFactory.Cube(Matrix, center, size, PolyDrawCommandFactory.Color(Color)),
                Duration
            );
        }

        public void WireSphere(Vector3 center, float radius, int segments = 32) {
            if (isIdentity) {
                PolyDrawRenderer.Instance.AddCommand(
                    PolyDrawCommandFactory.Sphere(
                        center,
                        radius,
                        segments,
                        PolyDrawCommandFactory.Color(Color),
                        PolyDrawCommandFlags.Wire
                    ),
                    Duration
                );
            } else {
                PolyDrawRenderer.Instance.AddCommand(
                    PolyDrawCommandFactory.Sphere(
                        Matrix,
                        center,
                        radius,
                        segments,
                        PolyDrawCommandFactory.Color(Color),
                        PolyDrawCommandFlags.Wire
                    ),
                    Duration
                );
            }
        }

        public void Sphere(Vector3 center, float radius, int segments = 32) {
            if (isIdentity) {
                PolyDrawRenderer.Instance.AddCommand(
                    PolyDrawCommandFactory.Sphere(center, radius, segments, PolyDrawCommandFactory.Color(Color)),
                    Duration
                );
            } else {
                PolyDrawRenderer.Instance.AddCommand(
                    PolyDrawCommandFactory.Sphere(
                        Matrix,
                        center,
                        radius,
                        segments,
                        PolyDrawCommandFactory.Color(Color)
                    ),
                    Duration
                );
            }
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c) {
            PolyDrawRenderer.Instance.AddCommand(
                isIdentity
                    ? PolyDrawCommandFactory.Triangle(
                        a,
                        b,
                        c,
                        PolyDrawCommandFactory.Color(Color),
                        PolyDrawCommandFlags.Wire
                    )
                    : PolyDrawCommandFactory.Triangle(
                        Matrix,
                        a,
                        b,
                        c,
                        PolyDrawCommandFactory.Color(Color)
                    ),
                Duration
            );
        }

        public void Triangles(ReadOnlySpan<Vector3> points) {
            for (var i = 0; i < points.Length; i += 3) {
                if (i + 2 >= points.Length) break;
                Triangle(points[i], points[i + 1], points[i + 2]);
            }
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
            PolyDrawRenderer.Instance.AddCommand(
                isIdentity
                    ? PolyDrawCommandFactory.Quad(
                        a,
                        b,
                        c,
                        d,
                        PolyDrawCommandFactory.Color(Color),
                        PolyDrawCommandFlags.Wire
                    )
                    : PolyDrawCommandFactory.Quad(
                        Matrix,
                        a,
                        b,
                        c,
                        d,
                        PolyDrawCommandFactory.Color(Color)
                    ),
                Duration
            );
        }

        public void Disc(Vector3 center, Vector3 normal, float radius, int segments = 32) {
            if (isIdentity) {
                PolyDrawRenderer.Instance.AddCommand(
                    PolyDrawCommandFactory.Disc(
                        center,
                        normal,
                        radius,
                        segments,
                        PolyDrawCommandFactory.Color(Color)
                    ),
                    Duration
                );
            } else {
                PolyDrawRenderer.Instance.AddCommand(
                    PolyDrawCommandFactory.Disc(
                        Matrix,
                        center,
                        normal,
                        radius,
                        segments,
                        PolyDrawCommandFactory.Color(Color)
                    ),
                    Duration
                );
            }
        }

        public void WireDisc(Vector3 center, Vector3 normal, float radius, int segments = 32) {
            if (isIdentity) {
                PolyDrawRenderer.Instance.AddCommand(
                    PolyDrawCommandFactory.Disc(
                        center,
                        normal,
                        radius,
                        segments,
                        PolyDrawCommandFactory.Color(Color),
                        PolyDrawCommandFlags.Wire
                    ),
                    Duration
                );
            } else {
                PolyDrawRenderer.Instance.AddCommand(
                    PolyDrawCommandFactory.Disc(
                        Matrix,
                        center,
                        normal,
                        radius,
                        segments,
                        PolyDrawCommandFactory.Color(Color),
                        PolyDrawCommandFlags.Wire
                    ),
                    Duration
                );
            }
        }

        public void Arc(Vector3 center, Vector3 normal, Vector3 from, float radius, float angle, int segments = 32) {
            if (isIdentity) {
                PolyDrawRenderer.Instance.AddCommand(
                    PolyDrawCommandFactory.Arc(
                        center,
                        normal,
                        from,
                        radius,
                        segments,
                        angle,
                        PolyDrawCommandFactory.Color(Color)
                    ),
                    Duration
                );
            } else {
                PolyDrawRenderer.Instance.AddCommand(
                    PolyDrawCommandFactory.Arc(
                        Matrix,
                        center,
                        normal,
                        from,
                        radius,
                        segments,
                        angle,
                        PolyDrawCommandFactory.Color(Color)
                    ),
                    Duration
                );
            }
        }

        public void WireArc(
            Vector3 center,
            Vector3 normal,
            Vector3 from,
            float radius,
            float angle,
            int segments = 32
        ) {
            if (isIdentity) {
                PolyDrawRenderer.Instance.AddCommand(
                    PolyDrawCommandFactory.Arc(
                        center,
                        normal,
                        from,
                        radius,
                        segments,
                        angle,
                        PolyDrawCommandFactory.Color(Color),
                        PolyDrawCommandFlags.Wire
                    ),
                    Duration
                );
            } else {
                PolyDrawRenderer.Instance.AddCommand(
                    PolyDrawCommandFactory.Arc(
                        Matrix,
                        center,
                        normal,
                        from,
                        radius,
                        segments,
                        angle,
                        PolyDrawCommandFactory.Color(Color),
                        PolyDrawCommandFlags.Wire
                    ),
                    Duration
                );
            }
        }

        public void Mesh(Mesh mesh) {
            PolyDrawRenderer.Instance.AddMesh(
                mesh,
                Color,
                Duration,
                _matrix,
                !isIdentity
            );
        }

        public void WireMesh(Mesh mesh) {
            PolyDrawRenderer.Instance.AddWireMesh(
                mesh,
                Color,
                Duration,
                _matrix,
                !isIdentity
            );
        }

        public void LineBuffer(PolyDrawBuffer buffer) {
            var copied = buffer;
            copied.transform = isIdentity ? AffineTransform.identity : new AffineTransform(Matrix);
            if (!Mathf.Approximately(Color.a, -1f)) copied.color = PolyDrawCommandFactory.Color(Color);
            PolyDrawRenderer.Instance.AddLineBuffer(copied, Duration);
        }

        public void MeshBuffer(PolyDrawBuffer buffer) {
            var copied = buffer;
            copied.transform = isIdentity ? AffineTransform.identity : new AffineTransform(Matrix);
            if (!Mathf.Approximately(Color.a, -1f))  copied.color = PolyDrawCommandFactory.Color(Color);
            PolyDrawRenderer.Instance.AddMeshBuffer(copied, Duration);
        }

        private Matrix4x4 _matrix;
        public bool isIdentity;


        public Matrix4x4 Matrix {
            get => _matrix;
            set {
                _matrix = value;
                isIdentity = _matrix.isIdentity;
            }
        }

        public Color Color { get; set; }
        public float Duration { get; set; }

        public void Dispose() { }

    }
}