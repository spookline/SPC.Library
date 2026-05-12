using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Spookline.SPC.Visual {
    [Serializable]
    public class TweenVolume {

        public float Weight => volume != null ? volume.weight : 0f;

        public Volume volume;
        
        [Header("Default Tween Settings")]
        public float fadeInDuration = 0.5f;
        public float fadeOutDuration = 0.5f;
        
        public Func<float, float> defaultEase = t => t;

        private UniTask _tweenTask;
        private CancellationTokenSource _cancellationTokenSource;
        
        public async UniTask FadeIn(Func<float, float> ease = null) {
            await Fade(1f, fadeInDuration, ease);
        }
        
        public async UniTask FadeOut(Func<float, float> ease = null) {
            await Fade(0f, fadeOutDuration, ease);
        }

        public async UniTask Fade(float weight, float duration, Func<float, float> ease = null) {
            if (volume == null) return;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            
            ease ??= defaultEase;

            var startWeight = volume.weight;
            var elapsed = 0f;
            
            var distance = Mathf.Abs(weight - startWeight);
            var speed = distance / duration;

            try {
                while (!Mathf.Approximately(volume.weight, startWeight)) {
                    token.ThrowIfCancellationRequested();
                    
                    elapsed += Time.deltaTime;
                    var time = Mathf.Clamp01(elapsed / duration);
                    var easedTime = ease(time);
                    
                    var newWeight = Mathf.Lerp(startWeight, weight, easedTime);
                    volume.weight = Mathf.MoveTowards(volume.weight, newWeight, speed * Time.deltaTime);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
                volume.weight = weight;
            } catch (OperationCanceledException) {
                
            }
        }

        public void SetWeightImmediate(float weight) {
            if (volume == null) return;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            volume.weight = weight;
        }

    }
}