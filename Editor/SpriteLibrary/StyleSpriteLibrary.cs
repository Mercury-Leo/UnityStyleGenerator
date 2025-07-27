
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityStyleGenerator.Editor 
{
	[CreateAssetMenu(fileName = "newToolkitSpriteLibrary", menuName = "ToolkitSpriteLibrary")]
	public class StyleSpriteLibrary : ScriptableObject 
	{
		[SerializeField] [Tooltip("Folder to generate USS's into")]
		private string targetFolder = null!; 
		
		[SerializeField] [Tooltip("The Theme that will hold the generated USS's")]
		private ThemeStyleSheet styleThemeSheet = null!;
		
		[SerializeField] private List<LibraryGroup> library = null!;

		private void Generate()
		{
		}
		
		[Serializable]
		private struct LibraryGroup
		{
			public string Name;
			public List<Sprite> Sprites;
		}
	}
}

