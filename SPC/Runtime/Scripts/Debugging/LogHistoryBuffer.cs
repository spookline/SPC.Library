using System;
using System.Collections.Generic;
using System.Linq;
using Spookline.SPC.Events;
using UnityEngine;

namespace Spookline.SPC.Debugging {
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