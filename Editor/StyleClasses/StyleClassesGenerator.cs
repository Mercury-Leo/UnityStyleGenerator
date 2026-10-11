#nullable enable
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LeosTools.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace StyleGenerator.Editor
{
    public static class StyleClassesGenerator
    {
        private static readonly StyleSettings Settings = StyleSettings.instance;
        private static readonly Regex CommentRegex = new(@"/\*.*?\*/", RegexOptions.Compiled | RegexOptions.Singleline);

        // A selector prelude: the text after a '}' or ';' (or the file start) up to the next '{'.
        // ';' keeps @import url("x.uss"); out of the next rule's prelude.
        private static readonly Regex PreludeRegex = new(@"([^{};]*)\{", RegexOptions.Compiled);
        private static readonly Regex ClassRegex = new(@"\.(-?[_a-zA-Z][\w-]*)", RegexOptions.Compiled);
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

            foreach (var file in files)
            {
                var className = Utility.SanitizeName(Path.GetFileNameWithoutExtension(file));

                if (string.IsNullOrEmpty(className))
                {
                    continue;
                }

                builder.WriteLine($"public class {prefix}{className}");
                builder.WriteLine("{");
                builder.Indent++;

                var names = new HashSet<string>();

                foreach (var style in GetClasses(File.ReadAllText(file)))
                {
                    var name = Utility.SanitizeName(style, false, true)!;

                    if (!names.Add(name))
                    {
                        Debug.LogWarning($"{file}: '.{style}' maps to the existing member {name}, skipped.");
                        continue;
                    }

                    builder.WriteLine($"public static StyleEntry {name} = new(\"{style}\");");
                }

                builder.Indent--;
                builder.WriteLine("}");

                builder.WriteLine();
            }

            Utility.TryCreateFile(outputPath, writer.ToString());
        }

        /// <summary>
        /// Every class used in a selector of the given USS, distinct, in first-appearance order.
        /// Comments and declaration blocks are never read.
        /// </summary>
        public static IEnumerable<string> GetClasses(string uss)
        {
            return PreludeRegex.Matches(CommentRegex.Replace(uss, " "))
                .SelectMany(prelude => ClassRegex.Matches(prelude.Groups[1].Value))
                .Select(match => match.Groups[1].Value)
                .Distinct();
        }
    }
}