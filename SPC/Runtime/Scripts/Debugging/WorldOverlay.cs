using UnityEngine;

namespace Spookline.SPC.Debugging {
    public readonly struct WorldOverlayBuilder {

        public readonly ulong id;
        public readonly IWorldOverlayAPI api;

        public WorldOverlayBuilder(ulong id, IWorldOverlayAPI api) {
            this.id = id;
            this.api = api;
        }

        public WorldOverlayBuilder Field(string label, string value, string unit = null, Color? color = null) {
            if (id == 0 || api == null) return this;
            ;
            api.UpdateField(id, label, value, unit, color);
            return this;
        }

        public WorldOverlayBuilder Field(
            string label,
            float value,
            int decimals = 2,
            string unit = null,
            Color? color = null
        ) {
            if (id == 0 || api == null) return this;
            ;
            api.UpdateField(id, label, value, decimals, unit, color);
            return this;
        }

        public WorldOverlayBuilder Field(string label, int value, string unit = null, Color? color = null) {
            if (id == 0 || api == null) return this;
            ;
            api.UpdateField(id, label, value, unit, color);
            return this;
        }

        public WorldOverlayBuilder Field(
            string label,
            Vector3 value,
            int decimals = 1,
            string unit = null,
            Color? color = null
        ) {
            if (id == 0 || api == null) return this;
            api.UpdateField(id, label, value, decimals, unit, color);
            return this;
        }

    }

    public readonly struct WorldOverlayInitiator {

        public readonly IWorldOverlayAPI api;

        public WorldOverlayInitiator(IWorldOverlayAPI api) {
            this.api = api;
        }

        public WorldOverlayBuilder Box(ulong id, string title, Vector3 worldPosition, string subtitle = null) {
            if (api == null) return default;
            api.BeginEntry(id, title, worldPosition, subtitle);
            return new WorldOverlayBuilder(api.CurrentId, api);
        }

        public WorldOverlayBuilder Box(string key, string title, Vector3 worldPosition, string subtitle = null) {
            if (api == null) return default;
            api.BeginEntry(key, title, worldPosition, subtitle);
            return new WorldOverlayBuilder(api.CurrentId, api);
        }

        public WorldOverlayBuilder Box(string title, Vector3 worldPosition, string subtitle = null) {
            if (api == null) return default;
            api.UpdateEntry(api.CurrentId, title, worldPosition, subtitle);
            return new WorldOverlayBuilder(api.CurrentId, api);
        }

        public WorldOverlayBuilder Box(Vector3 worldPosition) {
            if (api == null) return default;
            api.BeginEntry(worldPosition);
            return new WorldOverlayBuilder(api.CurrentId, api);
        }

        public WorldOverlayBuilder Continue(ulong id) {
            if (api == null) return default;
            return !api.ContinueEntry(id) ? default : new WorldOverlayBuilder(api.CurrentId, api);
        }

        public WorldOverlayBuilder Continue(string key) {
            if (api == null) return default;
            return !api.ContinueEntry(key) ? default : new WorldOverlayBuilder(api.CurrentId, api);
        }

    }


    public interface IWorldOverlayAPI {

        ulong CurrentId { get; set; }

        ulong UpdateEntry(ulong id, string title, Vector3 worldPosition, string subtitle = null);

        void BeginEntry(ulong id, string title, Vector3 worldPosition, string subtitle = null);
        void BeginEntry(string key, string title, Vector3 worldPosition, string subtitle = null);
        void BeginEntry(Vector3 worldPosition);

        bool ContinueEntry(ulong id);
        bool ContinueEntry(string key);


        void UpdateField(ulong id, string label, string value, string unit = null, Color? color = null);

        void UpdateField(
            ulong id,
            string label,
            float value,
            int decimals = 1,
            string unit = null,
            Color? color = null
        );

        void UpdateField(ulong id, string label, int value, string unit = null, Color? color = null);

        void UpdateField(
            ulong id,
            string label,
            Vector3 value,
            int decimals = 1,
            string unit = null,
            Color? color = null
        );


        void Tick();
        void SetCamera(Camera cam);

    }

    public class NoOpWorldOverlayAPI : IWorldOverlayAPI {

        public ulong CurrentId { get; set; }

        public ulong UpdateEntry(ulong id, string title, Vector3 worldPosition, string subtitle = null) {
            return id;
        }

        public void BeginEntry(ulong id, string title, Vector3 worldPosition, string subtitle = null) { }
        public void BeginEntry(string key, string title, Vector3 worldPosition, string subtitle = null) { }
        public void BeginEntry(Vector3 worldPosition) { }

        public bool ContinueEntry(ulong id) {
            return false;
        }

        public bool ContinueEntry(string key) {
            return false;
        }

        public void UpdateField(ulong id, string label, string value, string unit = null, Color? color = null) { }

        public void UpdateField(
            ulong id,
            string label,
            float value,
            int decimals = 1,
            string unit = null,
            Color? color = null
        ) { }

        public void UpdateField(ulong id, string label, int value, string unit = null, Color? color = null) { }

        public void UpdateField(
            ulong id,
            string label,
            Vector3 value,
            int decimals = 1,
            string unit = null,
            Color? color = null
        ) { }

        public void Tick() { }
        public void SetCamera(Camera cam) { }

    }
}