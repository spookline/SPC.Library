using System.Collections.Generic;
using System.Linq;
using Dahomey.Cbor.ObjectModel;
using UnityEngine;

namespace Spookline.SPC.Save {
    public static class PlayerDataExtension {

        public const string PlayerDataKey = "playerData";

        public static bool TryGetPlayerData(this SaveGame saveGame, string uid, out CborObject playerData) {
            playerData = null;
            if (!saveGame.data.TryGetValue(PlayerDataKey, out var value)) return false;
            if (value is not CborObject playerDataObj) return false;
            if (!playerDataObj.TryGetValue(uid, out var playerDataValue)) return false;
            if (playerDataValue is not CborObject dataValue) {
                Debug.LogError($"Player data for UID {uid} is not a CborObject.");
                return false;
            }

            playerData = dataValue;
            return true;
        }

        public static CborObject GetOrCreatePlayerData(this SaveGame saveGame, string uid) {
            if (saveGame.data.TryGetValue(PlayerDataKey, out var value) && value is CborObject playerDataObj) {
                if (playerDataObj.TryGetValue(uid, out var playerDataValue) &&
                    playerDataValue is CborObject dataValue) {
                    return dataValue;
                }
            } else {
                playerDataObj = new CborObject();
                saveGame.data[PlayerDataKey] = playerDataObj;
            }

            var newPlayerData = new CborObject();
            playerDataObj[uid] = newPlayerData;
            return newPlayerData;
        }

        public static HashSet<string> GetPlayerDataUids(this SaveGame saveGame) {
            if (saveGame.data.TryGetValue(PlayerDataKey, out var value) && value is CborObject playerDataObj) {
                return new HashSet<string>(playerDataObj.Keys.Select(k => k.Value<string>()));
            }
            return new HashSet<string>();
        }
    }
}