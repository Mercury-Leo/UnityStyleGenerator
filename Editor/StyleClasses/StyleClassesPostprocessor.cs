#nullable enable
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

            bool ChangedInSource(string path) =>
                (path.EndsWith(Utility.UssClassEnding) || path.EndsWith(Utility.ThemeClassEnding)) &&
                path.StartsWith(sourceFolder);

            if (importedAssets.Any(ChangedInSource) || deletedAssets.Any(ChangedInSource) ||
                movedAssets.Any(ChangedInSource))
            {
                StyleClassesGenerator.Generate();
            }
        }
    }
}
