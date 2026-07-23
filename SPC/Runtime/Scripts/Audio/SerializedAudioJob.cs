using System;
using System.Threading;

namespace Spookline.SPC.Audio {
    [Serializable]
    public class SerializedAudioJob {

        public string guid;
        public int data;
        public AudioOptions options;


        public AudioJob ToAudioJob(CancellationToken cancellationToken = default) {
            var def = SpookAudioRegistry.Instance.GuidLookup[guid];
            return new AudioJob(def, data, options, cancellationToken);
        }

    }
}