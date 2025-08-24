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

        private static void CreateSpriteClass(LibraryEntry entry, string groupName, StringBuilder builder)
        {
            string? className = Utility.SanitizeName(entry.Name);
            builder.AppendLine($".{groupName.ToLower()}-{className.ToLower()} {{");
            var styleField = entry.Type is SpriteStyleType.Background ? "background-image" : "cursor";
            builder.AppendLine($"\t{styleField}: url(\"{GetProjectDatabaseUrl(entry.Sprite)}\");");
            builder.AppendLine("}");
            builder.AppendLine();
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