using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable
namespace UnityStyleGenerator.Editor
{
    internal static class Utility
    {
        public static VisualElement CreateRowField(string title, string initialValue, string titleTooltip,
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

        public static VisualElement CreateTitle(string title)
        {
            return new Label(title)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 18,
                    marginBottom = 10
                }
            };
        }

        public static string? SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            var builder = new StringBuilder();
            foreach (var c in name)
            {
                builder.Append(char.IsLetterOrDigit(c) ? c : string.Empty);
            }

            if (char.IsDigit(builder[0]))
            {
                builder.Insert(0, '_');
            }

            return builder.ToString();
        }

        public static bool TryCreateFile(string path, string content)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, content, Encoding.UTF8);
                AssetDatabase.ImportAsset(path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create {path}: {e}");
                return false;
            }
        }
    }
}