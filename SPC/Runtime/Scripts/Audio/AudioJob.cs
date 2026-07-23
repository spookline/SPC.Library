using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Spookline.SPC.Audio {
    /// <summary>An immutable, allocation-free playback request.</summary>
    public readonly struct AudioJob {

        public readonly AudioDefinition definition;
        public readonly int data;
        public readonly AudioOptions options;
        public readonly CancellationToken cancellationToken;

        public AudioJob(
            AudioDefinition definition,
            int data,
            AudioOptions options,
            CancellationToken cancellationToken = default
        ) {
            this.definition = definition;
            this.data = data;
            this.options = options;
            this.cancellationToken = cancellationToken;
        }

        public bool IsValid => definition && definition.provider != null;

        public AudioJob WithOptions(AudioOptions newOptions) {
            return new AudioJob(definition, data, newOptions, cancellationToken);
        }

        public AudioJob With(Func<AudioOptions, AudioOptions> modifier) {
            if (modifier == null) throw new ArgumentNullException(nameof(modifier));
            return WithOptions(modifier(options));
        }

        public AudioJob WithToken(CancellationToken token) {
            return new AudioJob(definition, data, options, token);
        }

        public SerializedAudioJob Serialize() {
            if (!definition) throw new InvalidOperationException("Cannot serialize an invalid audio job.");
            return new SerializedAudioJob {
                guid = definition.assetGuid,
                data = data,
                options = options
            };
        }

        public async UniTask<AudioJobReference> Unstarted() {
            var reference = PreparePendingReference();
            await SpookAudioModule.Instance.Unstarted(reference, cancellationToken);
            return reference;
        }

        public AudioJobReference UnstartedSync() {
            var reference = PreparePendingReference();
            SpookAudioModule.Instance.Unstarted(reference, cancellationToken).Forget();
            return reference;
        }

        public AudioJobReference UnstartedSync(out UniTask readyTask) {
            var reference = PreparePendingReference();
            readyTask = SpookAudioModule.Instance.Unstarted(reference, cancellationToken);
            return reference;
        }

        public AudioJobReference Play() {
            var reference = PreparePendingReference();
            SpookAudioModule.Instance.Play(reference, cancellationToken).Forget();
            return reference;
        }

        public AudioJobReference PlayAt(Vector3 position) {
            var reference = PreparePendingReference();
            SpookAudioModule.Instance.Play(reference, position, cancellationToken).Forget();
            return reference;
        }

        public AudioJobReference PlayTracked(Transform target) {
            var reference = PreparePendingReference();
            SpookAudioModule.Instance.Play(reference, target, cancellationToken).Forget();
            return reference;
        }

        public AudioClip GetClip() => SpookAudioModule.Instance.GetClip(this);

        public AudioJobReference CreateNewReference() => new() { job = this };

        private AudioJobReference PreparePendingReference() {
            var reference = CreateNewReference();
            reference.MarkPending();
            return reference;
        }

    }
}
