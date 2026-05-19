using Spookline.SPC.Debugging;
using Spookline.SPC.Draw;

namespace Spookline.SPC.Cleaver.Points {
    public abstract class CleaverPoint {

        public abstract void DrawEditor(IDrawingAPI api);
        public abstract void Gizmos(ref GizmoEvt evt);

    }


}