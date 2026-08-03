# Sprite Library Window Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild `SpriteLibraryWindow` as a two-pane (group tree + sprite gallery) editor window with search and expand/collapse-all, with zero changes to the underlying `SpriteLibrarySettings` data model.

**Architecture:** Left `ScrollView` renders a tree of groups (rename field, delete/add-entry buttons, expandable to a plain-text list of entry names). Right `ScrollView` renders a flex-wrap grid of entry cards (thumbnail + inline-editable Name/Type/Sprite + remove button) — showing every entry from every group by default, filtered by the selected group and/or the search field. One pure filter-matching function is extracted to `Utility` so it's unit-testable; everything else is UI Toolkit construction verified by compiling in the Editor and manual exercise of the window.

**Tech Stack:** C#, Unity Editor UI Toolkit (`UnityEditor.UIElements` / `UnityEngine.UIElements`), NUnit (Unity Test Framework, EditMode).

## Global Constraints

- No changes to `SpriteLibrarySettings`, `SpriteLibraryGenerator`, or the auto-regenerate hook added previously — data model and generation are out of scope (per `docs/superpowers/specs/2026-08-02-sprite-library-window-redesign-design.md`).
- `Tools > Leo's Tools > Sprite Library` menu path and the `Generate` button stay exactly where they are.
- Existing validation must be preserved exactly: Resources-folder sprites rejected, delete-group confirmation dialog, `Undo.RegisterCompleteObjectUndo` around every mutation with the same action names (`"Add Group"`, `"Delete Group"`, `"Add Entry"`, `"Delete Entry"`, `"Edit Group"`, `"Edit Entry"`).
- Unity Editor for compile/test verification: `C:\Program Files\Unity\Hub\Editor\6000.4.6f1\Editor\Unity.exe`, project path `C:\UnityProjects\Ulink`.

---

### Task 1: Extract a testable search-filter helper

**Files:**
- Modify: `Assets/UnityStyleGenerator/Editor/Utility.cs`
- Test: `Assets/UnityStyleGenerator/Tests/SpriteLibraryFilterTests.cs` (new)

**Interfaces:**
- Produces: `internal static bool Utility.MatchesSearch(string search, string groupName, string entryName)` — empty/null `search` matches everything; otherwise case-insensitive substring match against `groupName` or `entryName`. Task 2's gallery filtering calls this directly.

- [ ] **Step 1: Write the failing test**

Create `Assets/UnityStyleGenerator/Tests/SpriteLibraryFilterTests.cs`:

```csharp
using LeosTools.Editor;
using NUnit.Framework;

namespace StyleGenerator.Editor.Tests
{
    public class SpriteLibraryFilterTests
    {
        [Test]
        public void MatchesSearch_EmptySearch_MatchesEverything()
        {
            Assert.That(Utility.MatchesSearch(string.Empty, "Buttons", "Primary"), Is.True);
        }

        [Test]
        public void MatchesSearch_GroupNameSubstring_Matches()
        {
            Assert.That(Utility.MatchesSearch("butt", "Buttons", "Primary"), Is.True);
        }

        [Test]
        public void MatchesSearch_EntryNameSubstring_Matches()
        {
            Assert.That(Utility.MatchesSearch("prim", "Buttons", "Primary"), Is.True);
        }

        [Test]
        public void MatchesSearch_CaseInsensitive_Matches()
        {
            Assert.That(Utility.MatchesSearch("BUTTONS", "Buttons", "Primary"), Is.True);
        }

        [Test]
        public void MatchesSearch_NoSubstringMatch_ReturnsFalse()
        {
            Assert.That(Utility.MatchesSearch("cursor", "Buttons", "Primary"), Is.False);
        }
    }
}
```

- [ ] **Step 2: Run the test suite to verify it fails to compile**

Run:
```bash
"/c/Program Files/Unity/Hub/Editor/6000.4.6f1/Editor/Unity.exe" -batchmode -projectPath "/c/UnityProjects/Ulink" -runTests -testPlatform EditMode -testFilter "SpriteLibraryFilterTests" -testResults "/c/UnityProjects/Ulink/Assets/UnityStyleGenerator/.superpowers/test-results/task1.xml" -logFile "/c/UnityProjects/Ulink/Assets/UnityStyleGenerator/.superpowers/test-results/task1.log"
```
Expected: non-zero exit, log contains `error CS0117` (or similar — `Utility` has no `MatchesSearch`).

