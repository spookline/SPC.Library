using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Spookline.SPC.Audio;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace Spookline.SPC.Editor {
    /// <summary>
    /// UI Toolkit library for creating and maintaining addressable audio definitions.
    /// </summary>
    public sealed class AudioDefinitionWindow : EditorWindow {

        private const string StylePath = "Assets/Library/SPC/Editor/AudioDefinitionWindow.uss";

        private readonly List<AudioDefinition> _definitions = new();
        private readonly Dictionary<string, int> _definitionItemIds = new();
        private readonly Dictionary<string, int> _definitionParentIds = new();
        private readonly HashSet<int> _usedTreeIds = new();

        private TreeView _tree;
        private VisualElement _details;
        private Label _librarySummary;
        private ToolbarSearchField _search;
        private AudioDefinition _selectedDefinition;
        private AudioMixerGroup _selectedMixerGroup;
        private SerializedObject _serializedDefinition;
        private string _requestedSelectionGuid;
        private bool _refreshQueued;
        private bool _suppressTreeSelection;
        private int _treeBuildVersion;

        private sealed class AudioTreeItem {
            public string name;
            public string guid;
            public AudioMixerGroup mixerGroup;
            public AudioDefinition definition;
            public int childCount;
            public bool addressable;

            public bool IsGroup => !definition;
        }

        [MenuItem("Window/Spookline/Audio Library")]
        public static void Open() {
            var window = GetWindow<AudioDefinitionWindow>();
            window.titleContent = new GUIContent("Audio Library");
            window.minSize = new Vector2(860f, 540f);
            window.Show();
        }

        [MenuItem("CONTEXT/AudioDefinition/Open in Audio Library")]
        private static void OpenDefinitionFromContext(MenuCommand command) {
            if (command.context is AudioDefinition definition)
                OpenDefinition(definition);
        }

        public static void OpenDefinition(AudioDefinition definition) {
            if (!definition) return;

            var window = GetWindow<AudioDefinitionWindow>();
            window.titleContent = new GUIContent("Audio Library");
            window.minSize = new Vector2(860f, 540f);
            window._selectedDefinition = definition;
            window._selectedMixerGroup = definition.group;
            window._requestedSelectionGuid =
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition));
            window.Show();
            window.Focus();

            if (window.rootVisualElement.childCount > 0) {
                window._search?.SetValueWithoutNotify(string.Empty);
                window.RefreshDefinitions();
            }
        }

        private void OnEnable() {
            EditorApplication.projectChanged += QueueRefresh;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable() {
            EditorApplication.projectChanged -= QueueRefresh;
            Undo.undoRedoPerformed -= OnUndoRedo;
            ApplyPendingChanges();
        }

        private void OnFocus() {
            if (rootVisualElement.childCount > 0) QueueRefresh();
        }

        public void CreateGUI() {
            rootVisualElement.Clear();
            rootVisualElement.AddToClassList("audio-library");

            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (style) rootVisualElement.styleSheets.Add(style);

            BuildToolbar();
            BuildWorkspace();
            RefreshDefinitions();
        }

        private void BuildToolbar() {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("audio-toolbar");

            var brand = new Label("♫");
            brand.AddToClassList("brand-mark");
            toolbar.Add(brand);

            var titleBlock = new VisualElement();
            titleBlock.AddToClassList("title-block");
            var title = new Label("Audio Library");
            title.AddToClassList("window-title");
            titleBlock.Add(title);
            _librarySummary = new Label("Discovering audio definitions…");
            _librarySummary.AddToClassList("window-subtitle");
            titleBlock.Add(_librarySummary);
            toolbar.Add(titleBlock);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            toolbar.Add(spacer);

            _search = new ToolbarSearchField();
            _search.AddToClassList("audio-search");
            _search.tooltip = "Search definitions, mixer groups, or clip names";
            _search.RegisterValueChangedCallback(_ => RefreshTree());
            toolbar.Add(_search);

            var repairButton = new Button(EnsureAllAddressableLabels) { text = "Label All" };
            repairButton.tooltip = $"Assign the '{SpookAudioRegistry.DefaultAddressableLabel}' label to every definition";
            repairButton.AddToClassList("secondary-button");
            toolbar.Add(repairButton);

            var refreshButton = new Button(RefreshDefinitions) { text = "↻" };
            refreshButton.tooltip = "Refresh the audio library";
            refreshButton.AddToClassList("square-button");
            toolbar.Add(refreshButton);

            var createButton = new Button(() => CreateDefinition(_selectedMixerGroup)) { text = "+ New Definition" };
            createButton.AddToClassList("primary-button");
            toolbar.Add(createButton);

            rootVisualElement.Add(toolbar);
        }

        private void BuildWorkspace() {
            var split = new TwoPaneSplitView(0, 310f, TwoPaneSplitViewOrientation.Horizontal);
            split.AddToClassList("workspace");

            var sidebar = new VisualElement();
            sidebar.AddToClassList("sidebar");

            var sidebarHeader = new VisualElement();
            sidebarHeader.AddToClassList("section-heading");
            var headingText = new VisualElement();
            headingText.style.flexGrow = 1f;
            var heading = new Label("MIXER HIERARCHY");
            heading.AddToClassList("eyebrow");
            headingText.Add(heading);
            var hint = new Label("Definitions grouped by output");
            hint.AddToClassList("section-hint");
            headingText.Add(hint);
            sidebarHeader.Add(headingText);
            sidebar.Add(sidebarHeader);

            _tree = new TreeView {
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                fixedItemHeight = 24f
            };
            _tree.AddToClassList("audio-tree");
            _tree.makeItem = MakeTreeRow;
            _tree.bindItem = BindTreeRow;
            _tree.selectionChanged += OnTreeSelectionChanged;
            sidebar.Add(_tree);

            _details = new ScrollView(ScrollViewMode.Vertical);
            _details.AddToClassList("details-scroll");

            split.Add(sidebar);
            split.Add(_details);
            rootVisualElement.Add(split);
        }

        private VisualElement MakeTreeRow() {
            var row = new VisualElement();
            row.AddToClassList("tree-row");

            var icon = new Image { name = "icon", scaleMode = ScaleMode.ScaleToFit };
            icon.AddToClassList("tree-icon");
            row.Add(icon);

            var name = new Label { name = "name" };
            name.AddToClassList("tree-name");
            row.Add(name);

            var badge = new Label { name = "badge" };
            badge.AddToClassList("tree-badge");
            row.Add(badge);

            row.AddManipulator(new ContextualMenuManipulator(evt => {
                if (row.userData is not AudioTreeItem item) return;

                if (item.IsGroup) {
                    evt.menu.AppendAction("Create Definition Here", _ => CreateDefinition(item.mixerGroup));
                    if (item.mixerGroup)
                        evt.menu.AppendAction("Ping Mixer Group", _ => EditorGUIUtility.PingObject(item.mixerGroup));
                    return;
                }

                evt.menu.AppendAction("Ping Asset", _ => EditorGUIUtility.PingObject(item.definition));
                evt.menu.AppendAction("Duplicate", _ => DuplicateDefinition(item.definition));
                if (!item.addressable)
                    evt.menu.AppendAction("Assign Addressable Label", _ => EnsureAddressableLabel(item.definition, true));
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Delete", _ => DeleteDefinition(item.definition));
            }));

            row.RegisterCallback<PointerDownEvent>(evt => {
                if (evt.clickCount != 2 || row.userData is not AudioTreeItem item) return;
                if (item.definition) {
                    Selection.activeObject = item.definition;
                    EditorGUIUtility.PingObject(item.definition);
                }
            });

            return row;
        }

        private void BindTreeRow(VisualElement row, int index) {
            var item = _tree.GetItemDataForIndex<AudioTreeItem>(index);
            row.userData = item;
            row.EnableInClassList("group-row", item.IsGroup);
            row.EnableInClassList("definition-row", !item.IsGroup);

            var icon = row.Q<Image>("icon");
            var name = row.Q<Label>("name");
            var badge = row.Q<Label>("badge");

            name.text = item.name;
            name.tooltip = item.name;

            if (item.IsGroup) {
                icon.image = EditorGUIUtility.IconContent("AudioMixerGroup Icon").image;
                badge.text = item.childCount.ToString();
                badge.tooltip = $"{item.childCount} definition{(item.childCount == 1 ? string.Empty : "s")}";
                badge.EnableInClassList("missing-label", false);
            } else {
                icon.image = EditorGUIUtility.IconContent("AudioClip Icon").image;
                badge.text = item.addressable ? "A" : "!";
                badge.tooltip = item.addressable
                    ? $"Addressable label: {SpookAudioRegistry.DefaultAddressableLabel}"
                    : $"Missing addressable label '{SpookAudioRegistry.DefaultAddressableLabel}'";
                badge.EnableInClassList("missing-label", !item.addressable);
            }
        }

        private void OnTreeSelectionChanged(IEnumerable<object> selected) {
            if (_suppressTreeSelection) return;
            ApplyPendingChanges();

            var item = selected.OfType<AudioTreeItem>().FirstOrDefault();
            _selectedDefinition = item?.definition;
            _selectedMixerGroup = item?.mixerGroup;

            if (_selectedDefinition) DrawDefinitionDetails(_selectedDefinition);
            else if (item?.IsGroup == true) DrawMixerGroupDetails(item);
            else DrawEmptyState();
        }

        private void RefreshDefinitions() {
            var selectedGuid = !string.IsNullOrEmpty(_requestedSelectionGuid)
                ? _requestedSelectionGuid
                : _selectedDefinition
                    ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_selectedDefinition))
                    : string.Empty;

            _definitions.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioDefinition")) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<AudioDefinition>(path);
                if (definition) _definitions.Add(definition);
            }

            _definitions.Sort((left, right) =>
                string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));

            var labeledCount = _definitions.Count(IsAddressable);
            _librarySummary.text =
                $"{_definitions.Count} definition{(_definitions.Count == 1 ? string.Empty : "s")}  •  " +
                $"{labeledCount} addressable";

            RefreshTree(selectedGuid);
        }

        private void RefreshTree(string selectionGuid = null) {
            if (_tree == null) return;

            if (string.IsNullOrEmpty(selectionGuid)) {
                if (!string.IsNullOrEmpty(_requestedSelectionGuid))
                    selectionGuid = _requestedSelectionGuid;
                else if (_selectedDefinition)
                    selectionGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_selectedDefinition));
            }

            var query = _search?.value?.Trim() ?? string.Empty;
            var visibleDefinitions = _definitions.Where(definition => MatchesSearch(definition, query)).ToList();
            var targetDefinition = !string.IsNullOrEmpty(selectionGuid)
                ? _definitions.FirstOrDefault(definition =>
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition)) == selectionGuid)
                : null;

            _definitionItemIds.Clear();
            _definitionParentIds.Clear();
            _usedTreeIds.Clear();
            var roots = new List<TreeViewItemData<AudioTreeItem>>();
            var buildVersion = ++_treeBuildVersion;
            _suppressTreeSelection = true;

            foreach (var grouping in visibleDefinitions
                         .GroupBy(definition => definition.group)
                         .OrderBy(group => GetMixerGroupName(group.Key), StringComparer.OrdinalIgnoreCase)) {
                var group = grouping.Key;
                var children = new List<TreeViewItemData<AudioTreeItem>>();
                var groupPath = group ? AssetDatabase.GetAssetPath(group) : "unassigned";
                var groupId = UniqueTreeId($"group:{groupPath}:{GetMixerGroupName(group)}");

                foreach (var definition in grouping.OrderBy(item => item.name, StringComparer.OrdinalIgnoreCase)) {
                    var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition));
                    var id = UniqueTreeId($"definition:{guid}");
                    _definitionItemIds[guid] = id;
                    _definitionParentIds[guid] = groupId;
                    children.Add(new TreeViewItemData<AudioTreeItem>(id, new AudioTreeItem {
                        name = definition.name,
                        guid = guid,
                        mixerGroup = group,
                        definition = definition,
                        addressable = IsAddressable(definition)
                    }));
                }

                roots.Add(new TreeViewItemData<AudioTreeItem>(groupId, new AudioTreeItem {
                    name = GetMixerGroupName(group),
                    mixerGroup = group,
                    childCount = children.Count
                }, children));
            }

            _tree.SetRootItems(roots);
            _tree.Rebuild();
            if (!string.IsNullOrEmpty(query)) _tree.ExpandAll();

            if (!string.IsNullOrEmpty(selectionGuid) &&
                _definitionItemIds.TryGetValue(selectionGuid, out var selectionId)) {
                _definitionParentIds.TryGetValue(selectionGuid, out var parentId);

                _selectedDefinition = targetDefinition;
                _selectedMixerGroup = targetDefinition ? targetDefinition.group : null;
                _tree.SetSelectionByIdWithoutNotify(new[] { -1 });
                if (_selectedDefinition) DrawDefinitionDetails(_selectedDefinition);

                SelectTreeItemAfterRebuild(selectionGuid, selectionId, parentId, buildVersion);
            } else if (_selectedMixerGroup) {
                _suppressTreeSelection = false;
                DrawMixerGroupDetails(new AudioTreeItem {
                    name = GetMixerGroupName(_selectedMixerGroup),
                    mixerGroup = _selectedMixerGroup,
                    childCount = _definitions.Count(item => item.group == _selectedMixerGroup)
                });
            } else {
                _suppressTreeSelection = false;
                DrawEmptyState();
            }
        }

        private void SelectTreeItemAfterRebuild(
            string targetGuid,
            int selectionId,
            int parentId,
            int buildVersion,
            int attempt = 0
        ) {
            _tree.schedule.Execute(() => {
                if (!this || _tree == null || buildVersion != _treeBuildVersion) return;

                _suppressTreeSelection = true;
                _tree.SetSelectionByIdWithoutNotify(new[] { -1 });
                _tree.SetSelectionById(selectionId);
                if (parentId != 0) _tree.ExpandItem(parentId);

                // Expanding changes the flattened row indices, so select once more against
                // the materialized tree before focusing and framing the item.
                _tree.RefreshItems();
                var selectionIndex = _tree.viewController.GetIndexForId(selectionId);
                if (selectionIndex >= 0) {
                    _tree.SetSelectionWithoutNotify(Array.Empty<int>());
                    _tree.SetSelection(selectionIndex);
                } else {
                    _tree.SetSelectionByIdWithoutNotify(new[] { -1 });
                    _tree.SetSelectionById(selectionId);
                }
                _tree.Focus();
                _tree.ScrollToItemById(selectionId);

                var selectedItem = _tree.selectedItem as AudioTreeItem;
                if (selectedItem?.guid != targetGuid && attempt < 5) {
                    SelectTreeItemAfterRebuild(targetGuid, selectionId, parentId, buildVersion, attempt + 1);
                    return;
                }

                _suppressTreeSelection = false;
                if (_requestedSelectionGuid == targetGuid)
                    _requestedSelectionGuid = null;
            }).StartingIn(attempt == 0 ? 1 : 25);
        }

        private void DrawEmptyState() {
            ReleaseSerializedObject();
            _details.Clear();

            var empty = new VisualElement();
            empty.AddToClassList("empty-state");
            var icon = new Label("♫");
            icon.AddToClassList("empty-icon");
            empty.Add(icon);
            var title = new Label(_definitions.Count == 0 ? "Build your audio library" : "Select a definition");
            title.AddToClassList("empty-title");
            empty.Add(title);
            var message = new Label(_definitions.Count == 0
                ? "Create an Audio Definition, then drop one or many clips into it."
                : "Choose a sound from the mixer hierarchy to edit its clips and playback settings.");
            message.AddToClassList("empty-message");
            empty.Add(message);
            var create = new Button(() => CreateDefinition(null)) { text = "+ Create Audio Definition" };
            create.AddToClassList("primary-button");
            empty.Add(create);
            _details.Add(empty);
        }

        private void DrawMixerGroupDetails(AudioTreeItem item) {
            ReleaseSerializedObject();
            _details.Clear();

            var hero = CreateHero(
                item.name,
                $"{item.childCount} audio definition{(item.childCount == 1 ? string.Empty : "s")}",
                "AudioMixerGroup Icon"
            );
            var actions = hero.Q<VisualElement>("actions");
            var create = new Button(() => CreateDefinition(item.mixerGroup)) { text = "+ New in Group" };
            create.AddToClassList("primary-button");
            actions.Add(create);
            if (item.mixerGroup) {
                var ping = new Button(() => EditorGUIUtility.PingObject(item.mixerGroup)) { text = "Ping Mixer" };
                ping.AddToClassList("secondary-button");
                actions.Add(ping);
            }
            _details.Add(hero);

            var card = CreateCard("Mixer Group", "Definitions below inherit this output routing.");
            if (item.mixerGroup) {
                var field = new ObjectField("Mixer Group") {
                    objectType = typeof(AudioMixerGroup),
                    value = item.mixerGroup
                };
                field.SetEnabled(false);
                card.Add(field);
            } else {
                card.Add(new HelpBox(
                    "These definitions do not currently route to an Audio Mixer Group.",
                    HelpBoxMessageType.Info
                ));
            }
            _details.Add(card);
        }

        private void DrawDefinitionDetails(AudioDefinition definition) {
            if (!definition) {
                DrawEmptyState();
                return;
            }

            ReleaseSerializedObject();
            _details.Clear();
            _serializedDefinition = new SerializedObject(definition);

            var clipCount = definition.provider is RangeAudioSourceProvider range ? range.ClipCount : 0;
            var subtitle = $"{GetMixerGroupName(definition.group)}  •  {clipCount} clip{(clipCount == 1 ? string.Empty : "s")}";
            var hero = CreateHero(definition.name, subtitle, "AudioClip Icon");
            var actions = hero.Q<VisualElement>("actions");

            var ping = new Button(() => {
                Selection.activeObject = definition;
                EditorGUIUtility.PingObject(definition);
            }) { text = "Ping" };
            ping.AddToClassList("secondary-button");
            actions.Add(ping);

            var duplicate = new Button(() => DuplicateDefinition(definition)) { text = "Duplicate" };
            duplicate.AddToClassList("secondary-button");
            actions.Add(duplicate);

            var remove = new Button(() => DeleteDefinition(definition)) { text = "Delete" };
            remove.AddToClassList("danger-button");
            actions.Add(remove);
            _details.Add(hero);

            DrawIdentityCard(definition);
            DrawProviderCard(definition);
            DrawOptionsCard();
            _details.Bind(_serializedDefinition);
        }

        private void DrawIdentityCard(AudioDefinition definition) {
            var card = CreateCard("Definition", "Asset identity, output routing, and runtime availability.");

            var nameField = new TextField("Name") {
                value = definition.name,
                isDelayed = true
            };
            nameField.RegisterValueChangedCallback(evt => RenameDefinition(definition, evt.newValue));
            card.Add(nameField);

            var groupField = new ObjectField("Mixer Group") {
                objectType = typeof(AudioMixerGroup),
                allowSceneObjects = false,
                value = definition.group
            };
            groupField.RegisterValueChangedCallback(evt => {
                Undo.RecordObject(definition, "Change Audio Mixer Group");
                definition.group = evt.newValue as AudioMixerGroup;
                SaveDefinition(definition);
                _selectedMixerGroup = definition.group;
                RefreshDefinitions();
            });
            card.Add(groupField);

            var addressableRow = new VisualElement();
            addressableRow.AddToClassList("status-row");
            var isAddressable = IsAddressable(definition);
            var status = new Label(isAddressable
                ? $"ADDRESSABLE  •  {SpookAudioRegistry.DefaultAddressableLabel}"
                : "ADDRESSABLE LABEL MISSING");
            status.AddToClassList("status-pill");
            status.EnableInClassList("status-warning", !isAddressable);
            addressableRow.Add(status);

            if (!isAddressable) {
                var fix = new Button(() => EnsureAddressableLabel(definition, true)) { text = "Assign Label" };
                fix.AddToClassList("secondary-button");
                addressableRow.Add(fix);
            }
            card.Add(addressableRow);
            _details.Add(card);
        }

        private void DrawProviderCard(AudioDefinition definition) {
            var card = CreateCard("Clip Provider", "Choose how this definition supplies audio at runtime.");

            var providerTypes = TypeCache.GetTypesDerivedFrom<IAudioSourceProvider>()
                .Where(type => !type.IsAbstract && !type.IsInterface && type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(type => ObjectNames.NicifyVariableName(type.Name), StringComparer.OrdinalIgnoreCase)
                .ToList();

            var providerNames = providerTypes.Select(type => ObjectNames.NicifyVariableName(type.Name)).ToList();
            var currentIndex = Math.Max(0, providerTypes.FindIndex(type =>
                definition.provider != null && type == definition.provider.GetType()));

            if (providerNames.Count > 0) {
                var providerPopup = new PopupField<string>("Provider", providerNames, currentIndex);
                providerPopup.RegisterValueChangedCallback(evt => {
                    var index = providerNames.IndexOf(evt.newValue);
                    if (index < 0 || index >= providerTypes.Count) return;
                    if (definition.provider?.GetType() == providerTypes[index]) return;

                    Undo.RecordObject(definition, "Change Audio Clip Provider");
                    definition.provider = Activator.CreateInstance(providerTypes[index]) as IAudioSourceProvider;
                    SaveDefinition(definition);
                    DrawDefinitionDetails(definition);
                });
                card.Add(providerPopup);
            }

            if (definition.provider is RangeAudioSourceProvider range) {
                DrawRangeProvider(definition, range, card);
            } else {
                card.Add(new HelpBox(
                    "This provider is supplied by an extension. Its serialized settings can be edited in the standard Inspector.",
                    HelpBoxMessageType.Info
                ));
                var inspector = new Button(() => {
                    Selection.activeObject = definition;
                    EditorGUIUtility.PingObject(definition);
                }) { text = "Open in Inspector" };
                inspector.AddToClassList("secondary-button");
                card.Add(inspector);
            }

            _details.Add(card);
        }

        private void DrawRangeProvider(
            AudioDefinition definition,
            RangeAudioSourceProvider provider,
            VisualElement card
        ) {
            var repeatToggle = new Toggle("Avoid Consecutive Repeats") {
                value = provider.avoidConsecutiveClipRepeats
            };
            repeatToggle.RegisterValueChangedCallback(evt => {
                Undo.RecordObject(definition, "Change Clip Repeat Setting");
                provider.avoidConsecutiveClipRepeats = evt.newValue;
                SaveDefinition(definition);
            });
            card.Add(repeatToggle);

            var clipsHeader = new VisualElement();
            clipsHeader.AddToClassList("clips-header");
            var title = new Label($"CLIPS  {provider.ClipCount}");
            title.AddToClassList("eyebrow");
            clipsHeader.Add(title);
            var headerSpacer = new VisualElement();
            headerSpacer.style.flexGrow = 1f;
            clipsHeader.Add(headerSpacer);
            if (provider.ClipCount > 0) {
                var clear = new Button(() => ClearClips(definition, provider)) { text = "Clear All" };
                clear.AddToClassList("link-button");
                clipsHeader.Add(clear);
            }
            card.Add(clipsHeader);

            var addClip = new ObjectField("Add Audio Clip") {
                objectType = typeof(AudioClip),
                allowSceneObjects = false
            };
            addClip.RegisterValueChangedCallback(evt => {
                if (evt.newValue is not AudioClip clip) return;
                AddClips(definition, provider, new[] { clip });
            });
            card.Add(addClip);

            var dropZone = new VisualElement();
            dropZone.AddToClassList("drop-zone");
            var dropIcon = new Label("⇣");
            dropIcon.AddToClassList("drop-icon");
            dropZone.Add(dropIcon);
            var dropTitle = new Label("Drop audio clips or folders");
            dropTitle.AddToClassList("drop-title");
            dropZone.Add(dropTitle);
            var dropHint = new Label("Multiple assets are supported");
            dropHint.AddToClassList("drop-hint");
            dropZone.Add(dropHint);
            RegisterClipDropZone(dropZone, definition, provider);
            card.Add(dropZone);

            if (provider.clips == null) provider.clips = new List<AudioClip>();
            if (provider.clips.Count == 0) {
                var empty = new Label("No clips yet. Add a clip above or drop a selection here.");
                empty.AddToClassList("clips-empty");
                card.Add(empty);
                return;
            }

            for (var i = 0; i < provider.clips.Count; i++) {
                var clipIndex = i;
                var row = new VisualElement();
                row.AddToClassList("clip-row");

                var index = new Label((i + 1).ToString("00"));
                index.AddToClassList("clip-index");
                row.Add(index);

                var clipField = new ObjectField {
                    objectType = typeof(AudioClip),
                    allowSceneObjects = false,
                    value = provider.clips[i]
                };
                clipField.AddToClassList("clip-field");
                clipField.RegisterValueChangedCallback(evt => {
                    Undo.RecordObject(definition, "Change Audio Clip");
                    provider.clips[clipIndex] = evt.newValue as AudioClip;
                    SaveDefinition(definition);
                    DrawDefinitionDetails(definition);
                });
                row.Add(clipField);

                var duration = new Label(FormatDuration(provider.clips[i]));
                duration.AddToClassList("clip-duration");
                row.Add(duration);

                var up = new Button(() => MoveClip(definition, provider, clipIndex, -1)) { text = "↑" };
                up.tooltip = "Move clip up";
                up.SetEnabled(i > 0);
                up.AddToClassList("clip-action");
                row.Add(up);

                var down = new Button(() => MoveClip(definition, provider, clipIndex, 1)) { text = "↓" };
                down.tooltip = "Move clip down";
                down.SetEnabled(i < provider.clips.Count - 1);
                down.AddToClassList("clip-action");
                row.Add(down);

                var delete = new Button(() => RemoveClip(definition, provider, clipIndex)) { text = "×" };
                delete.tooltip = "Remove clip";
                delete.AddToClassList("clip-remove");
                row.Add(delete);
                card.Add(row);
            }
        }

        private void DrawOptionsCard() {
            var card = CreateCard("Playback & Spatial Audio", "Defaults applied to each pooled AudioSource voice.");
            var optionsProperty = _serializedDefinition.FindProperty("options");
            var options = new PropertyField(optionsProperty);
            options.AddToClassList("options-property");
            card.Add(options);
            _details.Add(card);
        }

        private static VisualElement CreateHero(string title, string subtitle, string iconName) {
            var hero = new VisualElement();
            hero.AddToClassList("details-hero");

            var iconFrame = new VisualElement();
            iconFrame.AddToClassList("hero-icon-frame");
            var icon = new Image {
                image = EditorGUIUtility.IconContent(iconName).image,
                scaleMode = ScaleMode.ScaleToFit
            };
            icon.AddToClassList("hero-icon");
            iconFrame.Add(icon);
            hero.Add(iconFrame);

            var text = new VisualElement();
            text.AddToClassList("hero-text");
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("hero-title");
            text.Add(titleLabel);
            var subtitleLabel = new Label(subtitle);
            subtitleLabel.AddToClassList("hero-subtitle");
            text.Add(subtitleLabel);
            hero.Add(text);

            var actions = new VisualElement { name = "actions" };
            actions.AddToClassList("hero-actions");
            hero.Add(actions);
            return hero;
        }

        private static VisualElement CreateCard(string title, string subtitle) {
            var card = new VisualElement();
            card.AddToClassList("details-card");
            var heading = new Label(title);
            heading.AddToClassList("card-title");
            card.Add(heading);
            if (!string.IsNullOrWhiteSpace(subtitle)) {
                var description = new Label(subtitle);
                description.AddToClassList("card-subtitle");
                card.Add(description);
            }
            return card;
        }

        private void RegisterClipDropZone(
            VisualElement zone,
            AudioDefinition definition,
            RangeAudioSourceProvider provider
        ) {
            zone.RegisterCallback<DragUpdatedEvent>(evt => {
                if (!CollectDraggedClips().Any()) return;
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                zone.AddToClassList("drag-active");
                evt.StopPropagation();
            });
            zone.RegisterCallback<DragLeaveEvent>(_ => zone.RemoveFromClassList("drag-active"));
            zone.RegisterCallback<DragExitedEvent>(_ => zone.RemoveFromClassList("drag-active"));
            zone.RegisterCallback<DragPerformEvent>(evt => {
                var clips = CollectDraggedClips();
                zone.RemoveFromClassList("drag-active");
                if (clips.Count == 0) return;
                DragAndDrop.AcceptDrag();
                AddClips(definition, provider, clips);
                evt.StopPropagation();
            });
        }

        private static List<AudioClip> CollectDraggedClips() {
            var clips = new List<AudioClip>();
            var paths = DragAndDrop.paths ?? Array.Empty<string>();
            foreach (var path in paths) {
                if (AssetDatabase.IsValidFolder(path)) {
                    foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { path })) {
                        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                        if (clip && !clips.Contains(clip)) clips.Add(clip);
                    }
                } else {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip && !clips.Contains(clip)) clips.Add(clip);
                }
            }

            foreach (var objectReference in DragAndDrop.objectReferences) {
                if (objectReference is AudioClip clip && !clips.Contains(clip)) clips.Add(clip);
            }
            return clips;
        }

        private void AddClips(
            AudioDefinition definition,
            RangeAudioSourceProvider provider,
            IEnumerable<AudioClip> clips
        ) {
            var validClips = clips.Where(clip => clip).Distinct().ToList();
            if (validClips.Count == 0) return;

            Undo.RecordObject(definition, validClips.Count == 1 ? "Add Audio Clip" : "Add Audio Clips");
            provider.clips ??= new List<AudioClip>();
            provider.clips.AddRange(validClips);
            SaveDefinition(definition);
            DrawDefinitionDetails(definition);
            RefreshTree();
        }

        private void RemoveClip(AudioDefinition definition, RangeAudioSourceProvider provider, int index) {
            if (provider.clips == null || index < 0 || index >= provider.clips.Count) return;
            Undo.RecordObject(definition, "Remove Audio Clip");
            provider.clips.RemoveAt(index);
            SaveDefinition(definition);
            DrawDefinitionDetails(definition);
            RefreshTree();
        }

        private void ClearClips(AudioDefinition definition, RangeAudioSourceProvider provider) {
            if (provider.clips == null || provider.clips.Count == 0) return;
            if (!EditorUtility.DisplayDialog(
                    "Clear audio clips?",
                    $"Remove all clips from '{definition.name}'?",
                    "Clear Clips",
                    "Cancel")) return;

            Undo.RecordObject(definition, "Clear Audio Clips");
            provider.clips.Clear();
            SaveDefinition(definition);
            DrawDefinitionDetails(definition);
            RefreshTree();
        }

        private void MoveClip(
            AudioDefinition definition,
            RangeAudioSourceProvider provider,
            int index,
            int direction
        ) {
            var destination = index + direction;
            if (provider.clips == null || index < 0 || destination < 0 ||
                index >= provider.clips.Count || destination >= provider.clips.Count) return;

            Undo.RecordObject(definition, "Reorder Audio Clip");
            (provider.clips[index], provider.clips[destination]) =
                (provider.clips[destination], provider.clips[index]);
            SaveDefinition(definition);
            DrawDefinitionDetails(definition);
        }

        private void CreateDefinition(AudioMixerGroup mixerGroup) {
            var initialDirectory = "Assets";
            if (_selectedDefinition) {
                var selectedPath = AssetDatabase.GetAssetPath(_selectedDefinition);
                if (!string.IsNullOrWhiteSpace(selectedPath))
                    initialDirectory = Path.GetDirectoryName(selectedPath)?.Replace('\\', '/') ?? "Assets";
            }

            var path = EditorUtility.SaveFilePanelInProject(
                "Create Audio Definition",
                "New Audio Definition",
                "asset",
                "Choose where to save the new audio definition.",
                initialDirectory
            );
            if (string.IsNullOrWhiteSpace(path)) return;

            var definition = CreateInstance<AudioDefinition>();
            definition.options = AudioOptions.Default;
            definition.provider = new RangeAudioSourceProvider();
            definition.group = mixerGroup;
            AssetDatabase.CreateAsset(definition, path);
            EnsureAddressableLabel(definition, false);
            AssetDatabase.SaveAssets();

            _selectedDefinition = definition;
            _selectedMixerGroup = mixerGroup;
            Selection.activeObject = definition;
            RefreshDefinitions();
            EditorGUIUtility.PingObject(definition);
        }

        private void DuplicateDefinition(AudioDefinition source) {
            if (!source) return;
            var sourcePath = AssetDatabase.GetAssetPath(source);
            var extension = Path.GetExtension(sourcePath);
            var pathWithoutExtension = sourcePath.Substring(0, sourcePath.Length - extension.Length);
            var destination = AssetDatabase.GenerateUniqueAssetPath($"{pathWithoutExtension} Copy{extension}");
            if (!AssetDatabase.CopyAsset(sourcePath, destination)) {
                Debug.LogError($"Could not duplicate audio definition '{source.name}'.");
                return;
            }

            AssetDatabase.ImportAsset(destination);
            var duplicate = AssetDatabase.LoadAssetAtPath<AudioDefinition>(destination);
            EnsureAddressableLabel(duplicate, false);
            AssetDatabase.SaveAssets();
            _selectedDefinition = duplicate;
            _selectedMixerGroup = duplicate ? duplicate.group : null;
            Selection.activeObject = duplicate;
            RefreshDefinitions();
            if (duplicate) EditorGUIUtility.PingObject(duplicate);
        }

        private void DeleteDefinition(AudioDefinition definition) {
            if (!definition) return;
            var path = AssetDatabase.GetAssetPath(definition);
            if (!EditorUtility.DisplayDialog(
                    "Delete audio definition?",
                    $"Move '{definition.name}' to the operating system Trash?\n\n{path}",
                    "Move to Trash",
                    "Cancel")) return;

            if (!AssetDatabase.MoveAssetToTrash(path)) {
                EditorUtility.DisplayDialog("Delete failed", $"Unity could not move this asset to Trash:\n{path}", "OK");
                return;
            }

            _selectedDefinition = null;
            RefreshDefinitions();
        }

        private void RenameDefinition(AudioDefinition definition, string requestedName) {
            if (!definition) return;
            var trimmedName = requestedName?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedName) || trimmedName == definition.name) return;

            var error = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(definition), trimmedName);
            if (!string.IsNullOrEmpty(error)) {
                EditorUtility.DisplayDialog("Rename failed", error, "OK");
                DrawDefinitionDetails(definition);
                return;
            }
            AssetDatabase.SaveAssets();
            RefreshDefinitions();
        }

        private void EnsureAllAddressableLabels() {
            if (_definitions.Count == 0) return;
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings) {
                ShowMissingAddressablesSettings();
                return;
            }

            Undo.RecordObject(settings, "Label Audio Definitions");
            settings.AddLabel(SpookAudioRegistry.DefaultAddressableLabel, false);
            var changed = 0;
            foreach (var definition in _definitions) {
                if (IsAddressable(definition)) continue;
                if (EnsureAddressableLabel(definition, false)) changed++;
            }
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            RefreshDefinitions();
            ShowNotification(new GUIContent(changed == 0
                ? "Every definition is already labeled"
                : $"Labeled {changed} audio definition{(changed == 1 ? string.Empty : "s")}"));
        }

        private bool EnsureAddressableLabel(AudioDefinition definition, bool refresh) {
            if (!definition) return false;
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings) {
                ShowMissingAddressablesSettings();
                return false;
            }

            var path = AssetDatabase.GetAssetPath(definition);
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(guid)) return false;

            settings.AddLabel(SpookAudioRegistry.DefaultAddressableLabel, false);
            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.SetLabel(SpookAudioRegistry.DefaultAddressableLabel, true, true);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            if (refresh) RefreshDefinitions();
            return true;
        }

        private static bool IsAddressable(AudioDefinition definition) {
            if (!definition) return false;
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings) return false;
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition));
            var entry = settings.FindAssetEntry(guid);
            return entry != null && entry.labels.Contains(SpookAudioRegistry.DefaultAddressableLabel);
        }

        private static void ShowMissingAddressablesSettings() {
            EditorUtility.DisplayDialog(
                "Addressables are not configured",
                "Create Addressable Asset Settings before assigning the audio label.",
                "OK"
            );
        }

        private static bool MatchesSearch(AudioDefinition definition, string query) {
            if (string.IsNullOrWhiteSpace(query)) return true;
            if (definition.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (GetMixerGroupName(definition.group).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (definition.provider is not RangeAudioSourceProvider provider || provider.clips == null)
                return false;
            return provider.clips.Any(clip =>
                clip && clip.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string GetMixerGroupName(AudioMixerGroup group) =>
            group ? group.name : "Unassigned";

        private int UniqueTreeId(string value) {
            unchecked {
                const uint offset = 2166136261;
                const uint prime = 16777619;
                var hash = offset;
                foreach (var character in value) {
                    hash ^= character;
                    hash *= prime;
                }

                var id = (int)(hash & 0x7fffffff);
                if (id == 0) id = 1;
                while (!_usedTreeIds.Add(id)) id++;
                return id;
            }
        }

        private static string FormatDuration(AudioClip clip) {
            if (!clip) return "Missing";
            var duration = TimeSpan.FromSeconds(clip.length);
            return duration.TotalHours >= 1d
                ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
                : $"{(int)duration.TotalMinutes}:{duration.Seconds:00}";
        }

        private static void SaveDefinition(AudioDefinition definition) {
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssetIfDirty(definition);
        }

        private void ApplyPendingChanges() {
            if (_serializedDefinition == null) return;
            if (_serializedDefinition.targetObject)
                _serializedDefinition.ApplyModifiedProperties();
        }

        private void ReleaseSerializedObject() {
            ApplyPendingChanges();
            _serializedDefinition = null;
        }

        private void OnUndoRedo() {
            ReleaseSerializedObject();
            RefreshDefinitions();
        }

        private void QueueRefresh() {
            if (_refreshQueued) return;
            _refreshQueued = true;
            EditorApplication.delayCall += () => {
                _refreshQueued = false;
                if (!this || rootVisualElement.childCount == 0) return;
                RefreshDefinitions();
            };
        }

    }

    /// <summary>
    /// Unity's MenuItem validator can disable an Assets menu command, but cannot hide it.
    /// Registering this command on selection keeps unrelated Project assets free of audio actions.
    /// </summary>
    [InitializeOnLoad]
    internal static class AudioDefinitionAssetContextMenu {

        private const string MenuPath = "Assets/Open in Audio Library";
        private const BindingFlags StaticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;

        private static readonly MethodInfo AddMenuItemMethod = typeof(Menu).GetMethod(
            "AddMenuItem",
            StaticNonPublic,
            null,
            new[] {
                typeof(string),
                typeof(string),
                typeof(bool),
                typeof(int),
                typeof(Action),
                typeof(Func<bool>)
            },
            null
        );

        private static readonly MethodInfo RemoveMenuItemMethod = typeof(Menu).GetMethod(
            "RemoveMenuItem",
            StaticNonPublic,
            null,
            new[] { typeof(string) },
            null
        );

        private static bool _registered;
        private static AudioDefinition _contextDefinition;

        static AudioDefinitionAssetContextMenu() {
            Selection.selectionChanged += RefreshRegistration;
            AssemblyReloadEvents.beforeAssemblyReload += Remove;
            EditorApplication.delayCall += RefreshRegistration;
        }

        private static void RefreshRegistration() {
            Remove();
            _contextDefinition = Selection.activeObject as AudioDefinition;
            if (!_contextDefinition || AddMenuItemMethod == null) return;

            AddMenuItemMethod.Invoke(null, new object[] {
                MenuPath,
                string.Empty,
                false,
                19,
                (Action)OpenContextDefinition,
                (Func<bool>)(() => Selection.activeObject is AudioDefinition)
            });
            _registered = true;
        }

        private static void OpenContextDefinition() {
            if (_contextDefinition)
                AudioDefinitionWindow.OpenDefinition(_contextDefinition);
        }

        private static void Remove() {
            if (!_registered || RemoveMenuItemMethod == null) return;
            RemoveMenuItemMethod.Invoke(null, new object[] { MenuPath });
            _registered = false;
        }

    }
}
