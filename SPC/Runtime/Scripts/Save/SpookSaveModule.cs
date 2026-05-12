using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Dahomey.Cbor;
using Dahomey.Cbor.ObjectModel;
using Spookline.SPC.Ext;
using UnityEngine;

namespace Spookline.SPC.Save {
    [CreateAssetMenu(fileName = "SpookSaveModule", menuName = "Modules/SpookSaveModule", order = 0)]
    public class SpookSaveModule : OdinModule<SpookSaveModule> {

        public string gameName = "Spookline Game";
        public int version;
        public bool IsSaving { get; private set; } = false;

        private static string SaveDirectory {
            get {
                var dataPath = Application.persistentDataPath.Replace("/", Path.DirectorySeparatorChar.ToString());
                return Path.Combine(dataPath, "Saves");
            }
        }

        private static string FileNameToPath(string fileName) {
            return Path.Combine(SaveDirectory, $"{fileName}.save");
        }

        private SaveGame CreateSaveGameContainer() {
            var saveGame = new SaveGame {
                gameName = gameName,
                version = version,
                extensions = new Dictionary<string, int>(),
                data = new CborObject()
            };
            return saveGame;
        }

        public async UniTask<SaveGame> SaveGame() {
            if (IsSaving) {
                Debug.LogWarning("Save operation already in progress.");
                return null;
            }

            IsSaving = true;
            var saveGame = CreateSaveGameContainer();
            await new GameSaveEvt(saveGame).RaiseAsync();
            IsSaving = false;
            return saveGame;
        }

        public async UniTask TriggerLoad(SaveGame saveGame) {
            if (IsSaving) {
                Debug.LogWarning("Save operation already in progress. Cannot load while saving.");
                return;
            }

            await new GameLoadEvt(saveGame).RaiseAsync();
        }

        public List<SaveFileEntry> GetSaveFiles() {
            var saveFiles = new List<SaveFileEntry>();
            if (!Directory.Exists(SaveDirectory)) {
                Directory.CreateDirectory(SaveDirectory);
                return saveFiles;
            }

            var files = Directory.GetFiles(SaveDirectory, "*.save");
            foreach (var file in files) {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var visibleName = fileName.Replace("_", " ");
                var time = File.GetLastWriteTime(file).ToString("yyyy-MM-dd HH:mm");
                saveFiles.Add(new SaveFileEntry {
                    Name = visibleName,
                    Time = time,
                    FileName = fileName
                });
            }

            return saveFiles.OrderByDescending(x => x.Time).ToList();
        }

        public void SaveToFile(SaveGame state, string fileName = null) {
            if (string.IsNullOrEmpty(fileName)) {
                var saves = GetSaveFiles();
                fileName = $"Save_{saves.Count + 1}";
            }

            if (!Directory.Exists(SaveDirectory)) {
                Directory.CreateDirectory(SaveDirectory);
            }

            var obj = new CborObject();
            Save.SaveGame.Write(obj, state);
            var filePath = FileNameToPath(fileName);

            var arrayBufferWriter = new ArrayBufferWriter<byte>();
            Cbor.Serialize(obj, arrayBufferWriter);
            File.WriteAllBytes(filePath, arrayBufferWriter.WrittenSpan.ToArray());
        }

        public SaveGame LoadSaveFile(SaveFileEntry entry) {
            if (entry == null || string.IsNullOrEmpty(entry.FileName)) {
                Debug.LogError("Invalid save file entry.");
                return null;
            }

            return LoadSaveFile(entry.FileName);
        }

        public SaveGame LoadSaveFile(string fileName) {
            var filePath = FileNameToPath(fileName);
            if (!File.Exists(filePath)) {
                Debug.LogError($"Save file {fileName} does not exist.");
                return null;
            }

            var bytes = File.ReadAllBytes(filePath);
            var obj = Cbor.Deserialize<CborObject>(bytes);
            var saveGame = new SaveGame();
            Save.SaveGame.Read(obj, saveGame);
            return saveGame;
        }

        public void DeleteSaveFile(SaveFileEntry entry) {
            if (entry == null || string.IsNullOrEmpty(entry.FileName)) {
                Debug.LogError("Invalid save file entry.");
                return;
            }

            DeleteSaveFile(entry.FileName);
        }

        public void DeleteSaveFile(string fileName) {
            var filePath = FileNameToPath(fileName);
            if (File.Exists(filePath)) {
                File.Delete(filePath);
                Debug.Log($"Deleted save file: {fileName}");
            } else {
                Debug.LogError($"Save file {fileName} does not exist.");
            }
        }

        public string JsonDumpSaveGame(SaveGame saveGame) {
            var obj = new CborObject();
            Save.SaveGame.Write(obj, saveGame);
            var arrayBufferWriter = new ArrayBufferWriter<byte>();
            Cbor.Serialize(obj, arrayBufferWriter);
            return Cbor.ToJson(arrayBufferWriter.WrittenSpan);
        }

    }

    public class SaveFileEntry {

        public string Name { get; set; }
        public string Time { get; set; }
        public string FileName { get; set; }

    }
}