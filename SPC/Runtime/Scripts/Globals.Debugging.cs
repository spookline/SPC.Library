using System;
using System.Collections.Generic;
using System.Linq;
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
            var buffer = LogHistoryBuffer.Instance;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        public void TeardownLogMessageReceiver() {
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type) {
            LogHistoryBuffer.Instance.AddLogMessage(condition, stackTrace, type);
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

    public class LogHistoryBuffer {

        public static LogHistoryBuffer Instance { get; } = new();

        public readonly List<ExtendedLogEntry> messages = new(100);
        public int maxMessages = 100;
        public bool collapseRepeats = true;

        public void AddLogMessage(string message, string stackTrace, LogType type) {
            var entry = new ExtendedLogEntry(message) {
                stackTrace = stackTrace,
                type = LogTypeToExtLogType(type)
            };
            Add(entry);
        }

        public void Add(ExtendedLogEntry entry) {
            new LogMessageReceivedEvt { entry = entry }.RaiseSafe();
            if (UpdateAlike(entry)) return;
            if (messages.Count >= maxMessages) messages.RemoveAt(0);
            messages.Add(entry);
            new LogHistoryChangedEvt { entry = entry, type = LogHistoryModificationType.Added }.RaiseSafe();
        }

        private bool UpdateAlike(ExtendedLogEntry entry) {
            if (messages.Count == 0) return false;
            for (var i = 0; i < messages.Count; i++) {
                var message = messages[i];
                if (!string.Equals(message.message, entry.message, StringComparison.Ordinal)) continue;
                messages.RemoveAt(i);
                message.stackTrace = entry.stackTrace;
                message.type = entry.type;
                if (message.repeatCount < int.MaxValue) message.repeatCount++;
                messages.Add(message);
                new LogHistoryChangedEvt {
                    entry = message,
                    type = LogHistoryModificationType.Repeated
                }.RaiseSafe();
                return true;
            }

            return false;
        }

        public void SendRefreshHint() {
            new LogHistoryChangedEvt {
                type = LogHistoryModificationType.RefreshHint
            }.RaiseSafe();
        }


        public void Clear() {
            messages.Clear();
            new LogHistoryChangedEvt {
                type = LogHistoryModificationType.Cleared
            }.RaiseSafe();
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

    public interface ILogEvent { }

    public struct LogMessageReceivedEvt : Evt<LogMessageReceivedEvt>, ILogEvent {

        public ExtendedLogEntry entry;
        public bool isRepeat;

    }

    public struct LogHistoryChangedEvt : Evt<LogHistoryChangedEvt>, ILogEvent {

        public ExtendedLogEntry entry;
        public LogHistoryModificationType type;

    }

    public enum LogHistoryModificationType {

        Added = 0,
        Repeated = 1,
        Cleared = 2,
        RefreshHint = 3,

    }

    public struct ExtendedLogEntry {

        public string summary;
        public string message;
        public string stackTrace;
        public ExtLogType type;
        public int repeatCount;

        public string GetFullText() => $"{message}\n\n{stackTrace}";

        public ExtendedLogEntry(string message) : this() {
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