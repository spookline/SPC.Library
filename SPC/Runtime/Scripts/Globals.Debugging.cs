using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sirenix.OdinInspector;
using Spookline.SPC.Draw;
using Spookline.SPC.Events;
using UnityEngine;

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
        public HashSet<string> AvailableDebugFlags { get; } = new();


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

        public void RefreshDebugFlags() {
            AvailableDebugFlags.Clear();
            new CollectDebugFlagsEvt { flags = AvailableDebugFlags }.Raise();
        }

        private void SetupLogMessageReceiver() {
            var buffer = ConsoleHistoryBuffer.Instance;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        public void TeardownLogMessageReceiver() {
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type) {
            ConsoleHistoryBuffer.Instance.AddLogMessage(condition, stackTrace, type);
        }

    }

    public struct DebugFlagsChangedEvt : Evt<DebugFlagsChangedEvt> {

        public HashSet<string> flags;
        public bool debugging;

    }

    public struct CollectDebugFlagsEvt : Evt<CollectDebugFlagsEvt> {

        public HashSet<string> flags;

        public void Add(string flag) => flags.Add(flag);
        public void Add(params string[] multiple) {
            foreach (var flag in multiple) this.flags.Add(flag);
        }

    }

    public struct DebugDrawEvt : Evt<DebugDrawEvt> {

        public IDrawingAPI drawer;
        public HashSet<string> flags;
        public bool debugging;

        public readonly bool HasFlag(string flag) => flags.Contains(flag);
        public readonly bool HasFlagOrDebugging(string flag) => HasFlag(flag) || debugging;

    }

    public class ConsoleHistoryBuffer {

        public static ConsoleHistoryBuffer Instance { get; } = new();

        public readonly List<ConsoleLogEntry> messages = new(100);
        public int maxMessages = 100;

        public void AddLogMessage(string message, string stackTrace, LogType type) {
            var entry = new ConsoleLogEntry(message) {
                stackTrace = stackTrace,
                type = LogTypeToExtLogType(type)
            };
            Add(entry);
        }

        public void Add(ConsoleLogEntry entry) {
            if (messages.Count >= maxMessages) messages.RemoveAt(0);
            messages.Add(entry);
            new LogMessageReceivedEvt { entry = entry }.RaiseSafe();
        }

        public void Clear() {
            messages.Clear();
            new LogMessageReceivedEvt { entry = new ConsoleLogEntry() }.RaiseSafe();
        }

        private static ExtLogType LogTypeToExtLogType(LogType type) {
            return type switch {
                LogType.Log => ExtLogType.Log,
                LogType.Warning => ExtLogType.Warning,
                LogType.Error => ExtLogType.Error,
                LogType.Assert => ExtLogType.Assert,
                LogType.Exception => ExtLogType.Exception,
                _ => ExtLogType.Log
            };
        }

    }

    public struct LogMessageReceivedEvt : Evt<LogMessageReceivedEvt> {

        public ConsoleLogEntry entry;

    }

    public struct ConsoleLogEntry {

        public string summary;
        public string message;
        public string stackTrace;
        public ExtLogType type;

        public string GetFullText() => $"{message}\n\n{stackTrace}";

        public ConsoleLogEntry(string message) : this() {
            this.message = message;
            summary = string.Join(
                "\n",
                message.Split('\n', 3).Take(2).Select(s => {
                        if (s.Length > 128) return s[..128].TrimEnd() + "...";
                        return s.Trim();
                    }
                )
            );
        }

    }

    public enum ExtLogType {

        Log,
        Warning,
        Error,
        Assert,
        Exception,
        Input

    }
}