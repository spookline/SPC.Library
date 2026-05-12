using System;
using System.Buffers;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Draw {
    public static class CommonShapes {

        public static void Cone<T>(
            this T api,
            Vector3 center,
            Vector3 direction,
            float radius,
            float height,
            int segments = 8
        )
            where T : IDrawingAPI {
            segments = math.max(3, segments);
            radius = math.max(0f, radius);
            height = math.max(0f, height);

            if (height <= 0f) { return; }

            var normal = direction.sqrMagnitude > 1e-10f
                ? direction.normalized
                : Vector3.forward;

            var apex = center + normal * height;

            if (radius <= 1e-6f) {
                api.Line(center, apex);
                return;
            }

            var basisA = DrawingApiDefaults<T>.GetFallbackAxis(normal);
            var basisB = Vector3.Cross(normal, basisA).normalized;

            if (segments > 128) {
                var array = ArrayPool<Vector3>.Shared.Rent(segments);
                try { Render(array.AsSpan(0, segments)); } finally { ArrayPool<Vector3>.Shared.Return(array); }
            } else {
                Span<Vector3> span = stackalloc Vector3[segments];
                Render(span);
            }

            return;

            void Render(Span<Vector3> rim) {
                var step = math.TAU / segments;

                for (var i = 0; i < segments; i++) {
                    var theta = step * i;

                    rim[i] = center + (
                        basisA * math.cos(theta) +
                        basisB * math.sin(theta)
                    ) * radius;
                }

                for (var i = 0; i < segments; i++) {
                    var next = (i + 1) % segments;
                    api.Triangle(apex, rim[i], rim[next]);
                }

                api.Disc(center, normal, radius, segments);
            }
        }

        public static void WireCone<T>(
            this T api,
            Vector3 center,
            Vector3 direction,
            float radius,
            float height,
            int segments = 8
        )
            where T : IDrawingAPI {
            segments = math.max(3, segments);
            radius = math.max(0f, radius);
            height = math.max(0f, height);

            if (height <= 0f) { return; }

            var axis = direction.sqrMagnitude > 1e-10f
                ? direction.normalized
                : Vector3.forward;

            var apex = center + axis * height;

            // Degenerate case: just axis
            if (radius <= 1e-6f) {
                api.Line(center, apex);
                return;
            }

            var basisA = DrawingApiDefaults<T>.GetFallbackAxis(axis);
            var basisB = Vector3.Cross(axis, basisA).normalized;

            if (segments > 128) {
                var array = ArrayPool<Vector3>.Shared.Rent(segments);
                try { Render(array.AsSpan(0, segments)); } finally { ArrayPool<Vector3>.Shared.Return(array); }
            } else {
                Span<Vector3> span = stackalloc Vector3[segments];
                Render(span);
            }

            return;

            void Render(Span<Vector3> rim) {
                var step = math.TAU / segments;

                for (var i = 0; i < segments; i++) {
                    var theta = step * i;

                    rim[i] = center + (
                        basisA * math.cos(theta) +
                        basisB * math.sin(theta)
                    ) * radius;
                }

                // Base rim
                api.Strip(rim, true);

                // Representative side edges (match spherical cone style)
                var a = center + basisA * radius;
                var b = center + basisB * radius;
                var c = center - basisA * radius;
                var d = center - basisB * radius;

                api.Line(apex, a);
                api.Line(apex, b);
                api.Line(apex, c);
                api.Line(apex, d);
            }
        }

        public static void SphereSection<T>(
            this T api,
            Vector3 center,
            Vector3 direction,
            float length,
            float angle,
            int segments = 8
        ) where T : IDrawingAPI {
            segments = math.max(3, segments);
            length = math.max(0f, length);
            angle /= 2;

            if (length <= 0f) { return; }

            var axis = direction.sqrMagnitude > 1e-10f
                ? direction.normalized
                : Vector3.forward;

            // Treat angle as a half-angle in degrees, consistent with WireArc.
            angle = math.clamp(math.abs(angle), 0f, 180f);
            var angleRad = angle * math.TORADIANS;

            // Degenerate cone: just draw the center ray.
            if (angle <= 1e-4f) {
                api.Line(center, center + axis * length);
                return;
            }

            var basisA = DrawingApiDefaults<T>.GetFallbackAxis(axis);
            var basisB = Vector3.Cross(axis, basisA).normalized;

            // Spherical-section rim: all rim points are exactly `length` from origin.
            var rimCenter = center + axis * (math.cos(angleRad) * length);
            var rimRadius = math.sin(angleRad) * length;

            var size = segments;

            if (segments > 128) {
                var array = ArrayPool<Vector3>.Shared.Rent(size);
                try { Render(array.AsSpan(0, size)); } finally { ArrayPool<Vector3>.Shared.Return(array); }
            } else {
                Span<Vector3> span = stackalloc Vector3[size];
                Render(span);
            }

            return;

            void Render(Span<Vector3> rim) {
                var step = math.TAU / segments;

                for (var i = 0; i < segments; i++) {
                    var theta = step * i;

                    rim[i] = rimCenter + (
                        basisA * math.cos(theta) +
                        basisB * math.sin(theta)
                    ) * rimRadius;
                }

                var a = rimCenter + (basisA * math.cos(0) + basisB * math.sin(0)) * rimRadius;
                var b = rimCenter + (basisA * math.cos(math.TAU * 0.25f) + basisB * math.sin(math.TAU * 0.25f)) *
                    rimRadius;
                var c = rimCenter + (basisA * math.cos(math.TAU * 0.50f) + basisB * math.sin(math.TAU * 0.50f)) *
                    rimRadius;
                var d = rimCenter + (basisA * math.cos(math.TAU * 0.75f) + basisB * math.sin(math.TAU * 0.75f)) *
                    rimRadius;

                // Rim of the spherical section.
                api.Strip(rim, true);

                // Four representative cone boundary rays.
                api.Line(center, a);
                api.Line(center, b);
                api.Line(center, c);
                api.Line(center, d);

                // Four spherical meridian arcs from the axis direction to the rim.
                api.WireArc(center, Vector3.Cross(axis, basisA).normalized, axis, length, angle, segments);
                api.WireArc(center, Vector3.Cross(axis, -basisA).normalized, axis, length, angle, segments);
                api.WireArc(center, Vector3.Cross(axis, basisB).normalized, axis, length, angle, segments);
                api.WireArc(center, Vector3.Cross(axis, -basisB).normalized, axis, length, angle, segments);
            }
        }

        public static void Arrow<T>(
            this T api,
            Vector3 origin,
            Vector3 direction,
            float length = 1f,
            float headLength = 0.25f,
            float headRadius = 0.1f,
            int segments = 8
        )
            where T : IDrawingAPI {
            length = math.max(0f, length);
            headLength = math.clamp(headLength, 0f, length);
            headRadius = math.max(0f, headRadius);
            segments = math.max(3, segments);

            if (length <= 0f) { return; }

            var axis = direction.sqrMagnitude > 1e-10f
                ? direction.normalized
                : Vector3.forward;

            headLength = math.min(headLength, length);

            var shaftLength = length - headLength;

            var shaftEnd = origin + axis * shaftLength;
            var tip = origin + axis * length;

            if (shaftLength > 1e-6f) { api.Line(origin, shaftEnd); }

            if (headLength > 1e-6f && headRadius > 1e-6f) {
                api.Cone(
                    shaftEnd,
                    axis,
                    headRadius,
                    headLength,
                    segments
                );
            } else { api.Line(shaftEnd, tip); }
        }

    }
}