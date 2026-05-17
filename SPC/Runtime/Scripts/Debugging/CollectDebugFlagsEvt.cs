using System.Collections.Generic;
using Spookline.SPC.Events;

namespace Spookline.SPC.Debugging {
    public struct CollectDebugFlagsEvt : Evt<CollectDebugFlagsEvt> {

        public HashSet<string> flags;

        public void Add(string flag) => flags.Add(flag);

        public void Add(params string[] multiple) {
            foreach (var flag in multiple) this.flags.Add(flag);
        }

    }
}