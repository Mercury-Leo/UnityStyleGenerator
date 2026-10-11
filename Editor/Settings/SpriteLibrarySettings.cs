#nullable enable
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SpriteLibrary.Editor
{
    [FilePath("ProjectSettings/SpriteLibrarySettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class SpriteLibrarySettings : ScriptableSingleton<SpriteLibrarySettings>
    {
        [SerializeField] private string? targetFolder;
        [SerializeField] private string? themeName;
        [SerializeField] private string? prefix;

        [SerializeField] public List<LibraryGroup> Groups = new();

        [SerializeField] public bool GenerateTheme = true;

        private const string DefaultTargetFolder = "Assets";
        private const string DefaultThemeName = "SpriteLibraryTheme";
        private const string DefaultPrefix = "Sprite_";

        public string TargetFolder
        {
            get => targetFolder ?? DefaultTargetFolder;
            set
            {
                if (targetFolder == value)
                {
                    return;
                }

                targetFolder = value;
                SaveDirty();
            }
        }

        public string ThemeName
        {
            get => themeName ?? DefaultThemeName;
            set
            {
                themeName = value;
                SaveDirty();
            }
        }

        public string Prefix
        {
            get => prefix ?? DefaultPrefix;
            set
            {
                prefix = value;
                SaveDirty();
            }
        }

        private void Awake()
        {
            SetDefaultFolder();
        }

        public void SaveDirty()
        {
            Save(this);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        private void SetDefaultFolder()
        {
            if (targetFolder is null)
            {
                TargetFolder = DefaultTargetFolder;
            }
        }
    }

    [Serializable]
    public struct LibraryGroup
    {
        public string Name;
        public List<LibraryEntry> Entries;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Name) && Entries.Count > 0;
        }
    }

    [Serializable]
    public struct LibraryEntry
    {
        public string Name;
        public SpriteStyleType Type;

        /// <summary>
        /// A <see cref="UnityEngine.Sprite"/> or a <see cref="VectorImage"/>.
        /// Kept as "Sprite" so existing serialized references load unchanged.
        /// </summary>
        public Object? Sprite;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Name) && IsSupported(Sprite);
        }

        public static bool IsSupported(Object? asset)
        {
            return asset != null && asset is UnityEngine.Sprite or VectorImage;
        }
    }

    [Serializable]
    public enum SpriteStyleType
    {
        Background,
        Cursor
    }
}