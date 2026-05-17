using System.Collections.Generic;
using Spookline.SPC.Events;

namespace Spookline.SPC.Debugging {
    public struct DebugFlagsChangedEvt : Evt<DebugFlagsChangedEvt> {

        public HashSet<string> flags;
        public bool debugging;

    }
}