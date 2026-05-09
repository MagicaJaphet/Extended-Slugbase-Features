using SlugBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ExtendedSlugbase.Assets;

/// <summary>
/// A utility class meant to be used to load SlugBase-related atlases and elements.
/// </summary>
public class AtlasManager
{
	public static string GetSlugbaseName(SlugBaseCharacter slugcat)
	{
		return (slugcat.DisplayName.StartsWith("The ", StringComparison.InvariantCultureIgnoreCase) ? slugcat.DisplayName.Substring(4) : slugcat.DisplayName).ToLowerInvariant();
	}

	public static Dictionary<SlugBaseCharacter, Dictionary<FixedSpriteElements, string>> SpriteElements { get; } = [];

	public enum FixedSpriteElements
	{
		JollyIcon,
		JollyIconDead,
		JollyPlayerIcon,
		JollyPlayerUniqueIcon
	}

	public static string GetNameKey(FixedSpriteElements element, SlugBaseCharacter slugcat, int? index = null)
	{
		var name = GetSlugbaseName(slugcat);
		return element switch
		{
			FixedSpriteElements.JollyIcon => $"jolly_icon_{name}",
			FixedSpriteElements.JollyIconDead => $"jolly_icon_{name}_dead",
			FixedSpriteElements.JollyPlayerIcon => $"{name}_pup_off",
			FixedSpriteElements.JollyPlayerUniqueIcon => $"unique_{name}{($"_{index}_" ?? "")}_pup_off", // TODO: Implement custom sprites for all color slots
			_ => null,
		};
	}

	public static bool TryGetElement(SlugBaseCharacter character, FixedSpriteElements element, out string result, string folder = "illustrations")
	{
		result = null;
		if (character != null && SpriteElements.TryGetValue(character, out var elementDict))
		{
			if (elementDict.TryGetValue(element, out result))
			{
				if (!Futile.atlasManager.DoesContainElementWithName(result))
				{
					TryLoadImage(result, folder);
				}
				return Futile.atlasManager.DoesContainElementWithName(result);
			}
		}
		return false;
	}

	public static bool TryLoadImage(string fileName, string folder = "illustrations")
	{
		if (AssetManager.ResolveFilePath(Path.Combine(folder, $"{fileName}.png")) is string iconPath && File.Exists(iconPath))
		{
			var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
			try
			{
				AssetManager.SafeWWWLoadTexture(ref texture, iconPath, true, true);
			}
			catch (FileLoadException ex)
			{
				Plugin.Logger?.LogError(ex);
				return false;
			}
			HeavyTexturesCache.LoadAndCacheAtlasFromTexture(fileName, texture, false);
			Plugin.Logger?.LogInfo($"Loaded image {fileName}!");
			return true;
		}
		return false;
	}

	internal static void LoadSlugbaseImages()
	{
		foreach (var slugcat in SlugBaseCharacter.Registry.Values)
		{
			SpriteElements[slugcat] = [];

			foreach (FixedSpriteElements element in Enum.GetValues(typeof(FixedSpriteElements)))
			{
				var nameKey = GetNameKey(element, slugcat);
				if (TryLoadImage(nameKey))
				{
					SpriteElements[slugcat][element] = nameKey;
				}
			}
		}
	}
}
