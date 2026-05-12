using System.Collections.Generic;
using Dahomey.Cbor.ObjectModel;
using UnityEngine;

namespace Spookline.SPC.Save {
    public class SaveGame {

        public string gameName;
        public int version;
        public Dictionary<string, int> extensions;
        public CborObject data;

        public static void Write(CborObject obj, SaveGame saveGame) {
            obj["gameName"] = saveGame.gameName;
            obj["version"] = saveGame.version;
            var ext = new CborObject();
            foreach (var kvp in saveGame.extensions) {
                ext[kvp.Key] = kvp.Value;
            }

            obj["extensions"] = ext;
            obj["data"] = saveGame.data;
        }

        public static void Read(CborObject obj, SaveGame saveGame) {
            saveGame.gameName = obj["gameName"].Value<string>();
            saveGame.version = obj["version"].Value<int>();
            saveGame.extensions = new Dictionary<string, int>();
            if (obj.ContainsKey("extensions") && obj["extensions"] is CborObject ext) {
                foreach (var kvp in ext) {
                    saveGame.extensions[kvp.Key.Value<string>()] = kvp.Value.Value<int>();
                }
            } else {
                Debug.LogWarning("No extensions found in save data, initializing empty extensions dictionary.");
                saveGame.extensions = new Dictionary<string, int>();
            }

            if (obj.ContainsKey("data") && obj["data"] is CborObject dataObj) {
                saveGame.data = dataObj;
            } else {
                Debug.LogWarning("No data found in save data, initializing empty CborObject.");
                saveGame.data = new CborObject();
            }
        }
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