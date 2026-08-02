# Sprite Library window: rebuild as tree + gallery

## Problem

`SpriteLibraryWindow` builds its entire UI imperatively in `OnEnable`/`BuildUI`: one long vertical list of nested `Foldout`s (group → entries), each entry showing only a name field, type enum, and a small default `ObjectField` thumbnail. Problems reported:

- No way to see more than a couple of sprites at once — everything is buried behind foldouts you expand one at a time.
- No expand/collapse-all, no search — with more than a handful of groups/entries, navigating means manually opening and scanning every foldout.
- Visual density is poor (default `Foldout`/`ObjectField` styling, no thumbnails larger than the tiny default preview).

This spec rebuilds the window's UI only. `SpriteLibrarySettings` (the `Groups`/`LibraryEntry` data), `SpriteLibraryGenerator`, Undo support, and the `Tools > Leo's Tools > Sprite Library` entry point are unchanged — no breaking changes to saved data.

## Layout: two-pane, Project-window style

**Left pane — group tree.** One row per group (rename field, delete button, add-entry button, same as today), expandable to list its entries by name as plain text rows underneath. Clicking a group filters the right-hand gallery to that group's entries; clicking it again (or an "All" pseudo-row at the top) clears the filter back to showing everything.

**Toolbar above the tree:** `Expand All` / `Collapse All` buttons (toggle every group's expanded state in the tree) and a search field.

**Right pane — sprite gallery.** A wrapping grid of cards, one per `LibraryEntry`. By default (no group selected, no search) it shows every entry from every group — the "see all the sprites" view. Each card: sprite thumbnail (bigger than the default `ObjectField` preview), inline-editable Name field, Type enum field, Sprite object field, and a remove button — the same fields/validation the current entry foldout has (Resources-folder rejection, duplicate/empty-name warnings via `Utility.TryRegisterIdentifier` where applicable), just laid out as a card instead of a nested foldout row.

**Search field** filters by substring match against group name or entry name, case-insensitive. Search takes priority over the group filter: typing in it searches across all groups regardless of which group is selected in the tree; clearing it returns to the tree's current group filter (or "All").

## Data flow & editing

No changes to how data is read/written: every mutation still goes through the same `Undo.RegisterCompleteObjectUndo` + `_settings.Groups = ...` + `_settings.SaveDirty()` pattern already in place (which now also drives the debounced auto-regenerate from the prior change). The rebuild only changes what widgets are built and how they're laid out; the update/undo plumbing per group/entry stays the same shape, just triggered from card fields instead of foldout fields.

## File structure

Single `SpriteLibraryWindow.cs` stays a reasonable size by splitting build methods by responsibility (`BuildToolbar`, `BuildTree`, `BuildGallery`, `BuildCard`), not by splitting into multiple classes — there's no reuse or independent-testing need that would justify separate files for one window.

One pure, testable addition: a static filter-matching function (given search text + a group/entry, does it match) gets pulled out to `Utility` rather than inlined in the window, since it's the one piece of actual branching logic in this change (empty search → match all; else substring match against group or entry name).

## Testing

New `Tests/SpriteLibraryFilterTests.cs` (same plain-NUnit, no-mocking style as the existing tests): empty search matches everything, group-name match, entry-name match, case-insensitivity, no match. The rest of the change is UI layout/wiring with no independently meaningful assertions — verified manually in the Editor (see manual check below), not unit tested.

**Manual verification after implementation:** open `Tools > Leo's Tools > Sprite Library` on a settings asset with 2+ groups and several entries each; confirm Expand All/Collapse All, group-filter click, search (by group and by entry name), inline edits (name/type/sprite) persist and Undo works, delete-group confirmation dialog and Resources-folder rejection still fire, and `Generate` still produces identical output to before the rebuild.

## Out of scope

- Drag-and-drop reordering of groups/entries, multi-select/bulk edit, virtualized/paginated grid for very large libraries — not requested, current entry counts don't warrant it.
- Any change to `SpriteLibrarySettings`, `SpriteLibraryGenerator`, or the auto-regenerate hook added previously.
