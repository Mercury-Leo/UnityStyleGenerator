#nullable enable
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

            builder.AppendLine('\n');

            foreach (var entry in entries)
            {
                if (!entry.IsValid())
                {
                    continue;
                }

                CreateSpriteClass(entry, builder);
            }

            string assetPath = Path.Combine(targetFolder, groupName + Utility.UssClassEnding);

            Utility.TryCreateFile(assetPath, builder.ToString());
            return assetPath;
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

                var variableName = $"--sprite-library-{groupName.ToLower()}-{StringFromStyleType(entry.Type)}-{Utility.SanitizeName(entry.Name).ToLower()}";
                builder.AppendLine($"\t{variableName}: url(\"{GetProjectDatabaseUrl(entry.Sprite)}\");");
            }
            builder.AppendLine("}");
        }

        private static void CreateSpriteClass(LibraryEntry entry, StringBuilder builder)
        {
            string? className = Utility.SanitizeName(entry.Name);
            builder.AppendLine($".{className} {{");
            var styleField = StringFromStyleType(entry.Type);
            builder.AppendLine($"{styleField}: url(\"{GetProjectDatabaseUrl(entry.Sprite)}\");");
            builder.AppendLine("}");
        }

        private static string StringFromStyleType(SpriteStyleType type)
        {
            return type switch
            {
                SpriteStyleType.Background => "background",
                SpriteStyleType.Cursor => "cursor",
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