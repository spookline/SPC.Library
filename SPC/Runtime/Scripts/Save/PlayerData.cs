using System.Collections.Generic;
using Dahomey.Cbor.ObjectModel;
using UnityEngine;

namespace Spookline.SPC.Save {
    public class PlayerData : IVersionAware {

        public int Version { get; set; }
        public Dictionary<string, int> Extensions { get; set; }
        public CborObject data;
        public CborObject config;

        public PlayerData() {
            Extensions = new Dictionary<string, int>();
            data = new CborObject();
            config = new CborObject();
        }

        public static void Write(CborObject obj, PlayerData playerData) {
            obj["version"] = playerData.Version;
            var ext = new CborObject();
            foreach (var kvp in playerData.Extensions) { ext[kvp.Key] = kvp.Value; }

            obj["extensions"] = ext;
            obj["data"] = playerData.data;
            obj["config"] = playerData.config;
        }

        public static void Read(CborObject obj, PlayerData playerData) {
            playerData.Version = obj["version"].Value<int>();
            playerData.Extensions = new Dictionary<string, int>();
            if (obj.ContainsKey("extensions") && obj["extensions"] is CborObject ext) {
                foreach (var kvp in ext) { playerData.Extensions[kvp.Key.Value<string>()] = kvp.Value.Value<int>(); }
            } else {
                Debug.LogWarning("No extensions found in player data, initializing empty extensions dictionary.");
                playerData.Extensions = new Dictionary<string, int>();
            }

            if (obj.ContainsKey("data") && obj["data"] is CborObject dataObj) { playerData.data = dataObj; } else {
                Debug.LogWarning("No data found in player data, initializing empty CborObject.");
                playerData.data = new CborObject();
            }

            if (obj.ContainsKey("config") && obj["config"] is CborObject configObj) {
                playerData.config = configObj;
            } else {
                Debug.LogWarning("No config found in player data, initializing empty CborObject.");
                playerData.config = new CborObject();
            }
        }

    }
}