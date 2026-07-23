using System.Collections.Generic;
using System.Linq;
using Spookline.SPC.Audio;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.Editor {
    public sealed class AudioDefinitionWindow : EditorWindow {
        private readonly List<AudioDefinition> _definitions = new();
        private readonly List<object> _listItems = new();
        private readonly HashSet<string> _collapsedGroups = new();
        private ListView _list;
        private TextField _search;
        private VisualElement _details;
        private AudioDefinition _selected;
        private SerializedObject _serializedObject;
        private RangeAudioSourceProvider _clipPickerProvider;
        private bool _refreshScheduled;

        [MenuItem("Window/Spookline/Audio Definitions")]
        public static void ShowWindow() {
            var window = GetWindow<AudioDefinitionWindow>();
            window.titleContent = new GUIContent("Audio Definitions");
            window.minSize = new Vector2(720, 420);
        }

        private void CreateGUI() {
            rootVisualElement.Clear();
            BuildToolbar();

            var content = new TwoPaneSplitView(0, 260, TwoPaneSplitViewOrientation.Horizontal);
            _list = new ListView {
                selectionType = SelectionType.Single,
                makeItem = MakeListItem,
                bindItem = BindListItem,
                itemsSource = _listItems,
                fixedItemHeight = 24
            };
            _list.onSelectionChange += OnSelectionChanged;
            content.Add(_list);

            _details = new ScrollView { name = "details" };
            _details.style.paddingLeft = 12;
            _details.style.paddingRight = 12;
            content.Add(_details);
            rootVisualElement.Add(content);
            Refresh();
        }

        private void OnGUI() {
            if (Event.current.type != EventType.ExecuteCommand || Event.current.commandName != "ObjectSelectorUpdated") return;
            if (_clipPickerProvider == null) return;
            var clip = EditorGUIUtility.GetObjectPickerObject() as AudioClip;
            if (clip == null) return;
            Undo.RecordObject(_selected, "Add Audio Clip");
            (_clipPickerProvider.clips ??= new List<AudioClip>()).Add(clip);
            EditorUtility.SetDirty(_selected);
            AssetDatabase.SaveAssetIfDirty(_selected);
            _clipPickerProvider = null;
            DrawDetails();
        }

        private void BuildToolbar() {
            var toolbar = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingBottom = 4 } };
            _search = new TextField { name = "search" };
            _search.label = "Search";
            _search.style.flexGrow = 1;
            _search.RegisterValueChangedCallback(_ => Refresh());
            toolbar.Add(_search);
            toolbar.Add(new Button(Refresh) { text = "Refresh" });
            toolbar.Add(new Button(CreateDefinition) { text = "New Definition" });
            rootVisualElement.Add(toolbar);
        }

        private VisualElement MakeListItem() {
            var row = new VisualElement { name = "row" };
            row.style.flexDirection = FlexDirection.Row;
            var label = new Label { name = "name", style = { paddingLeft = 8, paddingTop = 3 } };
            label.style.flexGrow = 1;
            label.RegisterCallback<MouseEnterEvent>(_ => {
                if (label.userData is MixerGroupHeader) return;
                label.style.backgroundColor = new Color(0.24f, 0.24f, 0.24f, 1f);
            });
            label.RegisterCallback<MouseLeaveEvent>(_ => ApplyListItemBackground(label));
            var collapseButton = new Button {
                name = "collapse",
                text = ""
            };
            collapseButton.style.width = 32;
            collapseButton.style.height = 20;
            collapseButton.clicked += () => {
                if (!(collapseButton.userData is MixerGroupHeader header)) return;
                if (header.IsCollapsed) _collapsedGroups.Remove(header.Key);
                else _collapsedGroups.Add(header.Key);
                Refresh();
            };
            row.Add(label);
            row.Add(collapseButton);
            return row;
        }

        private void ApplyListItemBackground(Label label) {
            if (label.userData is MixerGroupHeader) {
                label.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);
                return;
            }

            label.style.backgroundColor = label.userData is AudioDefinition definition &&
                                          _list != null && _list.selectedIndex >= 0 &&
                                          _listItems[_list.selectedIndex] == definition
                ? new Color(0.17f, 0.36f, 0.6f, 1f)
                : Color.clear;
        }

        private void BindListItem(VisualElement element, int index) {
            var label = element.Q<Label>("name");
            var collapseButton = element.Q<Button>("collapse");
            if (_listItems[index] is MixerGroupHeader header) {
                label.userData = header;
                label.text = header.Name;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.paddingTop = 4;
                collapseButton.userData = header;
                collapseButton.text = header.IsCollapsed ? "▶" : "▼";
                collapseButton.style.display = DisplayStyle.Flex;
            } else {
                var definition = (AudioDefinition)_listItems[index];
                label.userData = definition;
                label.text = definition.name;
                label.style.unityFontStyleAndWeight = FontStyle.Normal;
                label.style.paddingTop = 3;
                collapseButton.userData = null;
                collapseButton.text = "";
                collapseButton.style.display = DisplayStyle.None;
            }
            ApplyListItemBackground(label);
        }

        private void OnSelectionChanged(IEnumerable<object> selection) {
            ApplyDetailsChanges();
            var definition = selection.OfType<AudioDefinition>().FirstOrDefault();
            if (definition == null) return;
            _selected = definition;
            DrawDetails();
            _list.RefreshItems();
        }

        private void Refresh() {
            var query = _search?.value ?? string.Empty;
            var selectedGuid = _selected ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_selected)) : null;
            _definitions.Clear();
            _definitions.AddRange(AssetDatabase.FindAssets("t:AudioDefinition")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<AudioDefinition>)
                .Where(definition => definition && (string.IsNullOrWhiteSpace(query) || definition.name.ToLowerInvariant().Contains(query.ToLowerInvariant())))
                .OrderBy(definition => definition.name));
            _listItems.Clear();
            foreach (var group in _definitions.GroupBy(GetMixerGroupKey).OrderBy(group => group.Key)) {
                var collapsed = _collapsedGroups.Contains(group.Key);
                _listItems.Add(new MixerGroupHeader(group.Key, collapsed));
                if (!collapsed) _listItems.AddRange(group);
            }
            _list?.RefreshItems();
            var index = selectedGuid == null ? -1 : _listItems.FindIndex(item => item is AudioDefinition definition && AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition)) == selectedGuid);
            if (index >= 0) _list.SetSelection(index);
            else if (_listItems.OfType<AudioDefinition>().Any()) _list.SetSelection(_listItems.FindIndex(item => item is AudioDefinition));
            else {
                ApplyDetailsChanges();
                _selected = null;
                DrawDetails();
            }
        }

        private static string GetMixerGroupKey(AudioDefinition definition) {
            return definition.group ? definition.group.name : "No Audio Mixer Group";
        }

        private sealed class MixerGroupHeader {
            public MixerGroupHeader(string key, bool isCollapsed) {
                Key = key;
                Name = $"Audio Mixer Group: {key}";
                IsCollapsed = isCollapsed;
            }

            public string Key { get; }
            public string Name { get; }
            public bool IsCollapsed { get; }
        }

        private void DrawDetails() {
            ApplyDetailsChanges();
            _details.Clear();
            _serializedObject = null;
            if (!_selected) {
                _details.Add(new HelpBox("Select an audio definition to edit it.", HelpBoxMessageType.Info));
                return;
            }

            var nameField = new TextField("Definition Name") {
                value = _selected.name
            };
            nameField.style.marginBottom = 8;
            nameField.RegisterCallback<KeyDownEvent>(eventData => {
                if (eventData.keyCode != KeyCode.Return && eventData.keyCode != KeyCode.KeypadEnter) return;
                RenameDefinition(nameField.value);
                eventData.StopPropagation();
            });
            nameField.RegisterCallback<FocusOutEvent>(_ => RenameDefinition(nameField.value));
            _details.Add(nameField);
            _serializedObject = new SerializedObject(_selected);
            _serializedObject.Update();
            var property = _serializedObject.GetIterator();
            var enterChildren = true;
            while (property.NextVisible(enterChildren)) {
                if (property.propertyPath == "m_Script") {
                    enterChildren = false;
                    continue;
                }
                _details.Add(new PropertyField(property.Copy()));
                enterChildren = false;
            }
            AddProviderEditor();
            _details.Bind(_serializedObject);
            _details.RegisterCallback<SerializedPropertyChangeEvent>(OnSerializedPropertyChanged);

            var actions = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 12 } };
            actions.Add(new Button(() => Selection.activeObject = _selected) { text = "Select Asset" });
            actions.Add(new Button(() => EditorGUIUtility.PingObject(_selected)) { text = "Ping" });
            actions.Add(new Button(() => DuplicateDefinition(_selected)) { text = "Duplicate" });
            actions.Add(new Button(() => DeleteDefinition(_selected)) { text = "Delete" });
            _details.Add(actions);
        }

        private void OnSerializedPropertyChanged(SerializedPropertyChangeEvent eventData) {
            if (eventData.changedProperty.propertyPath == "group") ScheduleRefresh();
        }

        private void RenameDefinition(string requestedName) {
            if (!_selected) return;
            var newName = requestedName?.Trim();
            if (string.IsNullOrEmpty(newName) || newName == _selected.name) return;

            var path = AssetDatabase.GetAssetPath(_selected);
            var error = AssetDatabase.RenameAsset(path, newName);
            if (!string.IsNullOrEmpty(error)) {
                EditorUtility.DisplayDialog("Rename Audio Definition", error, "OK");
                DrawDetails();
                return;
            }

            AssetDatabase.SaveAssets();
            Refresh();
        }

        private void ApplyDetailsChanges() {
            if (_serializedObject == null) return;
            if (_serializedObject.ApplyModifiedProperties()) {
                EditorUtility.SetDirty(_selected);
                AssetDatabase.SaveAssetIfDirty(_selected);
            }
            _serializedObject.Dispose();
            _serializedObject = null;
        }

        private void ScheduleRefresh() {
            if (_refreshScheduled) return;
            _refreshScheduled = true;
            EditorApplication.delayCall += RefreshAfterPropertyChange;
        }

        private void RefreshAfterPropertyChange() {
            EditorApplication.delayCall -= RefreshAfterPropertyChange;
            _refreshScheduled = false;
            if (this == null || !_selected) return;
            Refresh();
        }

        private void AddProviderEditor() {
            AddProviderSelector();
            if (_selected.provider == null) {
                _details.Add(new HelpBox("No audio source provider is assigned.", HelpBoxMessageType.Warning));
                return;
            }

            _details.Add(new Label($"Provider: {_selected.provider.GetType().Name}") {
                style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 8 }
            });

            if (_selected.provider is RangeAudioSourceProvider rangeProvider)
                AddRangeProviderEditor(rangeProvider);
        }

        private void AddProviderSelector() {
            var providerTypes = TypeCache.GetTypesDerivedFrom<IAudioSourceProvider>()
                .Where(type => type.IsClass && !type.IsAbstract && !type.IsGenericType && type.GetConstructor(System.Type.EmptyTypes) != null)
                .OrderBy(type => type.Name)
                .ToList();
            var options = new List<string> { "None" };
            options.AddRange(providerTypes.Select(type => type.FullName));
            var currentType = _selected.provider?.GetType();
            var currentIndex = currentType == null ? 0 : providerTypes.FindIndex(type => type == currentType) + 1;
            if (currentIndex < 0) currentIndex = 0;

            var selector = new PopupField<string>("Audio Source Provider", options, currentIndex);
            selector.RegisterValueChangedCallback(change => {
                var newType = change.newValue == "None"
                    ? null
                    : providerTypes.FirstOrDefault(type => type.FullName == change.newValue);
                if (newType == currentType) return;
                Undo.RecordObject(_selected, "Change Audio Source Provider");
                _selected.provider = newType == null
                    ? null
                    : (IAudioSourceProvider)System.Activator.CreateInstance(newType);
                EditorUtility.SetDirty(_selected);
                AssetDatabase.SaveAssetIfDirty(_selected);
                DrawDetails();
            });
            _details.Add(selector);
        }

        private void AddRangeProviderEditor(RangeAudioSourceProvider provider) {
            var container = new VisualElement { name = "range-provider" };
            container.style.marginTop = 4;
            container.style.paddingTop = 8;
            container.style.paddingBottom = 8;
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;
            container.style.borderTopWidth = 1;
            container.style.borderBottomWidth = 1;
            container.style.borderLeftWidth = 1;
            container.style.borderRightWidth = 1;

            var clipsLabel = new Label($"Clips ({provider.clips?.Count ?? 0})") {
                style = { unityFontStyleAndWeight = FontStyle.Bold }
            };
            container.Add(clipsLabel);
            var addClipButton = new Button(() => {
                _clipPickerProvider = provider;
                EditorGUIUtility.ShowObjectPicker<AudioClip>(null, false, string.Empty, 0);
            }) { text = "Add Clip..." };
            addClipButton.style.marginBottom = 4;
            container.Add(addClipButton);
            var clips = provider.clips ?? (provider.clips = new List<AudioClip>());
            for (var i = 0; i < clips.Count; i++) {
                var index = i;
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                var field = new ObjectField($"Clip {i + 1}") {
                    objectType = typeof(AudioClip),
                    allowSceneObjects = false,
                    value = clips[i]
                };
                field.style.flexGrow = 1;
                field.RegisterValueChangedCallback(change => {
                    Undo.RecordObject(_selected, "Edit Audio Clip");
                    clips[index] = change.newValue as AudioClip;
                    EditorUtility.SetDirty(_selected);
                    AssetDatabase.SaveAssetIfDirty(_selected);
                });
                row.Add(field);
                var removeButton = new Button(() => {
                    if (index < 0 || index >= clips.Count) return;
                    Undo.RecordObject(_selected, "Remove Audio Clip");
                    clips.RemoveAt(index);
                    EditorUtility.SetDirty(_selected);
                    AssetDatabase.SaveAssetIfDirty(_selected);
                    DrawDetails();
                }) { text = "Remove" };
                removeButton.style.marginTop = 2;
                row.Add(removeButton);
                container.Add(row);
            }

            var repeatToggle = new Toggle("Avoid consecutive clip repeats") {
                value = provider.avoidConsecutiveClipRepeats
            };
            repeatToggle.RegisterValueChangedCallback(change => {
                Undo.RecordObject(_selected, "Edit Audio Provider");
                provider.avoidConsecutiveClipRepeats = change.newValue;
                EditorUtility.SetDirty(_selected);
                AssetDatabase.SaveAssetIfDirty(_selected);
            });
            container.Add(repeatToggle);
            AddClipDropArea(container, provider);
            _details.Add(container);
        }

        private void AddClipDropArea(VisualElement parent, RangeAudioSourceProvider provider) {
            var container = new VisualElement();
            container.style.marginTop = 8;
            container.style.paddingTop = 8;
            container.style.paddingBottom = 8;
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;
            container.style.borderTopWidth = 1;
            container.style.borderBottomWidth = 1;
            container.style.borderLeftWidth = 1;
            container.style.borderRightWidth = 1;

            var label = new Label("Range clips (drag one or more AudioClips here)");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            container.Add(label);
            var hint = new HelpBox("Dragging clips appends them to the provider. Existing clips are preserved.", HelpBoxMessageType.Info);
            container.Add(hint);
            container.RegisterCallback<DragUpdatedEvent>(eventData => {
                eventData.StopPropagation();
                DragAndDrop.visualMode = GetDraggedClips().Length > 0
                    ? DragAndDropVisualMode.Copy
                    : DragAndDropVisualMode.Rejected;
            });
            container.RegisterCallback<DragPerformEvent>(eventData => {
                eventData.StopPropagation();
                var draggedClips = GetDraggedClips();
                if (draggedClips.Length == 0) return;
                DragAndDrop.AcceptDrag();
                Undo.RecordObject(_selected, "Add Audio Clips");
                Undo.RecordObject(_selected, "Add Audio Clips");
                if (provider.clips == null) provider.clips = new List<AudioClip>();
                provider.clips.AddRange(draggedClips);
                EditorUtility.SetDirty(_selected);
                AssetDatabase.SaveAssetIfDirty(_selected);
                DrawDetails();
            });
            parent.Add(container);
        }

        private static AudioClip[] GetDraggedClips() {
            return DragAndDrop.objectReferences.OfType<AudioClip>().Where(clip => clip).ToArray();
        }

        private static void CreateDefinition() {
            var folder = "Assets";
            var selectedPath = Selection.activeObject ? AssetDatabase.GetAssetPath(Selection.activeObject) : string.Empty;
            if (!string.IsNullOrEmpty(selectedPath)) {
                if (AssetDatabase.IsValidFolder(selectedPath)) folder = selectedPath;
                else folder = System.IO.Path.GetDirectoryName(selectedPath)?.Replace('\\', '/') ?? folder;
            }
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/AudioDefinition.asset");
            var definition = CreateInstance<AudioDefinition>();
            AssetDatabase.CreateAsset(definition, path);
            MarkAsAddressable(definition);
            AssetDatabase.SaveAssets();
            Selection.activeObject = definition;
            GetWindow<AudioDefinitionWindow>().Refresh();
        }

        private void DuplicateDefinition(AudioDefinition definition) {
            var path = AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(definition));
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(definition), path);
            var duplicate = AssetDatabase.LoadAssetAtPath<AudioDefinition>(path);
            MarkAsAddressable(duplicate);
            AssetDatabase.SaveAssets();
            Refresh();
        }

        private static void MarkAsAddressable(AudioDefinition definition) {
            if (!definition) return;

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) {
                Debug.LogWarning("Could not mark audio definition as Addressable because Addressable Asset Settings are not configured.");
                return;
            }

            settings.AddLabel("audio", false);
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition));
            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.SetLabel("audio", true, true);
            EditorUtility.SetDirty(settings);
        }

        private void DeleteDefinition(AudioDefinition definition) {
            var path = AssetDatabase.GetAssetPath(definition);
            if (!EditorUtility.DisplayDialog("Delete Audio Definition", $"Delete '{definition.name}'?", "Delete", "Cancel")) return;
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            Refresh();
        }
    }
}