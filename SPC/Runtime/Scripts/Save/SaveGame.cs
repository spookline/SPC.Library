using System.Collections.Generic;
using Dahomey.Cbor.ObjectModel;
using UnityEngine;

namespace Spookline.SPC.Save {
    public class SaveGame : IVersionAware {

        public string gameName;
        public int Version { get; set; }
        public Dictionary<string, int> Extensions { get; set; }
        public CborObject data;

        public static void Write(CborObject obj, SaveGame saveGame) {
            obj["gameName"] = saveGame.gameName;
            obj["version"] = saveGame.Version;
            var ext = new CborObject();
            foreach (var kvp in saveGame.Extensions) { ext[kvp.Key] = kvp.Value; }

            obj["extensions"] = ext;
            obj["data"] = saveGame.data;
        }

        public static void Read(CborObject obj, SaveGame saveGame) {
            saveGame.gameName = obj["gameName"].Value<string>();
            saveGame.Version = obj["version"].Value<int>();
            saveGame.Extensions = new Dictionary<string, int>();
            if (obj.ContainsKey("extensions") && obj["extensions"] is CborObject ext) {
                foreach (var kvp in ext) { saveGame.Extensions[kvp.Key.Value<string>()] = kvp.Value.Value<int>(); }
            } else {
                Debug.LogWarning("No extensions found in save data, initializing empty extensions dictionary.");
                saveGame.Extensions = new Dictionary<string, int>();
            }

            if (obj.ContainsKey("data") && obj["data"] is CborObject dataObj) { saveGame.data = dataObj; } else {
                Debug.LogWarning("No data found in save data, initializing empty CborObject.");
                saveGame.data = new CborObject();
            }
        }

    }
}