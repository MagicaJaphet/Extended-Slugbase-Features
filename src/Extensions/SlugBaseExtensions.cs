using ExtendedSlugbase.Assets;
using ExtendedSlugbase.DataTypes;
using ExtendedSlugbase.Features;
using MagicaHookingLibrary.Helpers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using SlugBase;
using SlugBase.Assets;
using SlugBase.DataTypes;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static ExtendedSlugbase.Features.ExtFeatureTypes;

namespace ExtendedSlugbase.Extensions;

/// <summary>
/// Intended to be used with implementing local variables in IL hooks.
/// </summary>
public static class TypeConverters
{
	public static Player GetPlayer(Player.Tongue self) => self.player;
	public static Player GetPlayer(PlayerGraphics self) => self.player;
	public static Player GetPlayer(Spear self) => self.thrownBy as Player;
}

public static class SlugbaseHelpers
{
	public static readonly Type FeatureManager;
	public static readonly MethodInfo Register;
	public static readonly MethodInfo TryGetFeature;

	/// <summary>
	/// Shorthand for invoking <see cref="TryGetFeature"/>.
	/// </summary>
	public static bool InvokeTryGetFeature(string id, out Feature result)
	{
		object[] args = [id, null];
		result = null;
		if (TryGetFeature?.Invoke(null, args) is bool gotFeature && gotFeature && args[1] is Feature feature)
		{
			result = feature;
			return true;
		}
		return false;
	}

	public static readonly MethodInfo AddMany;

	public static readonly Type FeatureHooks;

	static SlugbaseHelpers()
	{
		FeatureManager = (from a in ReflectionHelpers.GetScanAssemblies() from type in a.GetTypes() where type.Name == "FeatureManager" select type).FirstOrDefault();
		Register = FeatureManager?.GetMethod(nameof(Register), BindingFlags.Public | BindingFlags.Static);
		TryGetFeature = FeatureManager?.GetMethod(nameof(TryGetFeature), BindingFlags.Public | BindingFlags.Static);

		// From SlugBaseCharacter.FeatureList
		AddMany = typeof(SlugBaseCharacter.FeatureList).GetMethod(nameof(AddMany), BindingFlags.NonPublic | BindingFlags.Instance);

		// From SlugBase.Features
		FeatureHooks = typeof(Feature).Assembly.GetTypes().FirstOrDefault(x => x.Name == nameof(FeatureHooks));
	}

	/// <summary>
	/// A dictionary containing all registered <see cref="Feature"/>s, and their <see cref="FeatureInfo"/>.
	/// </summary>
	public static Dictionary<string, FeatureInfo> RegisteredFeatures { get; } = [];
	public struct FeatureInfo
	{
		public RequiresDLC dlc;
		public Assembly originAssembly;
	}

	public static void CheckForInvalidDLC(string id, JsonAny json, bool throwDLCError = true)
	{
		if (RegisteredFeatures.TryGetValue(id, out var info) && !RequiresDLC.DLCsEnabled(id) && throwDLCError)
		{
			throw new JsonException($"{id} needs {info.dlc.needsMSC.BlankConditional("MSC")}{(info.dlc.needsMSC && info.dlc.needsWatcher).BlankConditional(info.dlc.mutualExclusion ? " or" : " and")}{info.dlc.needsWatcher.BlankConditional(" Watcher")} enabled to use!", json);
		}
	}
}

public static class SlugBaseExtensions
{
	public static VariableDefinition ImplementFeatureVariable<T1, T2>(this PlayerFeature<T1> feature, ILContext il, ILCursor c, Func<T2, Player> converter = null)
	{
		if (converter != null)
		{
			T1 Converter(T2 self) => feature.Get(converter(self));
			return il.ImplementFeatureVariable<T1, T2>(c, Converter);
		}
		return il.ImplementFeatureVariable<T1, Player>(c, feature.Get);
	}