- [ ] **Step 3: Implement `Utility.MatchesSearch`**

In `Assets/UnityStyleGenerator/Editor/Utility.cs`, add this method to the `Utility` class (next to `TryRegisterIdentifier` — same internal-helper visibility):

```csharp
internal static bool MatchesSearch(string search, string groupName, string entryName)
{
    if (string.IsNullOrEmpty(search))
    {
        return true;
    }

    return (groupName?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
           (entryName?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
}
```

`System` is already imported in this file (used by `Exception`), so no new `using` is needed.

- [ ] **Step 4: Run the test suite to verify it passes**

Run the same command as Step 2 (point `-testResults`/`-logFile` at `task1-pass.xml`/`task1-pass.log` to avoid overwriting evidence of the red run).
Expected: exit code 0, `task1-pass.xml` shows 5 tests passed, 0 failed.

- [ ] **Step 5: Commit**

```bash
git add Assets/UnityStyleGenerator/Editor/Utility.cs Assets/UnityStyleGenerator/Tests/SpriteLibraryFilterTests.cs Assets/UnityStyleGenerator/Tests/SpriteLibraryFilterTests.cs.meta
git commit -m "feat: extract Utility.MatchesSearch for Sprite Library search filtering"
```

(The `.meta` file is generated by Unity the first time the Editor imports the new script — if it isn't present yet, open/focus the Unity Editor once so it imports the new file before staging, or let the batch-mode test run above generate it.)

---

### Task 2: Rebuild `SpriteLibraryWindow` as tree + gallery

**Files:**
- Modify: `Assets/UnityStyleGenerator/Editor/SpriteLibrary/SpriteLibraryWindow.cs` (full rewrite)

**Interfaces:**
- Consumes: `Utility.MatchesSearch(string, string, string)` from Task 1; `SpriteLibrarySettings.instance`, `.Groups`, `.SaveDirty()` (unchanged, from `SpriteLibrarySettings.cs`); `SpriteLibraryGenerator.Generate()` (unchanged); `LibraryGroup { Name, Entries }`, `LibraryEntry { Name, Type, Sprite }`, `SpriteStyleType` (unchanged, from `SpriteLibrarySettings.cs`).
- Produces: nothing new consumed elsewhere — this is the leaf UI for the feature.

- [ ] **Step 1: Replace the full contents of `SpriteLibraryWindow.cs`**

```csharp
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
            var addGroupBtn = new Button(OnAddGroup)
            {
                text = "+", tooltip = "Add Group",
                style = { width = 24, height = 24, unityTextAlign = TextAnchor.MiddleCenter, marginLeft = 5 }
            };

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
                text = expanded ? "\u25be" : "\u25b8",
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

            var addEntryBtn = new Button(() => InsertEntry(groupIndex, group.Entries.Count))
            {
                text = "+", tooltip = "Add Entry",
                style = { width = 20, height = 20, marginLeft = 2 }
            };
            header.Add(addEntryBtn);

            var deleteGroupBtn = new Button(() => ConfirmDeleteGroup(groupIndex))
            {
                text = "X", tooltip = "Delete Group",
                style = { width = 20, height = 20, marginLeft = 2 }
            };
            header.Add(deleteGroupBtn);

            container.Add(header);

            if (expanded)
            {
                var body = new VisualElement { style = { marginLeft = 22, marginTop = 2 } };

                var nameField = new TextField("Group Name") { value = group.Name };
                nameField.RegisterValueChangedCallback(evt =>
                {
                    var updated = group;
                    updated.Name = evt.newValue;
                    UpdateGroup(groupIndex, updated);
                });
                body.Add(nameField);

                foreach (var entry in group.Entries)
                {
                    body.Add(new Label(string.IsNullOrEmpty(entry.Name) ? "(unnamed)" : entry.Name)
                    {
                        style = { marginLeft = 4, fontSize = 11, color = new Color(0.75f, 0.75f, 0.75f) }
                    });
                }

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

            var thumbnail = new Image
            {
                image = entry.Sprite != null
                    ? EditorGUIUtility.ObjectContent(entry.Sprite, typeof(Sprite)).image
                    : null,
                scaleMode = ScaleMode.ScaleToFit,
                style = { width = 64, height = 64, alignSelf = Align.Center, marginTop = 4, marginBottom = 4 }
            };
            card.Add(thumbnail);

            var nameField = new TextField { value = entry.Name };
            nameField.RegisterValueChangedCallback(evt =>
            {
                var updated = entry;
                updated.Name = evt.newValue;
                UpdateEntry(groupIndex, entryIndex, updated);
            });
            card.Add(nameField);

            var typeField = new EnumField("Type", entry.Type);
            typeField.Init(entry.Type);
            typeField.RegisterValueChangedCallback(evt =>
            {
                var updated = entry;
                updated.Type = (SpriteStyleType)evt.newValue;
                UpdateEntry(groupIndex, entryIndex, updated);
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
                    spriteField.SetValueWithoutNotify(entry.Sprite);
                    return;
                }

                var updated = entry;
                updated.Sprite = newSprite;

                if (string.IsNullOrEmpty(updated.Name))
                {
                    updated.Name = newSprite.name;
                    nameField.SetValueWithoutNotify(updated.Name);
                }

                UpdateEntry(groupIndex, entryIndex, updated);
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
```

Notes for the implementer:
- `UpdateEntry` rebuilds only the tree (so the read-only entry-name label there stays in sync); it does **not** rebuild the gallery, so the `TextField`/`EnumField`/`ObjectField` the user is actively editing keeps focus.
- `UpdateGroup` rebuilds only the gallery (so each card's group-name tag stays in sync); it does **not** rebuild the tree, so the group rename field keeps focus while typing.
- Structural changes (add/delete group, add/delete entry, undo/redo) rebuild both panes — there's no focus to preserve across a structural change.
- `ConfirmDeleteGroup` re-indexes `_expandedGroups`/`_selectedGroup` after removal since, unlike the old foldout-based window, expand/selection state now persists across rebuilds instead of resetting every time.

- [ ] **Step 2: Compile-check in the Editor**

Run:
```bash
"/c/Program Files/Unity/Hub/Editor/6000.4.6f1/Editor/Unity.exe" -batchmode -quit -nographics -projectPath "/c/UnityProjects/Ulink" -logFile "/c/UnityProjects/Ulink/Assets/UnityStyleGenerator/.superpowers/test-results/task2-compile.log"
```
Then check the log for compile errors:
```bash
grep -i "error CS" "/c/UnityProjects/Ulink/Assets/UnityStyleGenerator/.superpowers/test-results/task2-compile.log"
```
Expected: no matches (empty output = clean compile).

- [ ] **Step 3: Manual verification in the Editor**

Open the project in the Unity Editor, then `Tools > Leo's Tools > Sprite Library`, against a settings asset with at least 2 groups and 2+ entries each (create test data if none exists), and confirm:

- `Expand All` opens every group's entry-name list in the tree; `Collapse All` closes them all.
- The gallery shows every entry from every group by default.
- Clicking a group in the tree filters the gallery to that group's entries only; clicking it again shows everything again.
- Typing in the search field filters the gallery by group name and by entry name, case-insensitively, and overrides the group-click filter while text is present.
- Editing a card's Name/Type/Sprite field updates the data (visible after reopening the window or via the tree's entry-name label) without losing focus mid-edit.
- Assigning a sprite from a `Resources` folder is rejected with the existing warning.
- Deleting a group still shows the confirmation dialog; deleting the group that's currently selected/expanded doesn't crash or mis-highlight another group.
- `Ctrl+Z`/`Ctrl+Y` after an edit undoes/redoes it and the window refreshes to match.
- `Generate` still produces the same `.uss`/`.tss`/`SpriteClasses.g.cs` output as before the rebuild (spot-check one file).

- [ ] **Step 4: Commit**

```bash
git add Assets/UnityStyleGenerator/Editor/SpriteLibrary/SpriteLibraryWindow.cs
git commit -m "feat: rebuild Sprite Library window as tree + gallery layout"
```
