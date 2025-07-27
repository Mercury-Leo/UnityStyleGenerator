using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityStyleGenerator.Editor.StyleClasses;

#nullable enable
namespace UnityStyleGenerator.Editor.Settings
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

            var targetRow = Utility.CreateRowField("Target Folder", settings.TargetFolder,
                "Where the generated class will be created at",
                "Folder to create the generated style file",
                result => settings.TargetFolder = result);

            var ussRow = Utility.CreateRowField("USS Folder", settings.SourceFolder,
                "Will create the folder's USS files's style classes",
                "Folder to find USS files to generate from",
                result => settings.SourceFolder = result);

            rootElement.Add(targetRow);
            rootElement.Add(ussRow);

            var generateButton = new Button(StyleClassesGenerator.Generate)
            {
                text = "Generate",
                style =
                {
                    marginTop = 20,
                    marginLeft = new Length(30, LengthUnit.Percent),
                    marginRight = new Length(30, LengthUnit.Percent),
                },
                tooltip = "Generate Style Classes"
            };

            rootElement.Add(generateButton);
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new StyleSettingsProvider("Leo's Tools/Style Generator", SettingsScope.Project);
        }
    }
}