#nullable enable
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine.UIElements;

namespace LeosTools.Editor
{
    public static class StyleClassesGenerator
    {
        private static readonly StyleSettings Settings = StyleSettings.instance;
        private static readonly Regex StyleRegex = new(@"\.([\w-]+)\s*\{", RegexOptions.Compiled);
        private const string StyleClassesName = "StyleClasses.g.cs";

        [MenuItem("Tools/Leo's Tools/Generate Styles")]
        public static void Generate()
        {
            Generate(Settings.SourceFolder, Path.Combine(Settings.TargetFolder, StyleClassesName), Settings.Prefix,
                true);
        }

        /// <summary>
        /// Generates style classes files
        /// </summary>
        /// <param name="sourcePath">The USS folder</param>
        /// <param name="outputPath">The generated files output</param>
        /// <param name="prefix">Prefix to add to Style classes names</param>
        /// <param name="generateStyleEntry"></param>
        public static void Generate(string sourcePath, string outputPath, string prefix,
            bool generateStyleEntry = false)
        {
            var writer = new StringWriter();
            var builder = new IndentedTextWriter(writer);

            var guids = AssetDatabase.FindAssets($"t: {nameof(StyleSheet)} t: {nameof(ThemeStyleSheet)}",
                new[] { sourcePath });

            var files = guids.Select(AssetDatabase.GUIDToAssetPath)
                .Where(file => file.EndsWith(Utility.UssClassEnding));

            builder.WriteLine("//Auto-Generated. Don't modify this file!");

            builder.WriteLine();

            if (generateStyleEntry)
            {
                builder.WriteLine("public record StyleEntry\n{\n    public string Class;\n}");

                builder.WriteLine();
            }

            foreach (var file in files)
            {
                var className = Utility.SanitizeName(Path.GetFileNameWithoutExtension(file));

                if (string.IsNullOrEmpty(className))
                {
                    continue;
                }

                var fileContent = File.ReadAllText(file);
                var styles = StyleRegex.Matches(fileContent).Select(match => match.Groups[1].Value).Distinct();

                builder.WriteLine($"public class {prefix}{className}");
                builder.WriteLine("{");
                builder.Indent++;

                foreach (var style in styles)
                {
                    builder.WriteLine(
                        $"public static StyleEntry {Utility.SanitizeName(style, false, true)} = new()  {{ Class = \"{style}\" }};");
                }

                builder.Indent--;
                builder.WriteLine("}");

                builder.WriteLine();
            }

            Utility.TryCreateFile(outputPath, writer.ToString());
        }
    }
}