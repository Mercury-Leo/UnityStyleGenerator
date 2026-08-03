using System.Collections.Generic;
using System.Linq;
using LeosTools.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpriteLibrary.Editor
{
    public class SpriteLibraryWindow : EditorWindow
    {
        private SpriteLibrarySettings _settings;
        private readonly HashSet<int> _expandedGroups = new();
        private int _selectedGroup = -1;
        private string _searchText = string.Empty;

        private ScrollView _treeView;
        private VisualElement _galleryGrid;
        private TextField _newGroupNameField;

        [MenuItem("Tools/Leo's Tools/Sprite Library")]
        public static void Open()
        {
            var window = GetWindow<SpriteLibraryWindow>("Sprite Library");
            window.minSize = new Vector2(600, 400);
        }

        private void OnEnable()
        {
            _settings = SpriteLibrarySettings.instance;
            Undo.undoRedoPerformed += OnUndo;

            rootVisualElement.Clear();
            rootVisualElement.style.paddingLeft = 10;
            rootVisualElement.style.paddingTop = 10;
            rootVisualElement.style.flexGrow = 1;

            rootVisualElement.Add(BuildToolbar());
            rootVisualElement.Add(BuildNewGroupRow());

            var split = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, flexGrow = 1, marginTop = 6 }
            };

            _treeView = new ScrollView
            {
                style =
                {
                    width = 220, marginRight = 8, borderRightWidth = 1,
                    borderRightColor = new Color(0.4f, 0.4f, 0.4f)
                }
            };
            split.Add(_treeView);

            var galleryScroll = new ScrollView { style = { flexGrow = 1 } };
            _galleryGrid = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap }
            };
            galleryScroll.Add(_galleryGrid);
            split.Add(galleryScroll);

            rootVisualElement.Add(split);

            rootVisualElement.Add(Utility.CreateButton("Generate", SpriteLibraryGenerator.Generate,
                "Generate Sprite Library"));

            RebuildTree();
            RebuildGallery();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndo;
        }

        private void OnUndo()
        {
            if (focusedWindow != this)
            {
                return;
            }

            RebuildTree();
            RebuildGallery();
        }

        private VisualElement BuildToolbar()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            var expandAllBtn = new Button(() =>
            {
                _expandedGroups.Clear();
                for (int i = 0; i < _settings.Groups.Count; i++)
                {
                    _expandedGroups.Add(i);
                }

                RebuildTree();
            }) { text = "Expand All" };
            row.Add(expandAllBtn);

            var collapseAllBtn = new Button(() =>
            {
                _expandedGroups.Clear();
                RebuildTree();
            }) { text = "Collapse All", style = { marginLeft = 4 } };
            row.Add(collapseAllBtn);

            var searchLabel = new Label("Search") { style = { marginLeft = 10, marginRight = 4 } };
            row.Add(searchLabel);

            var searchField = new TextField { style = { flexGrow = 1 } };
            searchField.RegisterValueChangedCallback(evt =>
            {
                _searchText = evt.newValue;
                RebuildGallery();
            });
            row.Add(searchField);

            return row;
        }

        private static Button BuildIconButton(string iconName, string tooltip, System.Action onClick)
        {
            var button = new Button(onClick)
            {
                tooltip = tooltip,
                style =
                {
                    width = 20, height = 20, marginLeft = 2,
                    alignItems = Align.Center, justifyContent = Justify.Center
                }
            };
            button.Add(new Image
            {
                image = EditorGUIUtility.IconContent(iconName).image,
                scaleMode = ScaleMode.ScaleToFit,
                style = { width = 13, height = 13 }
            });
            return button;
        }

        private VisualElement BuildNewGroupRow()
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 8, marginBottom = 4
                }
            };

            _newGroupNameField = new TextField { label = "New Group Name", style = { flexGrow = 1 } };
            var addGroupBtn = BuildIconButton("Toolbar Plus", "Add Group", OnAddGroup);
            addGroupBtn.style.width = 24;
            addGroupBtn.style.height = 24;
            addGroupBtn.style.marginLeft = 5;

            row.Add(_newGroupNameField);
            row.Add(addGroupBtn);
            return row;
        }

        private void RebuildTree()
        {
            _treeView.Clear();

            var allRow = new Label("All Sprites")
            {
                style =
                {
                    unityFontStyleAndWeight = _selectedGroup < 0 ? FontStyle.Bold : FontStyle.Normal,
                    paddingTop = 2, paddingBottom = 2
                },
                pickingMode = PickingMode.Position
            };
            allRow.RegisterCallback<ClickEvent>(_ =>
            {
                _selectedGroup = -1;
                RebuildTree();
                RebuildGallery();
            });
            _treeView.Add(allRow);

            var groups = _settings.Groups;
            for (int i = 0; i < groups.Count; i++)
            {
                _treeView.Add(BuildGroupRow(i, groups[i]));
            }
        }

        private VisualElement BuildGroupRow(int groupIndex, LibraryGroup group)
        {
            bool expanded = _expandedGroups.Contains(groupIndex);
            bool selected = _selectedGroup == groupIndex;

            var container = new VisualElement();

            var header = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 4 }
            };

            var arrowButton = new Button(() =>
            {
                if (!_expandedGroups.Remove(groupIndex))
                {
                    _expandedGroups.Add(groupIndex);
                }

                RebuildTree();
            })
            {
                text = expanded ? "▾" : "▸",
                style = { width = 20, height = 20, marginRight = 2 }
            };
            header.Add(arrowButton);

            var nameLabel = new Label(string.IsNullOrEmpty(group.Name) ? "New Group" : group.Name)
            {
                style =
                {
                    flexGrow = 1,
                    unityFontStyleAndWeight = selected ? FontStyle.Bold : FontStyle.Normal,
                    backgroundColor = selected ? new Color(0.24f, 0.37f, 0.58f, 0.6f) : Color.clear
                },
                pickingMode = PickingMode.Position
            };
            nameLabel.RegisterCallback<ClickEvent>(_ =>
            {
                _selectedGroup = selected ? -1 : groupIndex;
                RebuildTree();
                RebuildGallery();
            });
            header.Add(nameLabel);

            var addEntryBtn = BuildIconButton("Toolbar Plus", "Add Entry", () => InsertEntry(groupIndex, group.Entries.Count));
            header.Add(addEntryBtn);

            var deleteGroupBtn = BuildIconButton("Toolbar Minus", "Delete Group", () => ConfirmDeleteGroup(groupIndex));
            header.Add(deleteGroupBtn);

            container.Add(header);

            if (expanded)
            {
                var body = new VisualElement { style = { marginLeft = 22, marginTop = 2 } };

                // Shared across the callbacks below — LibraryGroup is a struct, so cross-field
                // edits must mutate one shared copy or they revert each other.
                var current = group;

                var nameField = new TextField("Group Name") { value = group.Name };
                nameField.RegisterValueChangedCallback(evt =>
                {
                    current.Name = evt.newValue;
                    UpdateGroup(groupIndex, current);
                });
                body.Add(nameField);

                container.Add(body);
            }

            return container;
        }

        private void RebuildGallery()
        {
            _galleryGrid.Clear();

            var groups = _settings.Groups;
            bool hasSearch = !string.IsNullOrEmpty(_searchText);

            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                var group = groups[groupIndex];

                if (!hasSearch && _selectedGroup >= 0 && _selectedGroup != groupIndex)
                {
                    continue;
                }

                var entries = group.Entries;
                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    var entry = entries[entryIndex];

                    if (hasSearch && !Utility.MatchesSearch(_searchText, group.Name, entry.Name))
                    {
                        continue;
                    }

                    _galleryGrid.Add(BuildEntryCard(groupIndex, group.Name, entryIndex, entry));
                }
            }
        }

        private VisualElement BuildEntryCard(int groupIndex, string groupName, int entryIndex, LibraryEntry entry)
        {
            var border = new Color(0.4f, 0.4f, 0.4f);
            var card = new VisualElement
            {
                style =
                {
                    width = 160, marginRight = 8, marginBottom = 8, paddingTop = 4, paddingBottom = 4,
                    paddingLeft = 4, paddingRight = 4,
                    borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                    borderTopColor = border, borderBottomColor = border, borderLeftColor = border,
                    borderRightColor = border,
                    borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };

            card.Add(new Label(groupName) { style = { fontSize = 10, color = new Color(0.65f, 0.65f, 0.65f) } });

            var sprite = entry.Sprite;
            var thumbnail = new Image
            {
                image = sprite != null ? AssetPreview.GetAssetPreview(sprite) ?? AssetPreview.GetMiniThumbnail(sprite) : null,
                scaleMode = ScaleMode.ScaleToFit,
                style = { width = 64, height = 64, alignSelf = Align.Center, marginTop = 4, marginBottom = 4 }
            };
            card.Add(thumbnail);

            if (sprite != null && AssetPreview.GetAssetPreview(sprite) == null)
            {
                thumbnail.schedule.Execute(() =>
                {
                    var preview = AssetPreview.GetAssetPreview(sprite);
                    if (preview != null)
                    {
                        thumbnail.image = preview;
                    }
                }).Every(100).Until(() => AssetPreview.GetAssetPreview(sprite) != null);
            }

            // Shared across the three field callbacks below — LibraryEntry is a struct, so cross-field
            // edits must mutate one shared copy or they revert each other.
            var current = entry;

            var nameField = new TextField { value = entry.Name };
            nameField.RegisterValueChangedCallback(evt =>
            {
                current.Name = evt.newValue;
                UpdateEntry(groupIndex, entryIndex, current);
            });
            card.Add(nameField);

            var typeField = new EnumField(entry.Type);
            typeField.Init(entry.Type);
            typeField.RegisterValueChangedCallback(evt =>
            {
                current.Type = (SpriteStyleType)evt.newValue;
                UpdateEntry(groupIndex, entryIndex, current);
            });
            card.Add(typeField);

            var spriteField = new ObjectField { objectType = typeof(Sprite), value = entry.Sprite };
            spriteField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is not Sprite newSprite)
                {
                    return;
                }

                string assetPath = AssetDatabase.GetAssetPath(newSprite);
                if (!string.IsNullOrEmpty(assetPath) && assetPath.Contains("Resources"))
                {
                    Debug.LogWarning("Sprites from Resources folder are not allowed.");
                    spriteField.SetValueWithoutNotify(current.Sprite);
                    return;
                }

                current.Sprite = newSprite;

                if (string.IsNullOrEmpty(current.Name))
                {
                    current.Name = newSprite.name;
                    nameField.SetValueWithoutNotify(current.Name);
                }

                UpdateEntry(groupIndex, entryIndex, current);
            });
            card.Add(spriteField);

            var removeButton = new Button(() => OnDeleteEntry(groupIndex, entryIndex)) { text = "Remove" };
            card.Add(removeButton);

            return card;
        }

        private void ConfirmDeleteGroup(int groupIndex)
        {
            if (!GroupExists(groupIndex))
            {
                return;
            }

            var groupName = _settings.Groups[groupIndex].Name;
            if (!EditorUtility.DisplayDialog("Delete Group",
                $"Are you sure you want to delete the group '{groupName}'?", "Delete", "Cancel"))
            {
                return;
            }

            Undo.RegisterCompleteObjectUndo(_settings, "Delete Group");
            var list = _settings.Groups.ToList();
            list.RemoveAt(groupIndex);
            _settings.Groups = list;
            _settings.SaveDirty();
            EditorUtility.SetDirty(_settings);

            var shiftedExpanded = new HashSet<int>();
            foreach (var expandedIndex in _expandedGroups)
            {
                if (expandedIndex < groupIndex)
                {
                    shiftedExpanded.Add(expandedIndex);
                }
                else if (expandedIndex > groupIndex)
                {
                    shiftedExpanded.Add(expandedIndex - 1);
                }
            }

            _expandedGroups.Clear();
            foreach (var index in shiftedExpanded)
            {
                _expandedGroups.Add(index);
            }

            if (_selectedGroup == groupIndex)
            {
                _selectedGroup = -1;
            }
            else if (_selectedGroup > groupIndex)
            {
                _selectedGroup--;
            }

            RebuildTree();
            RebuildGallery();

            if (_settings.AutoRegenerate)
            {
                SpriteLibraryGenerator.RequestRegenerate();
            }
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

            RebuildTree();
            RebuildGallery();

            if (_settings.AutoRegenerate)
            {
                SpriteLibraryGenerator.RequestRegenerate();
            }
        }

        private void InsertEntry(int groupIndex, int entryIndex)
        {
            if (!GroupExists(groupIndex))
            {
                return;
            }

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

            RebuildTree();
            RebuildGallery();

            if (_settings.AutoRegenerate)
            {
                SpriteLibraryGenerator.RequestRegenerate();
            }
        }

        private void OnDeleteEntry(int groupIndex, int entryIndex)
        {
            if (!EntryExists(groupIndex, entryIndex))
            {
                return;
            }

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

            RebuildTree();
            RebuildGallery();

            if (_settings.AutoRegenerate)
            {
                SpriteLibraryGenerator.RequestRegenerate();
            }
        }

        private void UpdateGroup(int groupIndex, LibraryGroup group)
        {
            if (!GroupExists(groupIndex))
            {
                return;
            }

            Undo.RegisterCompleteObjectUndo(_settings, "Edit Group");
            var groups = _settings.Groups;
            groups[groupIndex] = group;
            _settings.Groups = groups;
            _settings.SaveDirty();
            EditorUtility.SetDirty(_settings);

            RebuildGallery();

            if (_settings.AutoRegenerate)
            {
                SpriteLibraryGenerator.RequestRegenerate();
            }
        }

        private void UpdateEntry(int groupIndex, int entryIndex, LibraryEntry entry)
        {
            if (!EntryExists(groupIndex, entryIndex))
            {
                return;
            }

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

            RebuildTree();

            if (_settings.AutoRegenerate)
            {
                SpriteLibraryGenerator.RequestRegenerate();
            }
        }

        private bool GroupExists(int groupIndex) => _settings.Groups.Count > groupIndex;

        private bool EntryExists(int groupIndex, int entryIndex)
        {
            if (!GroupExists(groupIndex))
            {
                return false;
            }

            return _settings.Groups[groupIndex].Entries.Count > entryIndex;
        }
    }
}
