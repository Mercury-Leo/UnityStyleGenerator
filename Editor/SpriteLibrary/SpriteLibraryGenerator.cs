using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityStyleGenerator.Editor.Settings;

#nullable enable
namespace UnityStyleGenerator.Editor.SpriteLibrary
{
    public static class SpriteLibraryGenerator
    {
        private static readonly SpriteLibrarySettings Settings = SpriteLibrarySettings.instance;
        private const string ThemeClassName = "SpriteLibraryTheme.tss";

        [MenuItem("Tools/UIToolkit/Generate Styles")]
        public static void Generate()
        {
            Generate(Settings.TargetFolder);
        }

        public static void Generate(string targetFolder)
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
                BuildTheme(targetFolder, groups);
            }
        }

        private static void BuildTheme(string targetFolder, IEnumerable<string> groups)
        {
            var themeBuilder = new StringBuilder().AppendLine();

            foreach (var group in groups)
            {
                themeBuilder.AppendLine($"@import url(\"/{group}.uss\");");
            }

            var outputPath = Path.Combine(targetFolder, ThemeClassName);

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

            string assetPath = Path.Combine(targetFolder, groupName + ".uss");

            string? directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(assetPath, builder.ToString());
            AssetDatabase.ImportAsset(assetPath);
            return assetPath;
        }

        private static void CreateSpriteClass(LibraryEntry entry, StringBuilder builder)
        {
            string? className = Utility.SanitizeName(entry.Name);
            builder.AppendLine($".{className} {{");
            var styleField = entry.Type is SpriteStyleType.Background ? "background-image" : "cursor";
            builder.AppendLine($"{styleField}: url(\"{GetProjectDatabaseUrl(entry.Sprite)}\");");
            builder.AppendLine("}");
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

            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
            return $"project://database/{path}?fileID={fileId}&guid={guid}&type={typeCode}#{fileNameWithoutExt}";
        }
    }
}