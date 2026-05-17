using Spookline.SPC.Draw;
using Spookline.SPC.Events;
using Spookline.SPC.Geometry;

namespace Spookline.SPC.Debugging {

    public struct GizmoEvt : Evt<GizmoEvt>, IDebugGuards, IDebugCullable {

        public IDrawingAPI drawer;
        public IScreenOverlayAPI screenOverlay;
        public IWorldOverlayAPI worldOverlay;
        public GizmoType type;


        public Frustum6 Frustum { get; set; }
        public bool HasFrustum { get; set; }

        public bool ScreenOverlayPass(out ScreenOverlayInitiator screen) {
            screen = new ScreenOverlayInitiator(screenOverlay);
            return screenOverlay != null;
        }

        public bool WorldOverlayPass(out WorldOverlayInitiator world) {
            world = new WorldOverlayInitiator(worldOverlay);
            return worldOverlay != null;
        }

        public bool DrawingPass(out IDrawingAPI draw) {
            if (drawer == null) {
                draw = null;
                return false;
            }

            draw = drawer;
            return true;
        }

        public bool DrawingPass(GizmoType expected, out IDrawingAPI draw) {
            if (drawer == null || type != expected) {
                draw = null;
                return false;
            }

            draw = drawer;
            return true;
        }

        public static GizmoEvt EditorGizmos =>
            new() {
                type = GizmoType.Gizmos,
                drawer = Drawing.Gizmos,
                HasFrustum = false,
            };

        public static GizmoEvt EditorGizmosSelected =>
            new() {
                type = GizmoType.GizmosSelected,
                drawer = Drawing.Gizmos,
                HasFrustum = false,
            };

    }

    public enum GizmoType {
        Runtime,
        Gizmos,
        GizmosSelected,
    }
}