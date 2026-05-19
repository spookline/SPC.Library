using System.Collections.Generic;
using HELIX.Widgets;
using Spookline.SPC.Common;
using Spookline.SPC.Debugging;
using Spookline.SPC.Events;
using Spookline.SPC.Ext;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.UI.Overlay {
  public class ScreenOverlayHudComponent : SpookManagerBehaviour<ScreenOverlayHudComponent>, IScreenOverlayAPI {

    private class Section {

      public string title;
      public string subtitle;
      public int order;
      public OverlayElement element;
      public float lastSeenTime;
      public Dictionary<string, OverlayField> fields = new();

    }

    [SerializeField]
    private bool _sortSections = true;

    [Header("Scaling")]
    [SerializeField]
    private float _globalScale = 1.3f;

    private readonly Dictionary<string, Section> _sections = new();
    private readonly HashSet<string> _activeFieldsHelper = new();
    private readonly List<string> _sectionsToRemove = new();
    private readonly List<string> _fieldsToRemove = new();
    private VisualElement _root;
    private string _currentSectionTitle;

    private const float CleanupThreshold = 2.0f;

    protected override void Awake() {
      base.Awake();
      _root = new VisualElement {
        name = "ScreenOverlayRoot",
        style = {
          position = Position.Absolute,
          top = 10,
          left = 10,
          flexDirection = FlexDirection.Column,
          alignItems = Align.FlexStart,
          transformOrigin = new TransformOrigin(0, 0)
        },
        pickingMode = PickingMode.Ignore
      };
      On<DebugFlagsChangedEvt>().Do(OnDebugFlagsChanged);
    }

    private void OnDebugFlagsChanged(ref DebugFlagsChangedEvt args) {
      var globals = Globals.Instance;
      if (!globals.DebugGizmos || !globals.DebugScreenOverlay) {
        Clear();
      }
    }

    protected override void OnEnable() {
      base.OnEnable();
      CachedNumberFormat.Init();
      if (PlayerHudManager.HasInstance) {
        PlayerHudManager.Instance.SetComponent("screen_overlay", _root);

        if (Globals.Instance) {
          Globals.Instance.debugScreenOverlay = this;
        }
      }
    }

    protected override void OnDisable() {
      base.OnDisable();
      if (PlayerHudManager.HasInstance) {
        PlayerHudManager.Instance.RemoveComponent("screen_overlay");

        if (Globals.Instance) {
          if (ReferenceEquals(Globals.Instance.debugScreenOverlay, this)) {
            Globals.Instance.debugScreenOverlay = null;
          }
        }
      }
    }

    private void Clear() {
      _sections.Clear();
      _activeFieldsHelper.Clear();
      _sectionsToRemove.Clear();
      _fieldsToRemove.Clear();
      _root.Clear();
    }

    public void Tick() {
      _currentSectionTitle = "";

      // var evt = new ScreenOverlayEvt { api = this };
      // Evt.Raise(ref evt);

      _root.style.scale = new StyleScale(new Scale(new Vector3(_globalScale, _globalScale, 1)));

      var currentTime = Time.time;
      _sectionsToRemove.Clear();

      foreach (var sectionKv in _sections) {
        var section = sectionKv.Value;
        var isStale = currentTime - section.lastSeenTime > 0.1f;

        if (currentTime - section.lastSeenTime > CleanupThreshold) {
          if (section.element != null) {
            section.element.RemoveFromHierarchy();
            section.element = null;
          }

          _sectionsToRemove.Add(sectionKv.Key);
          continue;
        }

        if (isStale) {
          if (section.element != null) section.element.style.display = DisplayStyle.None;
          continue;
        }

        UpdateSectionUI(section);

        // Sweep fields
        _fieldsToRemove.Clear();
        foreach (var fieldKv in section.fields) {
          if (currentTime - fieldKv.Value.lastSeenTime > CleanupThreshold) { _fieldsToRemove.Add(fieldKv.Key); }
        }

        foreach (var fId in _fieldsToRemove) section.fields.Remove(fId);
      }

      foreach (var sId in _sectionsToRemove) _sections.Remove(sId);

      if (_sortSections) { SortSections(); }
    }

    private void UpdateSectionUI(Section section) {
      if (section.element == null) {
        section.element = new OverlayElement {
          style = {
            position = Position.Relative,
            marginBottom = 4
          }
        };
        _root.Add(section.element);
      }

      if (section.element.parent == null) _root.Add(section.element);

      if (string.IsNullOrWhiteSpace(section.title)) {
        section.element.titleLabel.style.display = DisplayStyle.None;
      } else {
        section.element.titleLabel.style.display = DisplayStyle.Flex;
        section.element.titleLabel.text = section.title;
      }

      if (string.IsNullOrWhiteSpace(section.subtitle)) {
        section.element.subtitleLabel.style.display = DisplayStyle.None;
      } else {
        section.element.subtitleLabel.style.display = DisplayStyle.Flex;
        section.element.subtitleLabel.text = section.subtitle;
      }

      section.element.style.display = DisplayStyle.Flex;

      var currentTime = Time.time;
      _activeFieldsHelper.Clear();
      foreach (var field in section.fields.Values) {
        if (currentTime - field.lastSeenTime < 0.1f) {
          var element = section.element.GetOrCreateElement(field.label, field);
          element.SetVisible(true);
          field.Render(element);
          _activeFieldsHelper.Add(field.label);
        }
      }

      section.element.HideUnusedFields(_activeFieldsHelper);
    }

    private void SortSections() {
      _root.Sort((a, b) => {
          var sectionA = FindSectionByElement(a);
          var sectionB = FindSectionByElement(b);
          if (sectionA == null || sectionB == null) return 0;
          return sectionA.order.CompareTo(sectionB.order);
        }
      );
    }

    private Section FindSectionByElement(VisualElement element) {
      foreach (var section in _sections.Values) {
        if (section.element == element) return section;
      }

      return null;
    }

    public void BeginSection(string title, int order = 0, string subtitle = null) {
      _currentSectionTitle = title;
      if (!_sections.TryGetValue(title, out var section)) {
        section = new Section { title = title };
        _sections[title] = section;
      }

      section.subtitle = subtitle;
      section.order = order;
      section.lastSeenTime = Time.time;
    }

    public void GlobalSection() {
      _currentSectionTitle = "";
    }

    private Section GetCurrentSection() {
      if (!_sections.TryGetValue(_currentSectionTitle, out var section)) {
        section = new Section { title = _currentSectionTitle };
        _sections[_currentSectionTitle] = section;
      }

      section.lastSeenTime = Time.time;
      return section;
    }

    public void UpdateField(string label, string value, string unit = null, Color? color = null) {
      var section = GetCurrentSection();
      if (!section.fields.TryGetValue(label, out var field) || field is not OverlayField.String stringField) {
        stringField = new OverlayField.String { label = label };
        section.fields[label] = stringField;
      }

      stringField.Update(value);
      stringField.unit = unit;
      stringField.color = color;
      stringField.lastSeenTime = Time.time;
    }

    public void UpdateField(string label, float value, int decimals = 1, string unit = null, Color? color = null) {
      var section = GetCurrentSection();
      if (!section.fields.TryGetValue(label, out var field) || field is not OverlayField.Float floatField) {
        floatField = new OverlayField.Float { label = label };
        section.fields[label] = floatField;
      }

      floatField.Update(value, decimals);
      floatField.unit = unit;
      floatField.color = color;
      floatField.lastSeenTime = Time.time;
    }

    public void UpdateField(string label, int value, string unit = null, Color? color = null) {
      var section = GetCurrentSection();
      if (!section.fields.TryGetValue(label, out var field) || field is not OverlayField.Int intField) {
        intField = new OverlayField.Int { label = label };
        section.fields[label] = intField;
      }

      intField.Update(value);
      intField.unit = unit;
      intField.color = color;
      intField.lastSeenTime = Time.time;
    }

    public void UpdateField(string label, Vector3 value, int decimals = 1, string unit = null, Color? color = null) {
      var section = GetCurrentSection();
      if (!section.fields.TryGetValue(label, out var field) || field is not OverlayField.Vec3 vectorField) {
        vectorField = new OverlayField.Vec3 { label = label };
        section.fields[label] = vectorField;
      }

      vectorField.Update(value, decimals);
      vectorField.unit = unit;
      vectorField.color = color;
      vectorField.lastSeenTime = Time.time;
    }

    public void UpdateField<T, E>(
        string label,
        T value,
        IOverlayFieldFactory<T, E> factory) where E : IFieldElement {
      var section = GetCurrentSection();
      if (!section.fields.TryGetValue(label, out var field) || field is not OverlayField.Custom<T,E> customField) {
        customField = new OverlayField.Custom<T,E>(factory) { label = label };
        section.fields[label] = customField;
      }

      customField.Update(value);
      customField.lastSeenTime = Time.time;
    }
  }
}