using Dahomey.Cbor.ObjectModel;
using Spookline.SPC.Events;

namespace Spookline.SPC.Save {
    public class GameSaveEvt : AsyncChainEvt<GameSaveEvt>, ISaveObjectWriter, ISaveObjectReader {

        public readonly SaveGame saveGame;

        public GameSaveEvt(SaveGame saveGame) {
            this.saveGame = saveGame;
        }

        public GameSaveEvt WithExtension(string key, int version) {
            saveGame.extensions[key] = version;
            return this;
        }

        public CborObject BackingObject => saveGame.data;
    }
}