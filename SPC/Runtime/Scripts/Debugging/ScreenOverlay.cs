using UnityEngine;

namespace Spookline.SPC.Debugging {
    public readonly struct ScreenOverlayBuilder {

        public readonly IScreenOverlayAPI api;

        public ScreenOverlayBuilder(IScreenOverlayAPI api) {
            this.api = api;
        }

        public ScreenOverlayBuilder Field(string label, string value, string unit = null, Color? color = null) {
            if (api == null) return this;
            api.UpdateField(label, value, unit, color);
            return this;
        }

        public ScreenOverlayBuilder Field(
            string label,
            float value,
            int decimals = 2,
            string unit = null,
            Color? color = null
        ) {
            if (api == null) return this;
            api.UpdateField(label, value, decimals, unit, color);
            return this;
        }

        public ScreenOverlayBuilder Field(string label, int value, string unit = null, Color? color = null) {
            if (api == null) return this;

            api.UpdateField(label, value, unit, color);
            return this;
        }

        public ScreenOverlayBuilder Field(
            string label,
            Vector3 value,
            int decimals = 1,
            string unit = null,
            Color? color = null
        ) {
            if (api == null) return this;
            api.UpdateField(label, value, decimals, unit, color);
            return this;
        }

    }

    public struct ScreenOverlayInitiator {

        public readonly IScreenOverlayAPI api;

        public ScreenOverlayInitiator(IScreenOverlayAPI api) {
            this.api = api;
        }

        public ScreenOverlayBuilder Section(string title, int order = 0, string subtitle = null) {
            if (api == null) return default;
            api.BeginSection(title, order, subtitle);
            return new ScreenOverlayBuilder(api);
        }

        public ScreenOverlayBuilder Global() {
            if (api == null) return default;
            api.GlobalSection();
            return new ScreenOverlayBuilder(api);
        }

    }

    public interface IScreenOverlayAPI {

        void GlobalSection();

        void BeginSection(string title, int order = 0, string subtitle = null);

        void UpdateField(string label, string value, string unit = null, Color? color = null);

        void UpdateField(
            string label,
            float value,
            int decimals = 1,
            string unit = null,
            Color? color = null
        );

        void UpdateField(string label, int value, string unit = null, Color? color = null);

        void UpdateField(
            string label,
            Vector3 value,
            int decimals = 1,
            string unit = null,
            Color? color = null
        );

        void Tick();

    }

    public class NoOpScreenOverlayAPI : IScreenOverlayAPI {

        public void GlobalSection() { }

        public void BeginSection(string title, int order = 0, string subtitle = null) { }

        public void UpdateField(string label, string value, string unit = null, Color? color = null) { }

        public void UpdateField(
            string label,
            float value,
            int decimals = 1,
            string unit = null,
            Color? color = null
        ) { }

        public void UpdateField(string label, int value, string unit = null, Color? color = null) { }

        public void UpdateField(
            string label,
            Vector3 value,
            int decimals = 1,
            string unit = null,
            Color? color = null
        ) { }

        public void Tick() { }

    }
}