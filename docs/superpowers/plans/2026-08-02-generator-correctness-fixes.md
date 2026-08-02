# Generator Correctness Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the selector-parsing gaps, silent duplicate/empty-identifier failures, the broken `StyleEntry`→string README example, and the non-exhaustive sprite-style switch in the UnityStyleGenerator package, backed by minimal EditMode tests.

**Architecture:** Two small additions to the existing internal `Utility` class (a comment-aware selector tokenizer, and a generic duplicate/empty-name guard) get consumed by `StyleClassesGenerator`, replacing the fragile regex and adding warn-and-skip behavior. `SpriteLibraryGenerator` gets the same null-guard treatment at its two `SanitizeName` call sites plus an exhaustive switch. `StyleEntry` gets a one-line implicit string conversion. A new EditMode test assembly pins down the new/changed logic.

**Tech Stack:** C# / Unity 6000.4.6f1, Unity Editor APIs (`UnityEditor`, `UnityEngine.UIElements`), Unity Test Framework (NUnit, bundled with the Editor — no new package dependency).

## Global Constraints

- No new external dependencies. Tests use only the Unity-bundled Test Framework (`UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `nunit.framework.dll`) — already available in this Editor install, no `package.json` change needed.
- The `Runtime/` assembly stays Editor-independent — no `UnityEditor` references added there (relevant to Task 5).
- Generation must never throw and must never silently drop an identifier without a message: invalid or duplicate identifiers are skipped via `Debug.LogWarning` naming the offending value, and generation still completes with compilable output.
- Reference sibling assemblies by GUID in asmdef files, matching the existing convention (`Editor/LeosTools.Editor.StyleGenerator.asmdef` already references `Runtime/LeosTools.Runtime.StyleGenerator.asmdef` as `"GUID:6d5ef153ed33f664b949728deed8130b"`).
- Package repo is `Assets/UnityStyleGenerator` (its own git repo, remote `origin` = `https://github.com/Mercury-Leo/UnityStyleGenerator.git`, branch `main`, currently clean). All paths below are relative to that repo root.

---

### Task 1: Selector tokenizer (`Utility.ExtractClassNames`) + EditMode test assembly

**Files:**
- Create: `Editor/AssemblyInfo.cs`
- Create: `Tests/LeosTools.Editor.StyleGenerator.Tests.asmdef`
- Create: `Tests/SelectorTokenizerTests.cs`
- Modify: `Editor/Utility.cs`

**Interfaces:**
- Produces: `internal static IEnumerable<string> Utility.ExtractClassNames(string ussContent)` in namespace `LeosTools.Editor` — consumed by Task 3.

- [ ] **Step 1: Make the internal `Utility` class visible to the new test assembly**

Create `Editor/AssemblyInfo.cs`:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LeosTools.Editor.StyleGenerator.Tests")]
```

- [ ] **Step 2: Create the EditMode test assembly definition**

Create `Tests/LeosTools.Editor.StyleGenerator.Tests.asmdef`. The GUID reference is `LeosTools.Editor.StyleGenerator`'s own asmdef GUID (confirmed from `Editor/LeosTools.Editor.StyleGenerator.asmdef.meta`):

```json
{
    "name": "LeosTools.Editor.StyleGenerator.Tests",
    "rootNamespace": "",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "GUID:fc03d92a8fb11ac4280a538f06f4b7b5"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: Write the failing tests**

Create `Tests/SelectorTokenizerTests.cs`:

```csharp
using System.Linq;
using LeosTools.Editor;
using NUnit.Framework;

namespace StyleGenerator.Editor.Tests
{
    public class SelectorTokenizerTests
    {
        [Test]
        public void ExtractClassNames_PlainSelector_ReturnsClass()
        {
            var result = Utility.ExtractClassNames(".plain { color: red; }");
            Assert.That(result, Does.Contain("plain"));
        }

        [Test]
        public void ExtractClassNames_PseudoClassSelector_ReturnsClass()
        {
            var result = Utility.ExtractClassNames(".foo:hover { color: red; }");
            Assert.That(result, Does.Contain("foo"));
        }

        [Test]
        public void ExtractClassNames_CompoundSelector_ReturnsBothClasses()
        {
            var result = Utility.ExtractClassNames(".foo.bar { color: red; }").ToList();
            Assert.That(result, Does.Contain("foo"));
            Assert.That(result, Does.Contain("bar"));
        }

        [Test]
        public void ExtractClassNames_CommaSeparatedGroup_ReturnsBothClasses()
        {
            var result = Utility.ExtractClassNames(".a, .b { color: red; }").ToList();
            Assert.That(result, Does.Contain("a"));
            Assert.That(result, Does.Contain("b"));
        }

        [Test]
        public void ExtractClassNames_ClassInsideComment_IsIgnored()
        {
            var result = Utility
                .ExtractClassNames("/* .commented { color: red; } */ .live { color: blue; }")
                .ToList();
            Assert.That(result, Does.Not.Contain("commented"));
            Assert.That(result, Does.Contain("live"));
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they fail**

In the Unity Editor (project `Ulink`), open **Window > General > Test Runner > EditMode**, let the project finish compiling, then run `SelectorTokenizerTests`.
Expected: compile error — `'Utility' does not contain a definition for 'ExtractClassNames'`.

- [ ] **Step 5: Implement the tokenizer**

In `Editor/Utility.cs`, add to the `using` block at the top:

```csharp
using System.Collections.Generic;
using System.Text.RegularExpressions;
```

Add these two fields near the existing constants (after `ThemeClassEnding`):

```csharp
private static readonly Regex CommentRegex = new(@"/\*.*?\*/", RegexOptions.Compiled | RegexOptions.Singleline);
private static readonly Regex ClassTokenRegex = new(@"\.([\w-]+)", RegexOptions.Compiled);
```

Add the method (anywhere inside the `Utility` class, e.g. right before `SanitizeName`):

```csharp
internal static IEnumerable<string> ExtractClassNames(string ussContent)
{
    var withoutComments = CommentRegex.Replace(ussContent, string.Empty);
    var classNames = new List<string>();

    foreach (var ruleBlock in withoutComments.Split('{'))
    {
        var selectorText = ruleBlock;
        var lastBrace = selectorText.LastIndexOf('}');

        if (lastBrace >= 0)
        {
            selectorText = selectorText[(lastBrace + 1)..];
        }

        foreach (var selector in selectorText.Split(','))
        {
            foreach (Match match in ClassTokenRegex.Matches(selector))
            {
                classNames.Add(match.Groups[1].Value);
            }
        }
    }

    return classNames.Distinct();
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run `SelectorTokenizerTests` again in Test Runner.
Expected: all 5 tests PASS.

- [ ] **Step 7: Commit**

```bash
git add Editor/AssemblyInfo.cs Tests/LeosTools.Editor.StyleGenerator.Tests.asmdef Tests/SelectorTokenizerTests.cs Editor/Utility.cs
git commit -m "feat: add comment/pseudo-class-aware selector tokenizer"
```

(Unity will generate matching `.meta` files for the new assets on next import — stage those too if `git status` shows them.)

---

### Task 2: Duplicate/empty-name guard (`Utility.TryRegisterIdentifier`)

**Files:**
- Create: `Tests/SanitizeNameTests.cs`
- Create: `Tests/IdentifierRegistryTests.cs`
- Modify: `Editor/Utility.cs`

**Interfaces:**
- Consumes: nothing new.
- Produces: `internal static bool Utility.TryRegisterIdentifier(HashSet<string> seenIdentifiers, string? sanitizedName, string rawName, string context)` in namespace `LeosTools.Editor` — consumed by Task 3.

- [ ] **Step 1: Pin down existing `SanitizeName` behavior (characterization tests)**

Create `Tests/SanitizeNameTests.cs`. These document already-correct, already-existing behavior — no implementation change in this step.

```csharp
using LeosTools.Editor;
using NUnit.Framework;

namespace StyleGenerator.Editor.Tests
{
    public class SanitizeNameTests
    {
        [Test]
        public void SanitizeName_HyphensAndSpaces_CamelCasesWithoutLeadingCapital()
        {
            Assert.That(Utility.SanitizeName("foo-bar baz"), Is.EqualTo("fooBarBaz"));
        }

        [Test]
        public void SanitizeName_CapitalizeFirst_CapitalizesLeadingChar()
        {
            Assert.That(Utility.SanitizeName("foo-bar baz", capitalizeFirst: true), Is.EqualTo("FooBarBaz"));
        }

        [Test]
        public void SanitizeName_KeepHyphens_PreservesThemLiterally()
        {
            Assert.That(Utility.SanitizeName("foo-bar", keepHyphens: true), Is.EqualTo("foo-bar"));
        }

        [Test]
        public void SanitizeName_SymbolsOnly_ReturnsEmptyNotNull()
        {
            Assert.That(Utility.SanitizeName("!!!", keepHyphens: true), Is.Empty);
        }
    }
}
```

- [ ] **Step 2: Run these tests**

Run `SanitizeNameTests` in Test Runner.
Expected: all 4 tests PASS immediately (existing behavior, nothing to implement).

- [ ] **Step 3: Write the failing tests for the new guard**

Create `Tests/IdentifierRegistryTests.cs`:

```csharp
using System.Collections.Generic;
using LeosTools.Editor;
using NUnit.Framework;

namespace StyleGenerator.Editor.Tests
{
    public class IdentifierRegistryTests
    {
        [Test]
        public void TryRegisterIdentifier_NewName_ReturnsTrue()
        {
            var seen = new HashSet<string>();
            Assert.That(Utility.TryRegisterIdentifier(seen, "Foo", "foo", "test"), Is.True);
        }

        [Test]
        public void TryRegisterIdentifier_DuplicateName_ReturnsFalseAndKeepsFirstOnly()
        {
            var seen = new HashSet<string>();
            Utility.TryRegisterIdentifier(seen, "Foo", "foo", "test");

            Assert.That(Utility.TryRegisterIdentifier(seen, "Foo", "foo-again", "test"), Is.False);
            Assert.That(seen, Has.Count.EqualTo(1));
        }

        [Test]
        public void TryRegisterIdentifier_NullOrEmptyName_ReturnsFalse()
        {
            var seen = new HashSet<string>();
            Assert.That(Utility.TryRegisterIdentifier(seen, null, "raw", "test"), Is.False);
            Assert.That(Utility.TryRegisterIdentifier(seen, string.Empty, "raw", "test"), Is.False);
            Assert.That(seen, Is.Empty);
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they fail**

Run `IdentifierRegistryTests` in Test Runner.
Expected: compile error — `'Utility' does not contain a definition for 'TryRegisterIdentifier'`.

- [ ] **Step 5: Implement the guard**

In `Editor/Utility.cs`, add (e.g. right after `SanitizeName`, before `TryCreateFile`):

```csharp
internal static bool TryRegisterIdentifier(HashSet<string> seenIdentifiers, string? sanitizedName, string rawName, string context)
{
    if (string.IsNullOrEmpty(sanitizedName))
    {
        Debug.LogWarning($"[StyleGenerator] Skipping '{rawName}' ({context}): sanitized name is empty.");
        return false;
    }

    if (!seenIdentifiers.Add(sanitizedName))
    {
        Debug.LogWarning($"[StyleGenerator] Skipping '{rawName}' ({context}): duplicate identifier '{sanitizedName}'.");
        return false;
    }

    return true;
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run `IdentifierRegistryTests` in Test Runner.
Expected: all 3 tests PASS.

- [ ] **Step 7: Commit**

```bash
git add Editor/Utility.cs Tests/SanitizeNameTests.cs Tests/IdentifierRegistryTests.cs
git commit -m "feat: add duplicate/empty identifier guard"
```

---

### Task 3: Wire the tokenizer and guard into `StyleClassesGenerator`

**Files:**
- Modify: `Editor/StyleClasses/StyleClassesGenerator.cs`

**Interfaces:**
- Consumes: `Utility.ExtractClassNames(string)` (Task 1), `Utility.TryRegisterIdentifier(HashSet<string>, string?, string, string)` (Task 2).
- Produces: no new public surface — `Generate(string, string, string)` keeps its existing signature and menu item.

This task has no new automated tests — per the spec, code paths depending on `AssetDatabase`/real project assets aren't worth mocking. Verification is manual, using the real Editor.

- [ ] **Step 1: Replace the generator body**

Replace the full contents of `Editor/StyleClasses/StyleClassesGenerator.cs` with:

```csharp
#nullable enable
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LeosTools.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace StyleGenerator.Editor
{
    public static class StyleClassesGenerator
    {
        private static readonly StyleSettings Settings = StyleSettings.instance;
        private const string StyleClassesName = "StyleClasses.g.cs";

        [MenuItem("Tools/Leo's Tools/Generate Styles")]
        public static void Generate()
        {
            Generate(Settings.SourceFolder, Path.Combine(Settings.TargetFolder, StyleClassesName), Settings.Prefix);
        }

        /// <summary>
        /// Generates style classes files
        /// </summary>
        /// <param name="sourcePath">The USS folder</param>
        /// <param name="outputPath">The generated files output</param>
        /// <param name="prefix">Prefix to add to Style classes names</param>
        public static void Generate(string sourcePath, string outputPath, string prefix)
        {
            var writer = new StringWriter();
            var builder = new IndentedTextWriter(writer);

            var guids = AssetDatabase.FindAssets($"t: {nameof(StyleSheet)} t: {nameof(ThemeStyleSheet)}",
                new[] { sourcePath });

            var files = guids.Select(AssetDatabase.GUIDToAssetPath)
                .Where(file => file.EndsWith(Utility.UssClassEnding));

            builder.WriteLine("//Auto-Generated. Don't modify this file!");
            builder.WriteLine();
            builder.WriteLine("using LeosTools.Runtime;");
            builder.WriteLine();

            var seenClassNames = new HashSet<string>();

            foreach (var file in files)
            {
                var rawClassName = Path.GetFileNameWithoutExtension(file);
                var className = Utility.SanitizeName(rawClassName);

                if (!Utility.TryRegisterIdentifier(seenClassNames, className, rawClassName, $"file '{file}'"))
                {
                    continue;
                }

                var fileContent = File.ReadAllText(file);
                var styles = Utility.ExtractClassNames(fileContent);

                builder.WriteLine($"public class {prefix}{className}");
                builder.WriteLine("{");
                builder.Indent++;

                var seenFieldNames = new HashSet<string>();

                foreach (var style in styles)
                {
                    var fieldName = Utility.SanitizeName(style, false, true);

                    if (!Utility.TryRegisterIdentifier(seenFieldNames, fieldName, style, $"class '.{style}' in '{file}'"))
                    {
                        continue;
                    }

                    builder.WriteLine($"public static StyleEntry {fieldName} = new(\"{style}\");");
                }

                builder.Indent--;
                builder.WriteLine("}");
                builder.WriteLine();
            }

            Utility.TryCreateFile(outputPath, writer.ToString());
        }
    }
}
```

- [ ] **Step 2: Manually verify the parsing fix**

In the Unity Editor, create a scratch file `Assets/_ScratchStyleTest.uss` containing:

```
/* .ignored-in-comment { color: red; } */
.foo:hover { color: red; }
.foo.bar { color: blue; }
.a, .b { color: green; }
```

Open **Edit > Project Settings > Leo's Tools > Style Generator**, point the USS folder at wherever `_ScratchStyleTest.uss` lives, and click **Generate**.
Expected: the generated `StyleClasses.g.cs` contains `Foo`, `Bar`, `A`, and `B` fields but not `IgnoredInComment`, and compiles with no Console errors.

- [ ] **Step 3: Manually verify the duplicate-file guard**

Duplicate `_ScratchStyleTest.uss` into a second folder under the same source root, keeping the same filename. Click **Generate** again.
Expected: Console shows a `Debug.LogWarning` naming the duplicate file (not a compile error), and the generated file still compiles.

- [ ] **Step 4: Clean up scratch files**

Delete `_ScratchStyleTest.uss` (both copies) and its `.meta` files — they were only for manual verification, not part of the shipped package.

- [ ] **Step 5: Commit**

```bash
git add Editor/StyleClasses/StyleClassesGenerator.cs
git commit -m "fix: use tokenizer and identifier guard in style class generation"
```

---

### Task 4: Fix `SpriteLibraryGenerator` NRE risk and non-exhaustive switch

**Files:**
- Modify: `Editor/SpriteLibrary/SpriteLibraryGenerator.cs`

**Interfaces:** none new — internal behavior change only, `Generate(string, string, string)` signature unchanged.

No new automated tests here either (same `AssetDatabase`/`Sprite` dependency reasoning as Task 3). Verification is manual.

- [ ] **Step 1: Add the missing `using`**

Add `using System;` to the top of `Editor/SpriteLibrary/SpriteLibraryGenerator.cs` (needed for `ArgumentOutOfRangeException`).

- [ ] **Step 2: Guard `VariableForEntry`'s null path**

Replace the existing `VariableForEntry` method with:

```csharp
private static string? VariableForEntry(LibraryEntry entry, string groupName)
{
    var sanitized = Utility.SanitizeName($"{groupName}-{StringFromStyleType(entry.Type)}-{entry.Name}", true)?.ToLower();
    return string.IsNullOrEmpty(sanitized) ? null : $"--sprite-library-{sanitized}";
}
```

- [ ] **Step 3: Guard the call site in `CreateSpriteVariables`**

Replace `CreateSpriteVariables` with:

```csharp
private static void CreateSpriteVariables(IEnumerable<LibraryEntry> entries, string groupName, StringBuilder builder)
{
    builder.AppendLine(":root {");
    foreach (var entry in entries)
    {
        if (!entry.IsValid())
        {
            continue;
        }

        var variable = VariableForEntry(entry, groupName);

        if (string.IsNullOrEmpty(variable))
        {
            Debug.LogWarning($"[SpriteLibrary] Skipping variable for entry '{entry.Name}' in group '{groupName}': sanitized name is empty.");
            continue;
        }

        builder.AppendLine($"\t{variable}: url(\"{GetProjectDatabaseUrl(entry.Sprite)}\");");
    }
    builder.AppendLine("}");
}
```

- [ ] **Step 4: Guard the call site in `CreateSpriteClass`**

Replace `CreateSpriteClass` with:

```csharp
private static void CreateSpriteClass(LibraryEntry entry, string groupName, StringBuilder builder)
{
    var className = Utility.SanitizeName($"{groupName}-{entry.Name}", true)?.ToLower();

    if (string.IsNullOrEmpty(className))
    {
        Debug.LogWarning($"[SpriteLibrary] Skipping entry '{entry.Name}' in group '{groupName}': sanitized class name is empty.");
        return;
    }

    var variable = VariableForEntry(entry, groupName);

    if (string.IsNullOrEmpty(variable))
    {
        Debug.LogWarning($"[SpriteLibrary] Skipping entry '{entry.Name}' in group '{groupName}': sanitized variable name is empty.");
        return;
    }

    builder.AppendLine($".{className} {{");
    builder.AppendLine($"\t{StringFromStyleType(entry.Type)}: var({variable});");
    builder.AppendLine("}");
    builder.AppendLine();
}
```

- [ ] **Step 5: Make `StringFromStyleType` exhaustive**

Replace `StringFromStyleType` with:

```csharp
private static string StringFromStyleType(SpriteStyleType type)
{
    return type switch
    {
        SpriteStyleType.Background => "background-image",
        SpriteStyleType.Cursor => "cursor",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unhandled SpriteStyleType value.")
    };
}
```

- [ ] **Step 6: Manually verify the NRE fix**

Open **Tools > Leo's Tools > Sprite Library**. Add a group with a valid name, add an entry named `!!!` with any `Sprite` assigned (so `LibraryEntry.IsValid()` passes since name is non-empty and sprite is set), click **Generate**.
Expected: Console shows a `Debug.LogWarning` naming the empty sanitized class/variable — no `NullReferenceException`. Remove the test group/entry afterward.

- [ ] **Step 7: Commit**

```bash
git add Editor/SpriteLibrary/SpriteLibraryGenerator.cs
git commit -m "fix: guard null sanitized names and make sprite style switch exhaustive"
```

---

### Task 5: `StyleEntry` implicit conversion to `string`

**Files:**
- Modify: `Runtime/StyleEntry.cs`

**Interfaces:**
- Produces: `public static implicit operator string(StyleEntry entry)` on `LeosTools.Runtime.StyleEntry` — makes any generated `StyleEntry` usable anywhere a `string` is expected (e.g. `VisualElement.AddToClassList(string)`), matching the README's existing usage example.

No new test assembly for `Runtime/` — it's a one-line operator with no branch or loop, and the `Runtime` assembly currently has no test infrastructure of its own; a manual compile check is the smallest sufficient verification here.

- [ ] **Step 1: Add the operator**

Replace the contents of `Runtime/StyleEntry.cs` with:

```csharp
namespace LeosTools.Runtime
{
    /// <summary>
    /// Represents a style class
    /// </summary>
    public class StyleEntry
    {
        public readonly string Class;

        public StyleEntry(string styleClass)
        {
            Class = styleClass;
        }

        public static implicit operator string(StyleEntry entry) => entry.Class;
    }
}
```

- [ ] **Step 2: Manually verify against the README example**

In the Unity Editor, with any generated `StyleClasses.g.cs` present (e.g. from Task 3's manual verification, before cleanup, or generate a fresh one), write a throwaway script containing:

```csharp
var element = new UnityEngine.UIElements.VisualElement();
element.AddToClassList(Style_ScratchStyleTest.Foo);
```

Expected: compiles with no errors (this is the exact shape of the README's usage example). Delete the throwaway script afterward.

- [ ] **Step 3: Commit**

```bash
git add Runtime/StyleEntry.cs
git commit -m "fix: make StyleEntry implicitly convertible to string"
```