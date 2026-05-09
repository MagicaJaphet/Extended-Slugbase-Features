using RWCustom;
using SlugBase;
using SlugBase.DataTypes;
using System.Collections.Generic;
using UnityEngine;

namespace ExtendedSlugbase.DataTypes;
public class ExtColorSlot
{
	public static Dictionary<ColorSlot, ExtColorSlot> ExtendedColorSlots { get; } = [];

	public IntVector2? DefaultPaletteIndex { get; internal set; }
	public IntVector2?[] VariantPaletteIndexes { get; internal set; }
	public Color? DefaultFade { get; }
	public IntVector2? DefaultFadePaletteIndex { get; }
	public Color?[] VariantFades { get; }
	public IntVector2?[] VariantFadePaletteIndexes { get; }
	public Vector2 FadeVariance { get; } = new(0.08f, 0.04f);

	public bool TryGetPalKey(out IntVector2 key, int? variant = null)
	{
		key = default;
		if (variant is int v && VariantPaletteIndexes != null && VariantPaletteIndexes.Length > v
			&& VariantPaletteIndexes[v] is IntVector2 arenaCol)
		{
			key = arenaCol;
			return true;
		}
		else if (DefaultFadePaletteIndex is IntVector2 col)
		{
			key = col;
			return true;
		}
		return false;
	}

	public bool TryGetFadeColor(out Color color, int? variant = null)
	{
		color = default;
		if (variant is int v && VariantFades != null && VariantFades.Length > v
			&& VariantFades[v] is Color arenaCol)
		{
			color = arenaCol;
			return true;
		}
		else if (DefaultFade is Color col)
		{
			color = col;
			return true;
		}
		return false;
	}

	public bool TryGetFadePalKey(out IntVector2 key, int? variant = null)
	{
		key = default;
		if (variant is int v && VariantFadePaletteIndexes != null && VariantFadePaletteIndexes.Length > v
			&& VariantFadePaletteIndexes[v] is IntVector2 arenaCol)
		{
			key = arenaCol;
			return true;
		}
		else if (DefaultFadePaletteIndex is IntVector2 col)
		{
			key = col;
			return true;
		}
		return false;
	}

	public float LerpThresholds(float lerp)
	{
		if (FadeVariance == null)
		{
			return 1f;
		}
		return Mathf.Lerp(FadeVariance.x, FadeVariance.y, lerp);
	}


	public void ParseColor(JsonAny json, out Color? col, out IntVector2? pal)
	{
		col = null;
		pal = null;

		if (json.TryParse(out Color color, throwIfParseError: false))
		{
			col = color;
			return;
		}
		if (json.TryParse(out IntVector2 palette, throwIfParseError: false))
		{
			pal = palette;
			return;
		}
		throw new JsonException("Value is not a valid Color or IntVector2!", json);
	}

	public ExtColorSlot(JsonObject json)
	{
		if (json.TryGet("story_fade", out JsonAny any))
		{
			ParseColor(any, out var fadeCol, out var fadePal);
			DefaultFade = fadeCol;
			DefaultFadePaletteIndex = fadePal;
		}
		if (json.TryGet("arena_fade", out JsonList list))
		{
			Color?[] arenaColors = new Color?[list.Count];
			IntVector2?[] arenaPalettes = new IntVector2?[list.Count];

			for (int i = 0; i < list.Count; i++)
			{
				var item = list[i];
				ParseColor(item, out var arenaFadeCol, out var arenaFadePal);
				arenaColors[i] = arenaFadeCol ?? default;
				arenaPalettes[i] = arenaFadePal;
			}

			VariantFades = arenaColors;
			VariantFadePaletteIndexes = arenaPalettes;
		}
		if (json.TryGet("darkness_variance", out Vector2 fades))
		{
			FadeVariance = fades;
		}
	}
}
