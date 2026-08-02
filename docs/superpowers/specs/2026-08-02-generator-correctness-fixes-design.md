# UnityStyleGenerator: correctness fixes for selector parsing and generation

## Problem

A review of the package (USS→C# style class generator, sprite library→USS/theme generator) found several bugs where generation silently produces incorrect or non-compiling output, plus one README example that doesn't compile at all. This spec covers fixing those, not adding new capabilities.

## 1. Selector tokenizer replaces the fragile regex

**Current bug:** `StyleClassesGenerator` extracts class names with:

```csharp
private static readonly Regex StyleRegex = new(@"\.([\w-]+)\s*\{", RegexOptions.Compiled);
```

This only matches a class name that is immediately followed by optional whitespace then `{`. It silently misses:

- Pseudo-classes: `.foo:hover {` — `foo` is never captured.
- Compound/combinator selectors: `.foo.bar {`, `.foo > .bar {` — the first class in the chain is dropped.
- Comma-separated selector groups: `.a, .b {` — `a` is dropped (only `b` matches, since it's immediately before `{`).
- Selectors inside `/* ... */` comments — matched and emitted as if live.

No warning or error is raised in any of these cases — the class is just absent from the generated file.

**Fix:** add a tokenizer to `Utility` (or a new `Utility.ExtractClassNames(string ussContent)`):

1. Strip `/* ... */` comments from the file content.
2. Split content into rule blocks on `{`, taking the selector text before each `{`.
3. Split each selector block on `,` to get individual selectors.
4. Within each individual selector, extract every `.class` token via `\.([\w-]+)` (no longer anchored to `{`), which now matches classes followed by pseudo-classes, combinators, or other classes.
5. `.Distinct()` the results as today.

Stays plain C# with `System.Text.RegularExpressions`, no new dependency, no real USS grammar/parser needed since the tool only needs class name extraction.

## 2. Collision & empty-name guards

Two categories of silent-failure exist today because generated identifiers are never checked for uniqueness or validity:

- **Per-class field collision**: two USS classes that sanitize to the same C# identifier (e.g. `foo-bar` and `foo_bar` both → `FooBar`) emit two `public static StyleEntry FooBar = ...` lines in one class → compile error in the generated file.
- **File-level class collision**: two USS/TSS files sharing a filename in different folders both emit `public class {prefix}Buttons { ... }` in the same generated output → duplicate class → compile error.
- **Empty sanitize result**: a class name made entirely of symbols/hyphens sanitizes to `""`, emitting `public static StyleEntry  = new("...")` → compile error.
- **Sprite library NRE risk**: `SpriteLibraryGenerator.CreateSpriteClass` / `VariableForEntry` call `Utility.SanitizeName(...).ToLower()` directly on a nullable return — a null result throws `NullReferenceException` instead of failing cleanly.

**Fix:** track emitted identifiers in a `HashSet<string>` during generation (one set for per-class field names scoped to the current class, one for file-level class names scoped to the whole generation run). Before emitting any generated identifier:

- If null/empty → `Debug.LogWarning` naming the source file and raw (pre-sanitize) name, skip it.
- If already present in the set → `Debug.LogWarning` naming the collision (both source names), skip it.
- Otherwise add to the set and emit normally.

Apply the same null/empty check to the sprite library's `SanitizeName` call sites before `.ToLower()`, closing the NRE.

Generation always completes and always produces compilable output; problems are surfaced via `Debug.LogWarning`, never silently dropped or fatal.

## 3. Small correctness fixes

- **`StyleEntry` has no conversion to `string`.** The README's usage example — `buttonStyle.AddToClassList(Style_Buttons.Primary)` — does not compile today, since `AddToClassList(string)` cannot accept a `StyleEntry`. This predates the current commit; it's been broken since `StyleEntry` was introduced as an object rather than a plain string. Fix: add `public static implicit operator string(StyleEntry e) => e.Class;` to `Runtime/StyleEntry.cs`. Zero call-site changes needed elsewhere; the README example starts working as written.
- **`SpriteLibraryGenerator.StringFromStyleType` switch expression has no default arm.** Adding a third `SpriteStyleType` value compiles fine but throws an unhelpful `SwitchExpressionException` at runtime. Fix: add a default arm throwing `ArgumentOutOfRangeException` naming the unhandled `SpriteStyleType` value.

## 4. Testing

New `Tests/` folder with one EditMode assembly (`LeosTools.Editor.StyleGenerator.Tests`, editor-only, references the generator/utility assembly and Unity Test Framework — bundled with the Unity Editor, no new package dependency). One test class, plain NUnit `[Test]` methods, no fixtures/mocks/AssetDatabase dependency:

- Tokenizer: pseudo-class selector, compound/chained selector, comma-separated group, class inside a comment (ignored), plain selector (still works).
- `SanitizeName`: existing hyphen/space/capitalize-first behavior (pinning down current contract), and the new empty-result case (symbols-only input).
- Collision guard: two names that sanitize identically → second is skipped, not duplicated.

Generation code paths that depend on `AssetDatabase`/real project assets are not covered — not worth mocking for this scope.

## Out of scope

- `StyleSettings` and `SpriteLibrarySettings` share near-identical `SaveDirty()` boilerplate. Cosmetic duplication, not a bug — not touching it.
- No new features (incremental/watch generation, richer selector support beyond class names, validation UI). This spec is bug fixes only, per scoping decision.
