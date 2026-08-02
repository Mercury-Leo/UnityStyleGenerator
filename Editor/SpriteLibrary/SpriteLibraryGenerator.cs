#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LeosTools.Editor;
using StyleGenerator.Editor;
using UnityEditor;
using UnityEngine;

namespace SpriteLibrary.Editor
{
    public static class SpriteLibraryGenerator
    {
        private static readonly SpriteLibrarySettings Settings = SpriteLibrarySettings.instance;
        private const string SpriteClassesName = "SpriteClasses.g.cs";

        [MenuItem("Tools/Leo's Tools/Generate Sprite Library")]
        public static void Generate()
        {
            Generate(Settings.TargetFolder, Settings.ThemeName, Settings.Prefix);
        }

        /// <summary>
        /// Generates USS & Theme files based on the content in the Sprite Library
        /// </summary>
        /// <param name="targetFolder">Where to generate the files</param>
        /// <param name="themeName">The name of the generated Theme file</param>
        /// <param name="prefix">Prefix to add to sprite classes</param>
        public static void Generate(string targetFolder, string themeName, string prefix)
        {
            var groups = new List<string>();
            foreach (var group in Settings.Groups)
            {
                if (!group.IsValid())
                {
                    continue;
                }

                var path = CreateSpriteUSS(targetFolder, group.Name, group.Entries);
                groups.Add(path);
            }

            if (Settings.GenerateTheme)
            {
                BuildTheme(targetFolder, themeName, groups);
            }

            StyleClassesGenerator.Generate(targetFolder, Path.Combine(targetFolder, SpriteClassesName), prefix);
        }

        private static void BuildTheme(string targetFolder, string themeName, IEnumerable<string> groups)
        {
            var themeBuilder = new StringBuilder().AppendLine();

            foreach (var groupPath in groups)
            {
                themeBuilder.AppendLine($"@import url(\"/{groupPath.FixSlashes()}\");");
            }

            var outputPath = Path.Combine(targetFolder, themeName + Utility.ThemeClassEnding);

            Utility.TryCreateFile(outputPath, themeBuilder.ToString());
        }

        private static string CreateSpriteUSS(string targetFolder, string groupName, IEnumerable<LibraryEntry> entries)
        {
            var builder = new StringBuilder();

            CreateSpriteVariables(entries, groupName, builder);

            builder.AppendLine();

            foreach (var entry in entries)
            {
                if (!entry.IsValid())
                {
                    continue;
                }

                CreateSpriteClass(entry, groupName, builder);
            }

            string assetPath = Path.Combine(targetFolder, groupName + Utility.UssClassEnding);

            Utility.TryCreateFile(assetPath, builder.ToString());
            return assetPath;
        }

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

        private static string? VariableForEntry(LibraryEntry entry, string groupName)
        {
            var sanitized = Utility.SanitizeName($"{groupName}-{StringFromStyleType(entry.Type)}-{entry.Name}", true)?.ToLower();
            return string.IsNullOrEmpty(sanitized) ? null : $"--sprite-library-{sanitized}";
        }

        private static string StringFromStyleType(SpriteStyleType type)
        {
            return type switch
            {
                SpriteStyleType.Background => "background-image",
                SpriteStyleType.Cursor => "cursor",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unhandled SpriteStyleType value.")
            };
        }

        private static string? GetProjectDatabaseUrl(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long fileId))
            {
                return null;
            }

            const int typeCode = 3;

            string name = asset is Sprite sprite ? sprite.name : Path.GetFileNameWithoutExtension(path);

            return $"project://database/{path}?fileID={fileId}&guid={guid}&type={typeCode}#{name}";
        }
    }
}