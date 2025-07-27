using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#nullable enable
namespace UnityStyleGenerator.Editor.Settings
{
    [FilePath("ProjectSettings/SpriteLibrarySettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class SpriteLibrarySettings : ScriptableSingleton<SpriteLibrarySettings>
    {
        [SerializeField] private string? targetFolder;

        [SerializeField] public List<LibraryGroup> Groups = new();

        [SerializeField] public bool GenerateTheme = true;

        private const string DefaultTargetFolder = "Assets";

        public string TargetFolder
        {
            get => targetFolder ?? DefaultTargetFolder;
            set
            {
                if (targetFolder == value)
                {
                    return;
                }

                TargetChanged?.Invoke(TargetFolder, value);
                targetFolder = value;
                SaveDirty();
            }
        }

        public event Action<string, string>? TargetChanged;

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
        public Sprite Sprite;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Name) && Sprite != null;
        }
    }

    [Serializable]
    public enum SpriteStyleType
    {
        Background,
        Cursor
    }
}