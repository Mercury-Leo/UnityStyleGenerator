using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityStyleGenerator.Editor.Settings;

namespace UnityStyleGenerator.Editor.SpriteLibrary
{
    public class SpriteLibraryWindow : EditorWindow
    {
        private SpriteLibrarySettings _settings;
        private ScrollView _scrollView;
        private TextField _newGroupNameField;

        [MenuItem("Window/Sprite Library Settings")]
        public static void Open()
        {
            var window = GetWindow<SpriteLibraryWindow>("Sprite Library Settings");
            window.minSize = new Vector2(500, 400);
        }

        private void OnEnable()
        {
            _settings = SpriteLibrarySettings.instance;
            Undo.undoRedoPerformed += OnUndo;
            rootVisualElement.Clear();
            rootVisualElement.style.paddingLeft = 10;
            rootVisualElement.style.paddingTop = 10;
            var groupRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 10 }
            };
            _newGroupNameField = new TextField { label = "New Group Name", style = { flexGrow = 1 } };
            var addGroupBtn = new Button(OnAddGroup)
            {
                text = "+",
                tooltip = "Add Group",
                style = { width = 24, height = 24, unityTextAlign = TextAnchor.MiddleCenter, marginLeft = 5 }
            };
            groupRow.Add(_newGroupNameField);
            groupRow.Add(addGroupBtn);
            rootVisualElement.Add(groupRow);
            _scrollView = new ScrollView { style = { flexGrow = 1 } };
            rootVisualElement.Add(_scrollView);
            BuildUI();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndo;
        }

        private void OnUndo()
        {
            if (focusedWindow == this && _scrollView != null)
            {
                BuildUI();
            }
        }

        private void BuildUI()
        {
            _scrollView.Clear();
            var groups = _settings.Groups;
            for (int i = 0; i < groups.Count; i++)
            {
                int groupIndex = i;
                var group = groups[groupIndex];
                var groupFoldout = new Foldout
                {
                    text = string.IsNullOrEmpty(group.Name) ? "New Group" : group.Name,
                    style =
                    {
                        marginTop = 10,
                        borderTopWidth = 1,
                        borderTopColor = new Color(0.7f, 0.7f, 0.7f),
                        paddingTop = 4
                    }
                };
                _scrollView.Add(groupFoldout);
                var deleteGroupBtn = new Button(() => ConfirmDeleteGroup(groupIndex))
                {
                    text = "X",
                    tooltip = "Delete Group",
                    style =
                    {
                        width = 20, height = 20, unityTextAlign = TextAnchor.MiddleCenter, marginLeft = 5,
                        alignItems = Align.Center
                    }
                };
                var groupLabel = groupFoldout.Q<Label>();
                groupLabel?.parent.Add(deleteGroupBtn);
                var addEntryBtn = new Button(() => InsertEntry(groupIndex, group.Entries.Count))
                {
                    text = "+",
                    tooltip = "Add Entry",
                    style =
                    {
                        width = 20, height = 20, unityTextAlign = TextAnchor.MiddleCenter, marginLeft = 2,
                        alignItems = Align.Center
                    }
                };
                groupLabel?.parent.Add(addEntryBtn);
                var nameField = new TextField("Group Name") { value = group.Name };
                nameField.RegisterValueChangedCallback(evt =>
                {
                    group.Name = evt.newValue;
                    UpdateGroup(groupIndex, group);
                    groupFoldout.text = string.IsNullOrEmpty(evt.newValue) ? "New Group" : evt.newValue;
                });
                groupFoldout.Add(nameField);
                for (int j = 0; j < group.Entries.Count; j++)
                {
                    int entryIndex = j;
                    var entry = group.Entries[entryIndex];
                    var entryFoldout = new Foldout
                    {
                        text = string.IsNullOrEmpty(entry.Name) ? $"Entry {entryIndex + 1}" : entry.Name,
                        style =
                        {
                            marginTop = 5,
                            marginLeft = 20,
                            borderLeftWidth = 1,
                            borderLeftColor = new Color(0.7f, 0.7f, 0.7f),
                            paddingLeft = 4
                        }
                    };
                    groupFoldout.Add(entryFoldout);
                    var removeButton = new Button(() => OnDeleteEntry(groupIndex, entryIndex))
                    {
                        text = "-",
                        tooltip = "Remove Entry",
                        style =
                        {
                            width = 20, height = 20, unityTextAlign = TextAnchor.MiddleCenter, marginLeft = 5,
                            alignItems = Align.Center
                        }
                    };
                    var entryLabel = entryFoldout.Q<Label>();
                    entryLabel?.parent.Add(removeButton);
                    var entryNameField = new TextField("Name") { value = entry.Name };
                    entryNameField.RegisterValueChangedCallback(evt =>
                    {
                        entry.Name = evt.newValue;
                        UpdateEntry(groupIndex, entryIndex, entry);
                        entryFoldout.text = string.IsNullOrEmpty(evt.newValue)
                            ? $"Entry {entryIndex + 1}"
                            : evt.newValue;
                    });
                    entryFoldout.Add(entryNameField);
                    var typeField = new EnumField("Type", entry.Type);
                    typeField.Init(entry.Type);
                    typeField.RegisterValueChangedCallback(evt =>
                    {
                        entry.Type = (SpriteStyleType)evt.newValue;
                        UpdateEntry(groupIndex, entryIndex, entry);
                    });
                    entryFoldout.Add(typeField);
                    var spriteField = new ObjectField("Sprite") { objectType = typeof(Sprite), value = entry.Sprite };
                    spriteField.RegisterValueChangedCallback(evt =>
                    {
                        entry.Sprite = (Sprite)evt.newValue;
                        UpdateEntry(groupIndex, entryIndex, entry);
                    });
                    entryFoldout.Add(spriteField);
                }
            }
        }

        private void ConfirmDeleteGroup(int groupIndex)
        {
            if (!GroupExists(groupIndex)) return;
            var groupName = _settings.Groups[groupIndex].Name;
            if (!EditorUtility.DisplayDialog("Delete Group",
                $"Are you sure you want to delete the group '{groupName}'?", "Delete", "Cancel")) return;
            Undo.RegisterCompleteObjectUndo(_settings, "Delete Group");
            var list = _settings.Groups.ToList();
            list.RemoveAt(groupIndex);
            _settings.Groups = list;
            _settings.SaveDirty();
            EditorUtility.SetDirty(_settings);
            BuildUI();
        }

        private void OnAddGroup()
        {
            Undo.RegisterCompleteObjectUndo(_settings, "Add Group");
            var groupName = _newGroupNameField.value;
            var groups = _settings.Groups;
            groups.Add(new LibraryGroup
            {
                Name = groupName,
                Entries = new List<LibraryEntry>
                {
                    new() { Name = string.Empty, Sprite = null, Type = SpriteStyleType.Background }
                }
            });
            _settings.Groups = groups;
            _settings.SaveDirty();
            EditorUtility.SetDirty(_settings);
            _newGroupNameField.value = string.Empty;
            BuildUI();
        }

        private void InsertEntry(int groupIndex, int entryIndex)
        {
            if (!GroupExists(groupIndex)) return;
            Undo.RegisterCompleteObjectUndo(_settings, "Add Entry");
            var groups = _settings.Groups;
            var group = groups[groupIndex];
            var entries = group.Entries;
            entries.Insert(entryIndex,
                new LibraryEntry { Name = string.Empty, Sprite = null, Type = SpriteStyleType.Background });
            group.Entries = entries;
            groups[groupIndex] = group;
            _settings.Groups = groups;
            _settings.SaveDirty();
            EditorUtility.SetDirty(_settings);
            BuildUI();
        }

        private void OnDeleteEntry(int groupIndex, int entryIndex)
        {
            if (!EntryExists(groupIndex, entryIndex)) return;
            Undo.RegisterCompleteObjectUndo(_settings, "Delete Entry");
            var groups = _settings.Groups;
            var group = groups[groupIndex];
            var entries = group.Entries;
            entries.RemoveAt(entryIndex);
            group.Entries = entries;
            groups[groupIndex] = group;
            _settings.Groups = groups;
            _settings.SaveDirty();
            EditorUtility.SetDirty(_settings);
            BuildUI();
        }

        private void UpdateGroup(int groupIndex, LibraryGroup group)
        {
            if (!GroupExists(groupIndex)) return;
            Undo.RegisterCompleteObjectUndo(_settings, "Edit Group");
            var groups = _settings.Groups;
            groups[groupIndex] = group;
            _settings.Groups = groups;
            _settings.SaveDirty();
            EditorUtility.SetDirty(_settings);
        }

        private void UpdateEntry(int groupIndex, int entryIndex, LibraryEntry entry)
        {
            if (!EntryExists(groupIndex, entryIndex)) return;
            Undo.RegisterCompleteObjectUndo(_settings, "Edit Entry");
            var groupsList = _settings.Groups;
            var group = groupsList[groupIndex];
            var entries = group.Entries;
            entries[entryIndex] = entry;
            group.Entries = entries;
            groupsList[groupIndex] = group;
            _settings.Groups = groupsList;
            _settings.SaveDirty();
            EditorUtility.SetDirty(_settings);
        }

        private bool GroupExists(int groupIndex) => _settings.Groups.Count > groupIndex;

        private bool EntryExists(int groupIndex, int entryIndex)
        {
            if (!GroupExists(groupIndex)) return false;
            return _settings.Groups[groupIndex].Entries.Count > entryIndex;
        }
    }
}