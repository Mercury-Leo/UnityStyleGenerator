#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace LeosTools.Editor
{
    internal sealed class StyleSettingsProvider : SettingsProvider
    {
        private StyleSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(
            path, scopes, keywords) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);

            var settings = StyleSettings.instance;

            rootElement.Add(Utility.CreateTitle("Style Generator"));

            var targetRow = Utility.CreateBrowseField("Target Folder", settings.TargetFolder,
                "Where the generated class will be created at",
                "Folder to create the generated style file",
                result => settings.TargetFolder = result);

            var ussRow = Utility.CreateBrowseField("USS Folder", settings.SourceFolder,
                "Will create the folder's USS files's style classes",
                "Folder to find USS files to generate from",
                result => settings.SourceFolder = result);

            rootElement.Add(targetRow);
            rootElement.Add(ussRow);

            var prefixField = Utility.CreateTextField("Style Prefix", settings.Prefix,
                "The prefix assigned to each style class");

            var field = prefixField.Q<TextField>();

            field?.RegisterCallback<FocusOutEvent>(_ =>
            {
                var name = Utility.SanitizeClassName(field.value);

                if (name is null)
                {
                    field.value = settings.Prefix;
                    return;
                }

                settings.Prefix = name;
            });

            rootElement.Add(prefixField);

            var generateButton =
                Utility.CreateButton("Generate", StyleClassesGenerator.Generate, "Generate Style Classes");

            rootElement.Add(generateButton);
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new StyleSettingsProvider("Leo's Tools/Style Generator", SettingsScope.Project);
        }
    }
}