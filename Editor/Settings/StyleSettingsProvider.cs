using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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

            var title = new Label("Style Generator")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 18,
                    marginBottom = 10
                }
            };

            rootElement.Add(title);

            var targetRow = CreateRowField("Target Folder", settings.TargetFolder,
                "Where the generated class will be created at",
                "Folder to create the generated style file",
                result => settings.TargetFolder = result);

            var ussRow = CreateRowField("USS Folder", settings.SourceFolder,
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

        private VisualElement CreateRowField(string title, string initialValue, string titleTooltip,
            string browseTooltip, Action<string> onFolderSelected)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginTop = 5
                }
            };

            var textField = new TextField(title)
            {
                value = initialValue,
                isReadOnly = true,
                style =
                {
                    flexGrow = 1,
                    marginRight = 5
                },
                tooltip = titleTooltip
            };

            row.Add(textField);

            var browseButton = new Button(() =>
            {
                var selectedFolder =
                    EditorUtility.OpenFolderPanel("Select folder", Application.dataPath, string.Empty);

                if (string.IsNullOrEmpty(selectedFolder))
                {
                    return;
                }

                if (!selectedFolder.StartsWith(Application.dataPath))
                {
                    EditorUtility.DisplayDialog("Invalid Folder",
                        "Please pick a folder inside the project's Assets directory.", "OK");
                    return;
                }

                var result = "Assets" + selectedFolder[Application.dataPath.Length..];
                onFolderSelected?.Invoke(result);
                textField.value = result;
            })
            {
                text = "Browse",
                style =
                {
                    width = 80,
                },
                tooltip = browseTooltip
            };

            row.Add(browseButton);

            return row;
        }
    }
}