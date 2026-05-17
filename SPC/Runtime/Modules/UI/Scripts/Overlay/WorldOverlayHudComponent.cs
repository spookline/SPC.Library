using System;
using System.Collections.Generic;
using HELIX.Widgets;
using Spookline.SPC.Common;
using Spookline.SPC.Debugging;
using Spookline.SPC.Events;
using Spookline.SPC.Ext;
using Spookline.SPC.Geometry;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.UI.Overlay {
  public class WorldOverlayHudComponent : SpookManagerBehaviour<WorldOverlayHudComponent>, IWorldOverlayAPI {

    [SerializeField]
    private bool _sortDepth = true;
    [SerializeField]
    private Camera _camera;

    [Header("Scaling")]
    [SerializeField]
    private float _globalScale = 1.0f;

    [Header("Distance Scaling")]
    [SerializeField]
    private bool _enableDistanceScaling = true;
    [SerializeField]
    private float _scalingReferenceDistance = 10f;
    [SerializeField]
    private float _minScale = 0.5f;
    [SerializeField]
    private float _maxScale = 1.5f;

    private readonly Dictionary<ulong, OverlayEntry> _entries = new();
    private readonly HashSet<string> _activeFieldsHelper = new();
    private VisualElement _root;
    private Vector3 _cachedCamPos;
    private readonly List<ulong> _entriesToRemove = new();
    private readonly List<string> _fieldsToRemove = new();
    private Comparison<VisualElement> _depthComparison;

    private const float CleanupThreshold = 2.0f; // Seconds before full disposal

    protected override void Awake() {
      base.Awake();
      _depthComparison = (a, b) => {
        if (a is not OverlayElement elemA || b is not OverlayElement elemB) return 0;
        var distA = (elemA.worldPosition - _cachedCamPos).sqrMagnitude;
        var distB = (elemB.worldPosition - _cachedCamPos).sqrMagnitude;
        return distB.CompareTo(distA);
      };
      _root = new VisualElement {
        name = "OverlayRoot",
        style = {
          flexGrow = 1
        },
        pickingMode = PickingMode.Ignore
      };
      On<DebugFlagsChangedEvt>().Do(OnDebugFlagsChanged);
    }

    protected override void OnEnable() {
      base.OnEnable();
      CachedNumberFormat.Init();
      if (PlayerHudManager.HasInstance) {
        PlayerHudManager.Instance.SetComponent("overlay_system", _root);
        if (Globals.Instance) { Globals.Instance.debugWorldOverlay = this; }
      }

      _camera = Camera.main;
    }

    protected override void OnDisable() {
      base.OnDisable();
      if (PlayerHudManager.HasInstance) {
        PlayerHudManager.Instance.RemoveComponent("overlay_system");
        if (Globals.Instance) {
          if (ReferenceEquals(Globals.Instance.debugWorldOverlay, this)) { Globals.Instance.debugWorldOverlay = null; }
        }
      }
    }

    private void OnDebugFlagsChanged(ref DebugFlagsChangedEvt args) {
      var globals = Globals.Instance;
      if (!globals.DebugGizmos || !globals.DebugScreenOverlay) { Clear(); }
    }

    private void Clear() {
      _entries.Clear();
      _activeFieldsHelper.Clear();
      _entriesToRemove.Clear();
      _fieldsToRemove.Clear();
      _root.Clear();
    }

    public void Tick() {
      if (!_camera) _camera = Camera.main;
      if (!_camera) return;

      // Raise event to collect updates
      // var evt = new WorldOverlayEvt { api = this, Frustum = _camera.CalculateFrustum6() };
      // Evt.Raise(ref evt);

      var currentTime = Time.time;
      var camPos = _camera.transform.position;
      _entriesToRemove.Clear();

      foreach (var entryKv in _entries) {
        var entry = entryKv.Value;
        var isStale = currentTime - entry.lastSeenTime > 0.1f; // Not updated this frame

        if (currentTime - entry.lastSeenTime > CleanupThreshold) {
          if (entry.element != null) {
            entry.element.RemoveFromHierarchy();
            entry.element = null;
          }

          _entriesToRemove.Add(entryKv.Key);
          continue;
        }

        if (isStale) {
          if (entry.element != null) entry.element.style.display = DisplayStyle.None;
          continue;
        }

        UpdateEntryUI(entry);

        // Sweep fields
        _fieldsToRemove.Clear();
        foreach (var fieldKv in entry.fields) {
          if (currentTime - fieldKv.Value.lastSeenTime > CleanupThreshold) { _fieldsToRemove.Add(fieldKv.Key); }
        }

        foreach (var fId in _fieldsToRemove) entry.fields.Remove(fId);
      }

      foreach (var eId in _entriesToRemove) _entries.Remove(eId);

      if (_sortDepth) {
        _cachedCamPos = camPos;
        _root.hierarchy.Sort(_depthComparison);
      }
    }

    private void UpdateEntryUI(OverlayEntry entry) {
      entry.element ??= new OverlayElement();
      if (entry.element.parent == null) _root.Add(entry.element);

      entry.element.worldPosition = entry.worldPosition;
      var screenPos = _camera.WorldToScreenPoint(entry.worldPosition);

      if (screenPos.z < 0) {
        entry.element.style.display = DisplayStyle.None;
        return;
      }

      entry.element.titleLabel.text = entry.title;
      if (string.IsNullOrWhiteSpace(entry.subtitle)) {
        entry.element.subtitleLabel.style.display = DisplayStyle.None;
      } else {
        entry.element.subtitleLabel.style.display = DisplayStyle.Flex;
        entry.element.subtitleLabel.text = entry.subtitle;
      }

      entry.element.style.display = DisplayStyle.Flex;
      entry.element.style.left = screenPos.x;
      entry.element.style.top = Screen.height - screenPos.y;
      entry.element.style.translate = new Translate(Length.Percent(-50), 0);

      var scale = _globalScale;
      if (_enableDistanceScaling) {
        var dist = Vector3.Distance(_camera.transform.position, entry.worldPosition);
        scale *= Mathf.Clamp(_scalingReferenceDistance / dist, _minScale, _maxScale);
      }

      entry.element.style.scale = new Vector3(scale, scale, 1);

      var currentTime = Time.time;
      _activeFieldsHelper.Clear();
      foreach (var field in entry.fields.Values) {
        if (currentTime - field.lastSeenTime < 0.1f) {
          var element = entry.element.GetOrCreateElement(field.label, field);
          element.SetVisible(true);
          field.Render(element);
          _activeFieldsHelper.Add(field.label);
        }
      }

      entry.element.HideUnusedFields(_activeFieldsHelper);
    }

    public ulong CurrentId { get; set; }

    ulong IWorldOverlayAPI.UpdateEntry(ulong id, string title, Vector3 worldPosition, string subtitle) {
      UpdateEntry(id, title, worldPosition, subtitle);
      return id;
    }

    public void BeginEntry(ulong id, string title, Vector3 worldPosition, string subtitle = null) {
      UpdateEntry(id, title, worldPosition, subtitle);
      CurrentId = id;
    }

    public void BeginEntry(string key, string title, Vector3 worldPosition, string subtitle = null) {
      var id = ((Key)key).value;
      UpdateEntry(id, title, worldPosition, subtitle);
      CurrentId = id;
    }

    public void BeginEntry(Vector3 worldPosition) {
      var gridPos = SpacialHash.ToGrid(worldPosition, 0.5f);
      var id = (ulong)SpacialHash.PackGridPosition(gridPos);
      UpdateEntry(id, "World", worldPosition);
      CurrentId = id;
    }

    public bool ContinueEntry(ulong id) {
      if (_entries.TryGetValue(id, out var entry)) {
        CurrentId = id;
        return true;
      }

      return false;
    }

    public bool ContinueEntry(string key) {
      var id = ((Key)key).value;
      return ContinueEntry(id);
    }

    public void UpdateEntry(ulong id, string title, Vector3 worldPosition, string subtitle = null) {
      if (!_entries.TryGetValue(id, out var entry)) {
        entry = new OverlayEntry { id = id };
        _entries[id] = entry;
      }

      entry.title = title;
      entry.subtitle = subtitle;
      entry.worldPosition = worldPosition;
      entry.lastSeenTime = Time.time;
    }

    public void UpdateField(ulong id, string label, string value, string unit = null, Color? color = null) {
      if (!_entries.TryGetValue(id, out var entry)) return;

      if (!entry.fields.TryGetValue(label, out var field) || field is not OverlayField.String stringField) {
        stringField = new OverlayField.String { label = label };
        entry.fields[label] = stringField;
      }

      stringField.Update(value);
      stringField.unit = unit;
      stringField.color = color;
      stringField.lastSeenTime = Time.time;
    }

    public void UpdateField(
      ulong id,
      string label,
      float value,
      int decimals = 1,
      string unit = null,
      Color? color = null
    ) {
      if (!_entries.TryGetValue(id, out var entry)) return;

      if (!entry.fields.TryGetValue(label, out var field) || field is not OverlayField.Float floatField) {
        floatField = new OverlayField.Float { label = label };
        entry.fields[label] = floatField;
      }

      floatField.Update(value, decimals);
      floatField.unit = unit;
      floatField.color = color;
      floatField.lastSeenTime = Time.time;
    }

    public void UpdateField(ulong id, string label, int value, string unit = null, Color? color = null) {
      if (!_entries.TryGetValue(id, out var entry)) return;

      if (!entry.fields.TryGetValue(label, out var field) || field is not OverlayField.Int intField) {
        intField = new OverlayField.Int { label = label };
        entry.fields[label] = intField;
      }

      intField.Update(value);
      intField.unit = unit;
      intField.color = color;
      intField.lastSeenTime = Time.time;
    }

    public void UpdateField(
      ulong id,
      string label,
      Vector3 value,
      int decimals = 1,
      string unit = null,
      Color? color = null
    ) {
      if (!_entries.TryGetValue(id, out var entry)) return;

      if (!entry.fields.TryGetValue(label, out var field) || field is not OverlayField.Vec3 vectorField) {
        vectorField = new OverlayField.Vec3 { label = label };
        entry.fields[label] = vectorField;
      }

      vectorField.Update(value, decimals);
      vectorField.unit = unit;
      vectorField.color = color;
      vectorField.lastSeenTime = Time.time;
    }

  }
}