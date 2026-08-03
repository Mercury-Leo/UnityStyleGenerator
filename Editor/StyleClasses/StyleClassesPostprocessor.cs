#nullable enable
using System;
using System.Linq;
using LeosTools.Editor;
using UnityEditor;

namespace StyleGenerator.Editor
{
    internal class StyleClassesPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            var settings = StyleSettings.instance;

            if (!settings.AutoRegenerate)
            {
                return;
            }

            var sourceFolder = settings.SourceFolder;

            // Only .uss: StyleClassesGenerator filters its search results down to .uss, so a
            // .tss-only change would regenerate to the exact same output.
            bool ChangedInSource(string path) =>
                path.EndsWith(Utility.UssClassEnding) &&
                path.StartsWith(sourceFolder + "/", StringComparison.Ordinal);

            // movedFromAssetPaths catches files moved *out* of the source folder, whose new path
            // is outside it and whose old path appears nowhere else.
            if (importedAssets.Any(ChangedInSource) || deletedAssets.Any(ChangedInSource) ||
                movedAssets.Any(ChangedInSource) || movedFromAssetPaths.Any(ChangedInSource))
            {
                StyleClassesGenerator.Generate();
            }
        }
    }
}
