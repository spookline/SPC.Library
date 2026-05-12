using UnityEngine;

namespace Spookline.SPC.Ext {
    public abstract class SpookManagerBehaviour<TSelf> : SpookBehaviour<TSelf> where TSelf : SpookManagerBehaviour<TSelf> {

        public static TSelf Instance { get; private set; }

        public static bool HasInstance => Instance;

        protected virtual void Awake() {
            if (HasInstance) {
                Debug.LogError($"Instance of {typeof(TSelf).Name} already exists. Destroying this instance.");
                Destroy(gameObject);
                return;
            }

            Instance = (TSelf)this;
        }

    }
}