using System;
using System.Collections.Generic;
using HELIX.Widgets.Diagnostics;
using HELIX.Widgets.Signals;
using Spookline.SPC.Debugging;
using Spookline.SPC.Events;

namespace Spookline.SPC.UI {
  public class SpookConsoleHistoryObserver : Signal {

    public readonly List<string> history = new();
    public readonly List<ExtendedLogEntry> messages = new();

    private IDisposable _subscription;
    public bool hasUnhandledUpdate;

    public static SpookConsoleHistoryObserver Instance { get; } = new();

    public void Resubscribe() {
      _subscription?.Dispose();
      _subscription = Evt<LogHistoryChangedEvt>.Subscribe(OnLogHistoryChanged);
    }

    public void AddToHistory(string message) {
      if (history.Contains(message)) history.Remove(message);
      if (history.Count >= 50) history.RemoveAt(history.Count - 1);
      history.Insert(0, message);
    }

    public void Refresh() {
      messages.Clear();
      foreach (var message in LogHistoryBuffer.Instance.messages)
        // Later filter or process them here
        messages.Add(message);

      NotifyDirty();
      NotifyObservers();
      hasUnhandledUpdate = false;
    }

    private void OnLogHistoryChanged(ref LogHistoryChangedEvt args) {
      switch (args.type) {
        case LogHistoryModificationType.Added: {
          // Do not refresh on helix-reported ui errors
          if (args.entry.message?.Contains(HelixDiagnostics.LogSignature, StringComparison.Ordinal) ?? false) return;

          Refresh();
          break;
        }
        case LogHistoryModificationType.RefreshHint: Refresh(); break;
        default: {
          hasUnhandledUpdate = true;
          break;
        }
      }
    }

  }
}