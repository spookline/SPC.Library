using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Dahomey.Cbor;
using Dahomey.Cbor.ObjectModel;
using Dahomey.Cbor.Serialization;
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

    public interface IVersionAware {

        public int Version { get; set; }
        public Dictionary<string, int> Extensions { get; set; }

    }

    public interface ISaveObjectWriter {

        public CborObject BackingObject { get; }

    }

    public interface ISaveObjectReader {

        public CborObject BackingObject { get; }

    }

    public static class ReaderExtensions {

        public static CborValue ReadData(this ISaveObjectReader reader, string key) {
            return reader.BackingObject.TryGetValue(key, out var value) ? value : null;
        }

        public static bool TryReadData(this ISaveObjectReader reader, string key, out CborValue value) {
            if (reader.BackingObject.TryGetValue(key, out var cborValue)) {
                value = cborValue;
                return true;
            }

            value = null;
            return false;
        }

        public static bool TryReadData<T>(this ISaveObjectReader reader, string key, out T value) {
            if (reader.BackingObject.TryGetValue(key, out var cborValue)) {
                try {
                    value = cborValue.Value<T>();
                    return true;
                } catch {
                    value = default;
                    return false;
                }
            }

            value = default;
            return false;
        }

    }

    public static class WriterExtensions {

        public static void WriteData(this ISaveObjectWriter writer, string key, CborValue value) {
            if (value == null) {
                Debug.LogWarning($"Attempted to write null value for key '{key}'");
                return;
            }

            writer.BackingObject[key] = value;
        }

    }
}