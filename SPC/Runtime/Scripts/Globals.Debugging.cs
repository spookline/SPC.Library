using System.Collections.Generic;
using Sirenix.OdinInspector;
using Spookline.SPC.Draw;
using Spookline.SPC.Events;

namespace Spookline.SPC {
    public partial class Globals {

        public static bool IsDebugging => Instance?.Debugging ?? GetEnvironmentDebugging();

        private static bool GetEnvironmentDebugging() {
#if DEBUG
            return true;
#endif
            return false;
        }

        public static bool HasDebugFlag(string flag) => Instance?.DebugFlags.Contains(flag) ?? false;
        public static bool HasDebugFlagOrDebugging(string flag) => IsDebugging || HasDebugFlag(flag);

        [ShowInInspector, HideInEditorMode]
        public bool Debugging { get; set; }

        [ShowInInspector, HideInEditorMode]
        public bool DebugDraw { get; set; } = true;

        public HashSet<string> DebugFlags { get; } = new();


        [ShowInInspector, LabelText("Debug Flags"), HideInEditorMode]
        private ISet<string> EditorDebugFlags {
            get => DebugFlags;
            set => SetDebugFlags(value);
        }

        public void SetDebugFlags(IEnumerable<string> flags) {
            DebugFlags.Clear();
            foreach (var flag in flags) DebugFlags.Add(flag);
            new DebugFlagsChangedEvt { flags = DebugFlags, debugging = Debugging }.Raise();
        }

        public void SetDebugFlag(string flag) {
            DebugFlags.Add(flag);
            new DebugFlagsChangedEvt { flags = DebugFlags, debugging = Debugging }.Raise();
        }

        public void RemoveDebugFlag(string flag) {
            DebugFlags.Remove(flag);
            new DebugFlagsChangedEvt { flags = DebugFlags, debugging = Debugging }.Raise();
        }

        public void ToggleDebugFlag(string flag) {
            if (DebugFlags.Contains(flag)) RemoveDebugFlag(flag);
            else SetDebugFlag(flag);
        }

    }

    public struct DebugFlagsChangedEvt : Evt<DebugFlagsChangedEvt> {

        public HashSet<string> flags;
        public bool debugging;

    }

    public struct DebugDrawEvt : Evt<DebugDrawEvt> {

        public IDrawingAPI drawer;
        public HashSet<string> flags;
        public bool debugging;

        public readonly bool HasFlag(string flag) => flags.Contains(flag);
        public readonly bool HasFlagOrDebugging(string flag) => HasFlag(flag) || debugging;

    }
}