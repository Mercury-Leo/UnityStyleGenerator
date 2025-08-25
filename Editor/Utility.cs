#nullable enable
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LeosTools.Editor
{
    internal static class Utility
    {
        public const string ClassEnding = ".cs";
        public const string UssClassEnding = ".uss";
        public const string ThemeClassEnding = ".tss";

        public static VisualElement CreateBrowseField(string title, string initialValue, string titleTooltip,
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

        public static VisualElement CreateButton(string label, Action? clickAction = null, string? tooltip = null)
        {
            return new Button(clickAction)
            {
                text = label,
                style =
                {
                    marginTop = 20,
                    marginLeft = new Length(30, LengthUnit.Percent),
                    marginRight = new Length(30, LengthUnit.Percent),
                },
                tooltip = tooltip
            };
        }

        public static VisualElement CreateTextField(string label, string initialValue, string tooltip)
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

            var textField = new TextField(label)
            {
                value = initialValue,
                style =
                {
                    flexGrow = 1,
                    marginRight = 5
                },
                tooltip = tooltip
            };

            row.Add(textField);

            return row;
        }

        public static string? SanitizeName(string name, bool keepHyphens = false, bool capitalizeFirst = false)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            var builder = new StringBuilder();
            bool capitalizeNext = capitalizeFirst;

            foreach (var c in name)
            {
                if (c == '-' || c == ' ')
                {
                    if (keepHyphens)
                    {
                        builder.Append('-');
                    }
                    else
                    {
                        capitalizeNext = true;
                    }

                    continue;
                }

                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    continue;
                }

                builder.Append(capitalizeNext && char.IsLetter(c) ? char.ToUpper(c) : c);
                capitalizeNext = false;
            }

            if (builder.Length > 0 && char.IsDigit(builder[0]))
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

        public static bool TryDeleteFile(string targetFile)
        {
            try
            {
                if (!File.Exists(targetFile))
                {
                    return false;
                }

                AssetDatabase.DeleteAsset(targetFile);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete style classes: {e}");
                return false;
            }
        }

        public static string FixSlashes(this string input)
        {
            return input.Replace("\\", "/");
        }
    }
}