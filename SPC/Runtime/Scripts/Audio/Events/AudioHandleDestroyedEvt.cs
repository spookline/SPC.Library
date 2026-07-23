using Spookline.SPC.Events;
using UnityEngine;

namespace Spookline.SPC.Audio.Events {
    public sealed class AudioHandleDestroyedEvt : Evt<AudioHandleDestroyedEvt> {
        public GameObject SourceObject { get; set; }
        public AudioHandle Handle { get; set; }
    }
}
