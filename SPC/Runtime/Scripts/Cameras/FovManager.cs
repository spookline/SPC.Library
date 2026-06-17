using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using Spookline.SPC.Cameras;
using Spookline.SPC.Ext;
using UnityEngine;

namespace Spookline.SPC.Cameras {
    public class FovManager : SpookManagerBehaviour<FovManager> {

        [SerializeField]
        private float defaultSpeed = 10f;

        [OdinSerialize, SerializeField]
        private ICameraProvider cameraProvider;

        private readonly List<FovSource> _sources = new();

        private float _currentFov;
        private float _targetFov;
        private float _currentSpeed;

        protected override void Awake() {
            base.Awake();
            _currentFov = cameraProvider.BaseFov;
            _targetFov = _currentFov;
            _currentSpeed = defaultSpeed;
            cameraProvider.Fov = _currentFov;
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            _sources.Clear();
        }

        private void Update() {
            Resolve();
            if (Mathf.Approximately(_currentFov, _targetFov)) return;
            _currentFov = Mathf.MoveTowards(_currentFov, _targetFov, _currentSpeed * 10f * Time.deltaTime);
            cameraProvider.Fov = _currentFov;
        }

        public FovSource AddSource(FovSource source) {
            _sources.Add(source);
            Resolve();
            return source;
        }

        public FovSource RemoveSource(FovSource source) {
            _sources.Remove(source);
            Resolve();
            return source;
        }

        private void Resolve() {
            FovSource highest = null;
            var highestPriority = int.MinValue;
            var found = false;
            foreach (var source in _sources) {
                if (source.condition != null && !source.condition())
                    continue;
                if (source.priority <= highestPriority)
                    continue;
                highest = source;
                highestPriority = source.priority;
                found = true;
            }

            if (!found) {
                _targetFov = cameraProvider.BaseFov;
                _currentSpeed = defaultSpeed;
                return;
            }

            _targetFov = highest.mode == FovValueMode.Constant ? highest.value : highest.value * cameraProvider.BaseFov;
            _currentSpeed = Mathf.Max(0f, highest.speed);
        }


        public interface ICameraProvider {

            public float BaseFov { get; }
            public float Fov { get; set; }

        }

    }
}

public class FovSource : IDisposable {

    public readonly Func<bool> condition;
    public readonly float value;
    public readonly int priority;
    public readonly float speed;
    public FovValueMode mode;

    public FovSource(Func<bool> condition, float value, int priority = 1, float speed = 10f,
        FovValueMode mode = FovValueMode.Constant) {
        this.condition = condition;
        this.value = value;
        this.priority = priority;
        this.speed = speed;
        this.mode = mode;
    }

    public void Dispose() {
        if (!FovManager.HasInstance) {
            Debug.LogWarning("FovManager not found, FovSource will not be disposed");
            return;
        }
        FovManager.Instance.RemoveSource(this);
    }

}

public enum FovValueMode {

    Constant,
    Multiplier,

}

public static class FovSpookBehaviourExtensions {

    public static FovSource AddFovSource(this ISpookBehaviour behaviour, FovSource source) {
        if (!FovManager.HasInstance) {
            Debug.LogWarning("FovManager not found, FovSource will not be registered");
            return null;
        }
        var disposable = FovManager.Instance.AddSource(source);
        behaviour.DisposeOnDestroy(disposable);
        return source;
    }

}