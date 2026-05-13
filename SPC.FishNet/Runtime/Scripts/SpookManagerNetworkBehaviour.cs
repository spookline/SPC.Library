using UnityEngine;

namespace Spookline.SPC.FishNet {
    public class SpookManagerNetworkBehaviour<TSelf> : SpookNetworkBehaviour<TSelf>
        where TSelf : SpookManagerNetworkBehaviour<TSelf> {

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