using Spookline.SPC.Events;
using UnityEngine;

namespace Spookline.SPC.Audio.Events {
    public sealed class AudioHandleCreatedEvt : Evt<AudioHandleCreatedEvt> {
        public GameObject SourceObject { get; set; }
        public AudioHandle Handle { get; set; }
    }
}
