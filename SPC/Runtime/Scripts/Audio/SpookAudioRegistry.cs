using System;
using System.Threading;
using Spookline.SPC.Registry;

namespace Spookline.SPC.Audio {
    public class SpookAudioRegistry : ObjectRegistry<SpookAudioRegistry, AudioDefinition> {

        public override string AddressableLabel => "audio";

        public static AudioDefinition EnumInterop(Enum enumValue) {
            return Instance.GetByEnum(enumValue);
        }

        public static AudioJob EnumJobInterop(Enum enumValue, CancellationToken cancellationToken = default) {
            var def = EnumInterop(enumValue);
            if (!def) throw new ArgumentException($"No audio definition found for enum value {enumValue}");
            return SpookAudioModule.Instance.CreateJob(def, cancellationToken);
        }

    }
}