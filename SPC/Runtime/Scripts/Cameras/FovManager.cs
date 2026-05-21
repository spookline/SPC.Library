using System.Collections.Generic;
using Sirenix.Serialization;
using Spookline.SPC.Ext;
using UnityEngine;

namespace Spookline.SPC.Cameras {
    public class FovManager : SpookManagerBehaviour<FovManager> {

        [SerializeField]
        private float defaultSpeed = 10f;

        [OdinSerialize, SerializeField]
        private ICameraProvider cameraProvider;

        private readonly Dictionary<string, FovSource> _sources = new();

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

        private void Update() {
            if (Mathf.Approximately(_currentFov, _targetFov)) return;
            _currentFov = Mathf.MoveTowards(_currentFov, _targetFov, _currentSpeed * Time.deltaTime);
            cameraProvider.Fov = _currentFov;
        }

        public void AddSource(string id, float targetFov, int priority, float speed) {
            _sources[id] = new FovSource(targetFov, priority, speed);
            Resolve();
        }

        public void RemoveSource(string id) {
            if (_sources.Remove(id)) {
                Resolve();
            }
        }

        public void ClearSources() {
            _sources.Clear();
            Resolve();
        }

        private void Resolve() {
            if (_sources.Count == 0) {
                _targetFov = cameraProvider.BaseFov;
                _currentSpeed = defaultSpeed;
                return;
            }

            FovSource highest = default;
            var highestPriority = int.MinValue;
            foreach (var source in _sources.Values) {
                if (source.priority <= highestPriority) continue;
                highest = source;
                highestPriority = source.priority;
            }

            _targetFov = highest.targetFov;
            _currentSpeed = highest.speed;
        }

        private readonly struct FovSource {

            public readonly float targetFov;
            public readonly int priority;
            public readonly float speed;

            public FovSource(float targetFov, int priority, float speed) {
                this.targetFov = targetFov;
                this.priority = priority;
                this.speed = speed;
            }

        }


        public interface ICameraProvider {

            public float BaseFov { get; }
            public float Fov { get; set; }

        }

    }
}