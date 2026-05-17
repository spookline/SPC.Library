using System.Collections.Generic;
using UnityEngine;

namespace Spookline.SPC.UI.Overlay {
  public class OverlayEntry {

    public ulong id;
    public string title;
    public string subtitle;
    public Vector3 worldPosition;
    public float lastSeenTime;
    public Dictionary<string, OverlayField> fields = new();
    public OverlayElement element;

  }
}