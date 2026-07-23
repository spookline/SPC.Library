using System;
using System.Threading;
using Spookline.SPC.Ext;

namespace Spookline.SPC.Audio {
    /// <summary>Addressables-backed lookup for all loaded audio definitions.</summary>
    public sealed class SpookAudioRegistry : ObjectRegistry<SpookAudioRegistry, AudioDefinition> {

        public const string DefaultAddressableLabel = "audio";

        internal string Label { get; set; } = DefaultAddressableLabel;

        public override string AddressableLabel => string.IsNullOrWhiteSpace(Label)
            ? DefaultAddressableLabel
            : Label;

        public static AudioDefinition EnumInterop(Enum enumValue) {
            if (enumValue == null) throw new ArgumentNullException(nameof(enumValue));
            return Instance.GetByEnum(enumValue);
        }

        public static AudioJob EnumJobInterop(
            Enum enumValue,
            CancellationToken cancellationToken = default
        ) {
            var definition = EnumInterop(enumValue);
            if (!definition)
                throw new ArgumentException($"No audio definition is registered for '{enumValue}'.", nameof(enumValue));
            return SpookAudioModule.Instance.CreateJob(definition, cancellationToken);
        }

    }
}
