using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Dahomey.Cbor;
using Dahomey.Cbor.ObjectModel;
using Spookline.SPC.Debugging;
using Spookline.SPC.Events;
using Spookline.SPC.Ext;
using UnityEngine;

namespace Spookline.SPC.Save {
    [CreateAssetMenu(fileName = "SpookSaveModule", menuName = "Modules/SpookSaveModule", order = 0)]
    public class SpookSaveModule : OdinModule<SpookSaveModule> {

        public string gameName = "Spookline Game";
        public int version;
        public bool IsSaving { get; private set; } = false;
        public bool IsSavingPlayerData { get; private set; } = false;

        [NonSerialized]
        public CborObject config = new();

        private bool _playerDataDirty = false;
        private bool _playerDataLoading = false;

        private static string SaveDirectory {
            get {
                var dataPath = Application.persistentDataPath.Replace("/", Path.DirectorySeparatorChar.ToString());
                return Path.Combine(dataPath, "Saves");
            }
        }

        private static string FileNameToPath(string fileName) {
            return Path.Combine(SaveDirectory, $"{fileName}.save");
        }

        public override void Load() {
            base.Load();
            On<GlobalStartEvt>().ChainDo(LoadPlayerData, EventPriority.Earlier);
            On<PlayerConfigSaveEvt>().ChainDo(SystemPlayerConfigSave);
            On<PlayerConfigLoadEvt>().ChainDo(SystemPlayerConfigLoad);
            On<DebugFlagsChangedEvt>().Do(OnDebugFlagsChanged);
            On<GlobalTickEvt>().Do(OnGlobalTick);
        }

        public void MarkPlayerDataDirty() {
            if (_playerDataLoading) return;
            _playerDataDirty = true;
        }

        private void OnGlobalTick(ref GlobalTickEvt args) {
            if (_playerDataDirty && !_playerDataLoading && !IsSavingPlayerData) {
                SavePlayerData().Forget();
                _playerDataDirty = false;
            }
        }

        private void OnDebugFlagsChanged(ref DebugFlagsChangedEvt args) {
            MarkPlayerDataDirty();
        }

        private void SystemPlayerConfigLoad(PlayerConfigLoadEvt arg) {
            if (arg.TryReadData(DebugConfig.Key, out var debugValue)) {
                if (debugValue is not CborObject dict) return;
                var debugConfig = DebugConfig.Decode(dict);
                debugConfig.Apply();
            }
        }

        private void SystemPlayerConfigSave(PlayerConfigSaveEvt arg) {
            var debugConfig = new DebugConfig();
            debugConfig.Load();
            arg.WriteData(DebugConfig.Key, DebugConfig.Encode(debugConfig));
        }

        private async UniTask LoadPlayerData(GlobalStartEvt arg) {
            _playerDataLoading = true;
            try {
                var playerData = LoadPlayerDataFile();
                if (playerData != null) {
                    config = playerData.config;
                    await new PlayerConfigLoadEvt(playerData).RaiseAsync();
                    await new PlayerDataLoadEvt(playerData).RaiseAsync();
                }
            } finally { _playerDataLoading = false; }
        }

        public async UniTask SavePlayerData() {
            IsSavingPlayerData = true;
            try {
                var playerData = CreatePlayerDataContainer();
                playerData.config = config;
                await new PlayerConfigSaveEvt(playerData).RaiseAsync();
                await new PlayerDataSaveEvt(playerData).RaiseAsync();
                WritePlayerDataFile(playerData);
            } finally { IsSavingPlayerData = false; }
        }

        private PlayerData CreatePlayerDataContainer() {
            var playerData = new PlayerData {
                Version = version,
                Extensions = new Dictionary<string, int>(),
                data = new CborObject(),
                config = new CborObject()
            };
            return playerData;
        }

        private SaveGame CreateSaveGameContainer() {
            var saveGame = new SaveGame {
                gameName = gameName,
                Version = version,
                Extensions = new Dictionary<string, int>(),
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
                saveFiles.Add(
                    new SaveFileEntry {
                        Name = visibleName,
                        Time = time,
                        FileName = fileName
                    }
                );
            }

            return saveFiles.OrderByDescending(x => x.Time).ToList();
        }

        public void SaveToFile(SaveGame state, string fileName = null) {
            if (string.IsNullOrEmpty(fileName)) {
                var saves = GetSaveFiles();
                fileName = $"Save_{saves.Count + 1}";
            }

            if (!Directory.Exists(SaveDirectory)) { Directory.CreateDirectory(SaveDirectory); }

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
            } else { Debug.LogError($"Save file {fileName} does not exist."); }
        }

        public PlayerData LoadPlayerDataFile() {
            var path = Path.Combine(Application.persistentDataPath, "player.cbor");
            if (!File.Exists(path)) {
                Debug.LogWarning($"Player data file not found at {path}");
                return null;
            }

            var bytes = File.ReadAllBytes(path);
            var obj = Cbor.Deserialize<CborObject>(bytes);

            var playerData = new PlayerData();
            PlayerData.Read(obj, playerData);
            Debug.Log($"Loaded player data from {path}");
            return playerData;
        }

        public void WritePlayerDataFile(PlayerData playerData) {
            var path = Path.Combine(Application.persistentDataPath, "player.cbor");
            var cborData = new CborObject();
            PlayerData.Write(cborData, playerData);

            var arrayBufferWriter = new ArrayBufferWriter<byte>();
            Cbor.Serialize(cborData, arrayBufferWriter);
            File.WriteAllBytes(path, arrayBufferWriter.WrittenSpan.ToArray());
            Debug.Log($"Player data saved to {path}");
        }

        public string JsonDumpSaveGame(SaveGame saveGame) {
            var obj = new CborObject();
            Save.SaveGame.Write(obj, saveGame);
            var arrayBufferWriter = new ArrayBufferWriter<byte>();
            Cbor.Serialize(obj, arrayBufferWriter);
            return Cbor.ToJson(arrayBufferWriter.WrittenSpan);
        }

        public string JsonDumpPlayerData(PlayerData playerData) {
            var obj = new CborObject();
            PlayerData.Write(obj, playerData);
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