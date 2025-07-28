#nullable enable
using System;
using UnityEditor;
using UnityEngine;

namespace LeosTools.Editor
{
    [FilePath("ProjectSettings/StyleSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class StyleSettings : ScriptableSingleton<StyleSettings>
    {
        [SerializeField] private string? targetFolder;
        [SerializeField] private string? sourceFolder;
        [SerializeField] private string? prefix;

        private const string DefaultTargetFolder = "Assets";
        private const string DefaultSourceFolder = "Assets";
        private const string DefaultPrefix = "Style_";

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

        public string SourceFolder
        {
            get => sourceFolder ?? DefaultSourceFolder;
            set
            {
                if (sourceFolder == value)
                {
                    return;
                }

                sourceFolder = value;
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

        private void SetDefaultFolder()
        {
            if (targetFolder is null)
            {
                TargetFolder = DefaultTargetFolder;
            }

            if (sourceFolder is null)
            {
                SourceFolder = DefaultSourceFolder;
            }
        }

        private void SaveDirty()
        {
            Save(this);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}