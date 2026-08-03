# Unity Style Generator

UnityStyleGenerator is a Unity Editor extension that automates the creation of strongly-typed C# classes for styling, replacing hard-coded strings and improving maintainability. It has two generators that share the same underlying pipeline:

- **Style Classes** — scans your `.uss`/`.tss` files and generates a C# class per file, one `StyleEntry` field per CSS class.
- **Sprite Library** — lets you organize sprites into named groups in an editor window, then generates the USS variables/classes (and optionally a combined theme) for them, feeding straight back into the Style Classes generator.

Both can regenerate automatically as you work, or on demand via a menu item.

## Features

- Generate C# classes from USS/TSS files — eliminates magic strings for style class names.
- Sprite Library editor window: organize sprites into named groups, see every sprite across every group in one searchable gallery, and generate the corresponding USS/theme/C# classes from them.
- Auto Regenerate (toggleable per generator): Style Classes regenerate when a watched `.uss`/`.tss` file changes; Sprite Library regenerates shortly after you stop editing in its window.

## Installation

Installing as GIT dependency via Package Manager
1. Open Package Manager (Window -> Package Manager)
2. Click `+` button on the upper left of the window, select "Add package from git URL..."
3. Enter the following URL and click the `Add` button

   ```
   https://github.com/Mercury-Leo/UnityStyleGenerator.git
   ```

## Usage

### Style Classes

1. Open **Edit > Project Settings > Leo's Tools > Style Generator**.
   - **USS Folder** — folder to scan (recursively) for `.uss`/`.tss` files.
   - **Target Folder** — where the generated `StyleClasses.g.cs` is written.
   - **Style Prefix** — prefix added to every generated class name.
   - **Auto Regenerate** — when on (default), regenerates automatically whenever a `.uss`/`.tss` file changes under USS Folder. Turn off to require a manual Generate.
2. Click **Generate** (on the settings page, or **Tools > Leo's Tools > Generate Styles**). Each source file becomes a C# class named `{Prefix}{FileName}`, and each CSS class inside it becomes a `static StyleEntry` field — so `.primary { }` in `Buttons.uss` becomes `Style_Buttons.Primary`.

### Sprite Library

1. Open **Edit > Project Settings > Leo's Tools > Sprite Library**.
   - **Target Folder** — where the generated per-group USS files, the theme, and `SpriteClasses.g.cs` are written.
   - **Generate Theme** — also emit a combined `.tss` theme file importing every group's stylesheet.
   - **Theme Name** / **Sprite Prefix** — naming for the generated theme and sprite classes.
   - **Auto Regenerate** — when on (default), regenerates a short delay after you stop editing groups/entries in the window (edits are debounced, so a burst of edits triggers one regeneration, not one per keystroke). Turn off to require a manual Generate.
2. Open the editor via **Tools > Leo's Tools > Sprite Library**. The window has two panes:
   - **Left — group tree.** One row per group, with an add-entry button, a delete-group button, and an expand arrow that reveals a rename field. **Expand All** / **Collapse All** toggle every group at once. Click a group's name to filter the gallery to just that group; click it again (or click **All Sprites**) to see everything again.
   - **Right — sprite gallery.** One card per entry: a real preview of the assigned sprite, editable Name / Type (Background or Cursor) / Sprite fields, and a Remove button. Shows every sprite from every group by default, so you can see your whole library at a glance.
   - **Search** filters the gallery by group name or entry name (case-insensitive) across every group, regardless of which group is selected in the tree.
   - Sprites picked from a `Resources` folder are rejected, since generated style references shouldn't pull assets in through Resources.
3. Click **Generate** (on the settings page, in the window, or **Tools > Leo's Tools > Generate Sprite Library**). This writes the group USS/theme files and then runs the Style Classes generator over them, so the same `{Prefix}{Name}.{Field}` pattern shown below applies to sprite classes too.

## Example

```csharp

public class StyleExample : MonoBehaviour
{
    void Start()
    {
        // Apply a USS class via generated code
        var buttonStyle = new Button();
        buttonStyle.AddToClassList(Style_Buttons.Primary);
    }
}
```
