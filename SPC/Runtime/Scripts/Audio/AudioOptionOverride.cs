using System;
using Sirenix.OdinInspector;

namespace Spookline.SPC.Audio {
    [Serializable]
    [InlineProperty]
    public class AudioOptionOverride {

        [ToggleGroup("hasOverride", "Audio Overrides")]
        public bool hasOverride;
        [ToggleGroup("hasOverride")]
        [InlineProperty]
        [HideLabel]
        public AudioOptions options;

    }
}