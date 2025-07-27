using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LeosTools.Editor
{
    internal sealed class SpriteLibrarySettingsProvider : SettingsProvider
    {
        private SpriteLibrarySettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) :
            base(path, scopes, keywords) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);

            var settings = SpriteLibrarySettings.instance;

            rootElement.Add(Utility.CreateTitle("Sprite Library"));

            var targetRow = Utility.CreateBrowseField("Target Folder", settings.TargetFolder,
                "Where the generated class will be created at",
                "Folder to create the generated style file",
                result => settings.TargetFolder = result);

            rootElement.Add(targetRow);

            var generateTheme = new Toggle("Generate Theme")
            {
                tooltip = "Generate a Theme style sheet for the Sprite Library.",
                value = settings.GenerateTheme
            };

            generateTheme.RegisterValueChangedCallback(evt => { settings.GenerateTheme = evt.newValue; });

            rootElement.Add(generateTheme);

            var nameField = Utility.CreateTextField("Theme Name", settings.ThemeName, "Name of the Theme class");

            var field = nameField.Q<TextField>();

            field?.RegisterCallback<FocusOutEvent>(_ =>
            {
                var name = Utility.SanitizeName(field.value);

                if (name is null)
                {
                    field.value = settings.ThemeName;
                    return;
                }

                settings.ThemeName = name;
            });

            rootElement.Add(nameField);

            var prefixField = Utility.CreateTextField("Sprite Prefix", settings.Prefix,
                "The prefix assigned to each sprite class");

            var fieldPrefix = prefixField.Q<TextField>();

            fieldPrefix?.RegisterCallback<FocusOutEvent>(_ =>
            {
                var name = Utility.SanitizeClassName(fieldPrefix.value);

                if (name is null)
                {
                    fieldPrefix.value = settings.Prefix;
                    return;
                }

                settings.Prefix = name;
            });

            rootElement.Add(prefixField);

            var libraryButton = Utility.CreateButton("Open Sprite Library",
                () => ScriptableObject.CreateInstance<SpriteLibraryWindow>().Show());

            rootElement.Add(libraryButton);

            var generateButton =
                Utility.CreateButton("Generate", SpriteLibraryGenerator.Generate, "Generate Sprite Library");

            rootElement.Add(generateButton);
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SpriteLibrarySettingsProvider("Leo's Tools/Sprite Library", SettingsScope.Project);
        }
    }
}