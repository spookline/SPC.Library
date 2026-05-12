using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Spookline.SPC.Ext;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Spookline.SPC.Animation {
    public class SpookAnimator : IDisposable {

        private const int ActionSlotCount = 3;

        public string Name { get; private set; }
        public Animator Animator { get; private set; }
        public AnimationClip CurrentClip { get; private set; }

        public PlayableGraph Graph { get; }
        public AnimationLayerMixerPlayable Mixer { get; }
        public AnimationPlayableOutput Output { get; }

        private readonly AnimationMixerPlayable _actionMixer;
        private CancellationTokenSource _cts;

        public SpookAnimator(string name, Animator animator, AvatarMask mask = null) {
            Name = name;
            Animator = animator;

            Graph = PlayableGraph.Create(name);
            Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            Output = AnimationPlayableOutput.Create(
                Graph,
                $"{name}-Output",
                animator
            );

            Mixer = AnimationLayerMixerPlayable.Create(Graph, 2);

            _actionMixer = AnimationMixerPlayable.Create(Graph, ActionSlotCount);

            Mixer.ConnectInput(1, _actionMixer, 0);
            Mixer.SetInputWeight(1, 0f);

            if (animator.runtimeAnimatorController) {
                var controllerPlayable = AnimatorControllerPlayable.Create(
                    Graph,
                    animator.runtimeAnimatorController
                );
                Graph.Connect(controllerPlayable, 0, Mixer, 0);
                Mixer.SetInputWeight(0, 1f);
            }

            if (mask) {
                Mixer.SetLayerMaskFromAvatarMask(1, mask);
            }

            Output.SetSourcePlayable(Mixer);
            Graph.Play();
        }

        public async UniTask PlayClip(
            AnimationClip clip,
            float fadeInTime = 0.2f,
            float fadeOutTime = 0.2f,
            bool loop = false,
            bool holdLastFrame = true
        ) {
            if (!clip || !Graph.IsValid()) return;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try {
                CurrentClip = clip;

                Mixer.SetInputWeight(1, 1f);

                var targetIndex = GetBestFreeActionSlot();

                if (_actionMixer.GetInput(targetIndex).IsValid()) {
                    _actionMixer.DisconnectInput(targetIndex);
                }

                var playable = AnimationClipPlayable.Create(Graph, clip);

                playable.SetApplyFootIK(false);

                playable.SetTime(0);
                playable.SetSpeed(1);
                playable.Play();

                _actionMixer.ConnectInput(targetIndex, playable, 0);
                _actionMixer.SetInputWeight(targetIndex, 0f);

                await UniTask.Yield(PlayerLoopTiming.PreLateUpdate, token);

                await FadeToSingleActionSlot(targetIndex, fadeInTime, token);

                if (loop) return;
                var remainingTime = clip.length - fadeInTime;

                if (remainingTime > 0f) {
                    await UniTask.WaitForSeconds(
                        remainingTime,
                        cancellationToken: token
                    );
                }

                if (holdLastFrame) {
                    var lastFrameTime = Mathf.Max(0f, clip.length - 0.0001f);
                    playable.SetTime(lastFrameTime);
                    playable.SetSpeed(0f);
                    playable.Pause();
                    _actionMixer.SetInputWeight(targetIndex, 1f);
                    Mixer.SetInputWeight(1, 1f);
                    CurrentClip = clip;
                    return;
                }

                await FadeWeight(
                    Mixer,
                    1,
                    Mixer.GetInputWeight(1),
                    0f,
                    fadeOutTime,
                    token
                );

                if (!token.IsCancellationRequested) {
                    StopClipImmediate();
                }
            } catch (OperationCanceledException) {
                // A newer PlayClip call took over.
            }
        }

        public async UniTask StopClip(float fadeOutTime = 0.2f) {
            if (!CurrentClip || !Graph.IsValid()) return;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            try {
                await FadeWeight(
                    Mixer,
                    1,
                    Mixer.GetInputWeight(1),
                    0f,
                    fadeOutTime,
                    _cts.Token
                );
                StopClipImmediate();
            } catch (OperationCanceledException) { }
        }

        public void StopClipImmediate() {
            if (!Graph.IsValid()) return;

            Mixer.SetInputWeight(1, 0f);

            for (int i = 0; i < ActionSlotCount; i++) {
                _actionMixer.SetInputWeight(i, 0f);

                if (_actionMixer.GetInput(i).IsValid()) {
                    _actionMixer.DisconnectInput(i);
                }
            }

            CurrentClip = null;
        }

        private int GetBestFreeActionSlot() {
            var bestIndex = 0;
            var bestWeight = float.MaxValue;

            for (var i = 0; i < ActionSlotCount; i++) {
                if (!_actionMixer.GetInput(i).IsValid()) {
                    return i;
                }

                var weight = _actionMixer.GetInputWeight(i);

                if (!(weight < bestWeight)) continue;
                bestWeight = weight;
                bestIndex = i;
            }

            return bestIndex;
        }

        private async UniTask FadeToSingleActionSlot(int targetIndex, float duration, CancellationToken token) {
            var startWeights = new float[ActionSlotCount];

            for (var i = 0; i < ActionSlotCount; i++) {
                startWeights[i] = _actionMixer.GetInputWeight(i);
            }

            if (duration <= 0f) {
                for (var i = 0; i < ActionSlotCount; i++) {
                    _actionMixer.SetInputWeight(
                        i,
                        i == targetIndex ? 1f : 0f
                    );
                }

                DisconnectZeroWeightActionSlotsExcept(targetIndex);
                return;
            }

            var elapsed = 0f;

            while (elapsed < duration) {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);

                for (var i = 0; i < ActionSlotCount; i++) {
                    var targetWeight = i == targetIndex ? 1f : 0f;
                    var weight = Mathf.Lerp(
                        startWeights[i],
                        targetWeight,
                        t
                    );

                    _actionMixer.SetInputWeight(i, weight);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            for (var i = 0; i < ActionSlotCount; i++) {
                _actionMixer.SetInputWeight(
                    i,
                    i == targetIndex ? 1f : 0f
                );
            }

            DisconnectZeroWeightActionSlotsExcept(targetIndex);
        }

        private void DisconnectZeroWeightActionSlotsExcept(int keepIndex) {
            for (var i = 0; i < ActionSlotCount; i++) {
                if (i == keepIndex) continue;

                _actionMixer.SetInputWeight(i, 0f);

                if (_actionMixer.GetInput(i).IsValid()) {
                    _actionMixer.DisconnectInput(i);
                }
            }
        }

        private async UniTask FadeWeight(
            Playable mixer,
            int inputIndex,
            float startWeight,
            float endWeight,
            float duration,
            CancellationToken token
        ) {
            if (duration <= 0f) {
                mixer.SetInputWeight(inputIndex, endWeight);
                return;
            }

            var elapsed = 0f;

            while (elapsed < duration) {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);

                mixer.SetInputWeight(
                    inputIndex,
                    Mathf.Lerp(startWeight, endWeight, t)
                );

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            mixer.SetInputWeight(inputIndex, endWeight);
        }

        public void Dispose() {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            if (Graph.IsValid()) {
                Graph.Destroy();
            }
        }

    }

    public static class SpookAnimatorExtensions {

        public static SpookAnimator ProvideSpookAnimator(this ISpookBehaviour behaviour, string name, Animator animator,
            AvatarMask mask = null) {
            var spookAnimator = new SpookAnimator(name, animator, mask);
            behaviour.DisposeOnDestroy(spookAnimator);
            return spookAnimator;
        }

    }
}