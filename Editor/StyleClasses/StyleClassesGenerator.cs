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
