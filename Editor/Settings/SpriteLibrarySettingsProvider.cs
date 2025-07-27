using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityStyleGenerator.Editor.SpriteLibrary;

namespace UnityStyleGenerator.Editor.Settings
{
    internal sealed class SpriteLibrarySettingsProvider : SettingsProvider
    {
        public SpriteLibrarySettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) :
            base(path, scopes, keywords) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);

            var settings = SpriteLibrarySettings.instance;

            rootElement.Add(Utility.CreateTitle("Sprite Library"));

            var targetRow = Utility.CreateRowField("Target Folder", settings.TargetFolder,
                "Where the generated class will be created at",
                "Folder to create the generated style file",
                result => settings.TargetFolder = result);

            rootElement.Add(targetRow);

            var generateTheme = new Toggle("Generate Theme")
            {
                tooltip = "Generate a Theme style sheet for the Sprite Library.",
                value = settings.GenerateTheme
            };

            generateTheme.RegisterValueChangedCallback((evt) => { settings.GenerateTheme = evt.newValue; });

            rootElement.Add(generateTheme);

            var libraryButton = new Button(() => ScriptableObject.CreateInstance<SpriteLibraryWindow>().Show())
            {
                text = "Open Sprite Library"
            };
            
            rootElement.Add(libraryButton);
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SpriteLibrarySettingsProvider("Leo's Tools/Sprite Library", SettingsScope.Project);
        }
    }
}