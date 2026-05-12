using Dahomey.Cbor.ObjectModel;
using Spookline.SPC.Events;

namespace Spookline.SPC.Save {
    public class GameLoadEvt : AsyncChainEvt<GameLoadEvt>, ISaveObjectReader {

        public readonly SaveGame saveGame;

        public GameLoadEvt(SaveGame saveGame) {
            this.saveGame = saveGame;
        }
        

        public CborObject BackingObject => saveGame.data;
    }
}