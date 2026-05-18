using System.Collections.Generic;
using Dahomey.Cbor.ObjectModel;
using Spookline.SPC.Events;

namespace Spookline.SPC.Save {
    public abstract class BaseDataReadEvt<T> : AsyncChainEvt<T>, IDataReader, IVersionAware
        where T : BaseDataReadEvt<T> {

        private readonly IVersionAware _versionAware;

        protected BaseDataReadEvt(CborObject data, IVersionAware versionAware) {
            Obj = data;
            _versionAware = versionAware;
        }

        public CborObject Obj { get; }

        public int Version {
            get => _versionAware.Version;
            set =>
                throw new System.NotSupportedException(
                    "Cannot set version on load events, it is determined by the data being loaded."
                );
        }

        public Dictionary<string, int> Extensions {
            get => _versionAware.Extensions;
            set =>
                throw new System.NotSupportedException(
                    "Cannot set extensions on load events, it is determined by the data being loaded."
                );
        }

    }

    public abstract class BaseDataWriteEvt<T> : AsyncChainEvt<T>,
        IDataWriter, IDataReader, IVersionAware
        where T : BaseDataWriteEvt<T> {

        private readonly IVersionAware _versionAware;

        public CborObject Obj { get; }

        protected BaseDataWriteEvt(CborObject data, IVersionAware versionAware) {
            Obj = data;
            _versionAware = versionAware;
        }

        public int Version {
            get => _versionAware.Version;
            set => _versionAware.Version = value;
        }

        public Dictionary<string, int> Extensions {
            get => _versionAware.Extensions;
            set => _versionAware.Extensions = value;
        }

    }


    public class GameLoadEvt : BaseDataReadEvt<GameLoadEvt> {

        public GameLoadEvt(SaveGame saveGame) : base(saveGame.data, saveGame) { }

    }

    public class PlayerDataLoadEvt : BaseDataReadEvt<PlayerDataLoadEvt> {

        public PlayerDataLoadEvt(PlayerData data) : base(data.data, data) { }

    }

    public class PlayerConfigLoadEvt : BaseDataReadEvt<PlayerConfigLoadEvt> {

        public PlayerConfigLoadEvt(PlayerData data) : base(data.config, data) { }

    }

    public class GameSaveEvt : BaseDataWriteEvt<GameSaveEvt> {

        public GameSaveEvt(SaveGame saveGame) : base(saveGame.data, saveGame) { }

    }

    public class PlayerDataSaveEvt : BaseDataWriteEvt<PlayerDataSaveEvt> {

        public PlayerDataSaveEvt(PlayerData data) : base(data.data, data) { }

    }

    public class PlayerConfigSaveEvt : BaseDataWriteEvt<PlayerConfigSaveEvt> {

        public PlayerConfigSaveEvt(PlayerData data) : base(data.config, data) { }

    }
}