	public static VariableDefinition ImplementFeatureVariable<T1, T2>(this GameFeature<T1> feature, ILContext il, ILCursor c, Func<T2, RainWorldGame> converter = null)
	{
		if (converter != null)
		{
			T1 Converter(T2 self) => feature.Get(converter(self));
			return il.ImplementFeatureVariable<T1, T2>(c, Converter);
		}
		return il.ImplementFeatureVariable<T1, RainWorldGame>(c, feature.Get);
	}

	public static VariableDefinition ImplementFeatureVariable<T1, T2>(this Feature<T1> feature, ILContext il, ILCursor c, Func<T2, SlugBaseCharacter> converter = null)
	{
		if (converter != null)
		{
			T1 Converter(T2 self) => feature.Get(converter(self));
			return il.ImplementFeatureVariable<T1, T2>(c, Converter);
		}
		return il.ImplementFeatureVariable<T1, SlugBaseCharacter>(c, feature.Get);
	}

	private static VariableDefinition ImplementFeatureVariable<T1, T2>(this ILContext il, ILCursor c, Func<T2, T1> implementation)
	{
		var localVar = il.ImplementLocalVariable<T1>();
		c.EmitLdarg0Delegate(implementation);
		c.Emit(OpCodes.Stloc, localVar);

		return localVar;
	}

	/// <summary>
	/// Emits a local variable and optionally <see cref="OpCodes.Ldarg_0"/> onto the stack. Returns in order of (stack), (local var), (ldarg_0)
	/// </summary>
	public static void EmitFeatureDelegate(this ILCursor c, VariableDefinition feature, Delegate del, bool emitLdarg0 = false)
    {
        c.Emit(OpCodes.Ldloc, feature);
        if (emitLdarg0)
            c.Emit(OpCodes.Ldarg_0);

        c.EmitDelegate(del);
	}

	/// <summary>
	/// Intended to be used with IL local variable declarations, returns an instance of a nullable feature type.
	/// </summary>
	public static T Get<T>(this PlayerFeature<T> feature, Player player) => player != null && feature.TryGet(player, out T result) ? result : default;

	/// <summary>
	/// Intended to be used with IL local variable declarations, returns an instance of a nullable feature type.
	/// </summary>
	public static T Get<T>(this GameFeature<T> feature, RainWorldGame game) => game != null && feature.TryGet(game, out T result) ? result : default;

	/// <summary>
	/// Intended to be used with IL local variable declarations, returns an instance of a nullable feature type.
	/// </summary>
	public static T Get<T>(this Feature<T> feature, SlugBaseCharacter slugBaseCharacter) => slugBaseCharacter != null && feature.TryGet(slugBaseCharacter, out T result) ? result : default;

	/// <summary>
	/// Shorthand for retrieving a <see cref="SlugBaseCharacter"/>'s <see cref="ColorSlot"/>s, if custom colors are present.
	/// </summary>
	public static bool TryGetColorSlots(this Player player, out ColorSlot[] slots)
    {
        slots = null;
        if (player.TryGetFeature(PlayerFeatures.CustomColors, out var colors))
        {
            slots = colors;
            return true;
        }
        return false;
    }

    public static bool TryGetExtColorSlot(this ColorSlot slot, out ExtColorSlot extSlot)
    {
        return ExtColorSlot.ExtendedColorSlots.TryGetValue(slot, out extSlot);
    }

	public static bool TryGetExtCustomScene(this CustomScene scene, out ExtCustomScene extCustomScene)
	{
		return ExtCustomScene.ExtCustomScenes.TryGetValue(scene, out extCustomScene);
	}

