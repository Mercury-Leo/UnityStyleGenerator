#nullable enable
using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityStyleGenerator.Editor.Settings;

namespace UnityStyleGenerator.Editor.StyleClasses
{
    public static class StyleClassesGenerator
    {
        private const string UssClassEnding = ".uss";
        private const string StyleClassesName = "StyleClasses.cs";
        private const string Prefix = "Style_";
        private static readonly Regex StyleRegex = new(@"\.([\w-]+)\s*\{", RegexOptions.Compiled);

        [InitializeOnLoadMethod]
        private static void InitializeEditorEvents()
        {
            StyleSettings.instance.TargetChanged += OnTargetChanged;
            StyleSettings.instance.SourceChanged += OnSourceChanged;
        }

        private static void OnTargetChanged(string oldFolder, string newFolder)
        {
            DeleteStyleClasses(Path.Combine(oldFolder, StyleClassesName));
            Generate(StyleSettings.instance.SourceFolder, Path.Combine(newFolder, StyleClassesName));
        }

        private static void OnSourceChanged(string oldFolder, string newFolder)
        {
            var targetFile = Path.Combine(StyleSettings.instance.TargetFolder, StyleClassesName);
            DeleteStyleClasses(targetFile);
            Generate(newFolder, targetFile);
        }

        private static void DeleteStyleClasses(string targetFile)
        {
            try
            {
                if (File.Exists(targetFile))
                {
                    AssetDatabase.DeleteAsset(targetFile);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete style classes: {e}");
            }
        }

        [MenuItem("Tools/UIToolkit/Generate Styles")]
        public static void Generate()
        {
            Generate(StyleSettings.instance.SourceFolder,
                Path.Combine(StyleSettings.instance.TargetFolder, StyleClassesName));
        }

        public static void Generate(string sourcePath, string outputPath)
        {
            var writer = new StringWriter();
            var builder = new IndentedTextWriter(writer);

            var guids = AssetDatabase.FindAssets($"t: {nameof(StyleSheet)} t: {nameof(ThemeStyleSheet)}",
                new[] { sourcePath });

            var files = guids.Select(AssetDatabase.GUIDToAssetPath).Where(file => file.EndsWith(UssClassEnding));

            builder.WriteLine("//Auto-Generated. Don't modify this file!");

            builder.WriteLine();

            foreach (var file in files)
            {
                var className = Utility.SanitizeName(Path.GetFileNameWithoutExtension(file));

                if (string.IsNullOrEmpty(className))
                {
                    continue;
                }

                var fileContent = File.ReadAllText(file);
                var styles = StyleRegex.Matches(fileContent).Select(match => match.Groups[1].Value).Distinct();

                builder.WriteLine($"public static class {Prefix}{className}");
                builder.WriteLine("{");
                builder.Indent++;

                foreach (var style in styles)
                {
                    builder.WriteLine($"public const string {Utility.SanitizeName(style)} = \"{style}\";");
                }

                builder.Indent--;
                builder.WriteLine("}");

                builder.WriteLine();
            }

            Utility.TryCreateFile(outputPath, writer.ToString());
        }
    }
}