    /// <summary>
    /// Shorthand for getting a <see cref="Color"/> from a <see cref="SlugBaseCharacter"/>'s custom colors by a string key.
    /// </summary>
    public static bool TryGetCustomColor(this PlayerGraphics graphics, ColorSlot[] slots, string name, out Color color)
    {
        color = new();
        if (slots.FirstOrDefault(col => col.Name.ToLower() == name.ToLower()) is ColorSlot col)
        {
			color = col.GetSlotColor(graphics.player.ArenaIndex(), graphics.player.abstractCreature.Room.world?.game?.cameras[0]?.paletteTexture);

			// Also use starve color for the final color
			if (name == "Body")
			{
				Color starveColor = Color.Lerp(color, Color.gray, 0.4f);
				float starveAmount = graphics.player.Malnourished ? graphics.malnourished : Mathf.Max(0f, graphics.malnourished - 0.005f);
				color = Color.Lerp(color, starveColor, starveAmount);
			}
			return true;
        }
        return false;
	}

	public static Color GetSlotColor(this ColorSlot col, int? variant, Texture2D palette)
	{
		bool defaultPaletteLoaded = RainWorldGameExtensions.TryGetDefaultPalette(out var defaultPalette);
		bool hasExtSlot = col.TryGetExtColorSlot(out var extSlot);

		// Calculate default color
		Color color = col.Default;
		if (hasExtSlot && extSlot.TryGetPalKey(out var palKey, variant) && defaultPaletteLoaded)
		{
			color = (palette ?? defaultPalette).GetPixel(palKey.x, palKey.y);
		}
		if (variant is int index && col.Variants.Length > index)
		{
			color = col.Variants[index];
		}

		// Calculate potential fade
		Color? fadeColor = null;
		if (hasExtSlot)
		{
			if (extSlot.TryGetFadePalKey(out var fadePalKey, variant) && defaultPaletteLoaded)
			{
				fadeColor = (palette ?? defaultPalette).GetPixel(fadePalKey.x, fadePalKey.y);
			}
			else if (extSlot.TryGetFadeColor(out var fade, variant))
			{
				fadeColor = fade;
			}

			if (fadeColor != null && Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game
				&& game.cameras[0]?.PaletteDarkness() is float darkness)
			{
				color = Color.Lerp(color, fadeColor.Value, Mathf.Lerp(extSlot.FadeVariance[0], extSlot.FadeVariance[1], darkness));
			}
		}
		

		return color.ReturnPaletteOrOffBlack();
	}

	/// <summary>
	/// Returns a value from a <see cref="Feature"/>'s value based on a <see cref="SlugcatStats.Name"/>.
	/// </summary>
	public static bool TryGetFeature<T>(this SlugcatStats.Name name, Feature<T> feature, out T value)
    {
		value = default;
        return RequiresDLC.DLCsEnabled(feature.ID) && SlugBaseCharacter.TryGet(name, out var slug) && feature.TryGet(slug, out value);
    }

    /// <summary>
    /// Not more efficient than calling <see cref="PlayerFeature.TryGet(Player, out T)"/>, but also checks if the correct DLC is installed.
    /// </summary>
    public static bool TryGetFeature<T>(this Player player, PlayerFeature<T> feature, out T value)
    {
		return player.SlugCatClass.TryGetFeature(feature, out value);

	}

    /// <summary>
    /// Not more efficient than calling <see cref="GameFeature.TryGet(RainWorldGame, out T)"/>, but also checks if the correct DLC is installed.
    /// </summary>
    public static bool TryGetFeature<T>(this RainWorldGame game, GameFeature<T> feature, out T value)
    {
        value = default;
		return game.IsStorySession && game.StoryCharacter.TryGetFeature(feature, out value);
	}

	/// <summary>
	/// Not more efficient than calling <see cref="TimelineFeature.TryGet(RainWorldGame, out T)"/>, but also checks if the correct DLC is installed.
	/// </summary>
	public static bool TryGetFeature<T>(this RainWorldGame game, TimelineFeature<T> feature, out T value)
	{
		value = default;
		return RequiresDLC.DLCsEnabled(feature.ID) && feature.TryGet(game, out value);
	}
}
