using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using ExtendedSlugbase.Features;
using ExtendedSlugbase.Objects;
using HUD;
using MagicaHookingLibrary.Helpers;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using SlugBase;
using SlugBase.Assets;
using SlugBase.DataTypes;
using SlugBase.Features;
using UnityEngine;
using static ExtendedSlugbase.Objects.PlayerObjects;

namespace ExtendedSlugbase.Helpers;

public static class FeatureHelpers
{
    public delegate void GetFeatureIL(ILCursor c);

    public static VariableDefinition GetFeature<T1, T2>(this ILContext il, ILCursor c, Func<T2, T1> func)
    {
        var localVar = il.ImplementLocalVariable<T1>();
        c.EmitLdarg0Delegate(func);
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

	public static bool TryGetExtCustomScene(this CustomScene scene, out GameObjects.ExtCustomScene extCustomScene)
	{
		return GameObjects.ExtCustomScene.ExtCustomScenes.TryGetValue(scene, out extCustomScene);
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
		bool defaultPaletteLoaded = RainWorldGameHelpers.TryGetDefaultPalette(out var defaultPalette);
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
        if (RequiresDLC.DLCsEnabled(feature.ID) && SlugBaseCharacter.TryGet(name, out var slug) && feature.TryGet(slug, out T v))
        {
            value = v;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Not more efficient than calling <see cref="PlayerFeature.TryGet(Player, out T)"/>, but also checks if the correct DLC is installed.
    /// </summary>
    public static bool TryGetFeature<T>(this Player player, PlayerFeature<T> feature, out T value)
    {
        value = default;
        if (player.SlugCatClass.TryGetFeature(feature, out T result))
        {
            value = result;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Not more efficient than calling <see cref="GameFeature.TryGet(RainWorldGame, out T)"/>, but also checks if the correct DLC is installed.
    /// </summary>
    public static bool TryGetFeature<T>(this RainWorldGame game, GameFeature<T> feature, out T value)
    {
        value = default;
        if (game.IsStorySession && game.StoryCharacter.TryGetFeature(feature, out T result))
        {
            value = result;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Not more efficient than calling <see cref="TimelineFeature.TryGet(RainWorldGame, out T)"/>, but also checks if the correct DLC is installed.
    /// </summary>
    public static bool TryGetFeature<T>(this RainWorldGame game, TimelineFeature<T> feature, out T value)
    {
        value = default;
        if (RequiresDLC.DLCsEnabled(feature.ID) && feature.TryGet(game, out T result))
        {
            value = result;
            return true;
        }
        return false;
    }
    
	public static PlayerFeature<T> ObsoletePlayerFeature<T>(string id, string message = null)
	{
		return new(id, json => {
				ObsoleteFeature(id, json, message);
				return default;
			});
	}
	public static GameFeature<T> ObsoleteGameFeature<T>(string id, string message = null)
	{
		return new(id, json => {
				ObsoleteFeature(id, json, message);
				return default;
			});
	}
	public static TimelineFeature<T> ObsoleteTimelineFeature<T>(string id, string message = null)
	{
		return new(id, json => {
				ObsoleteFeature(id, json, message);
				return default;
			});
	}

	/// <summary>
	/// A method for throwing an inner Slugbase error referring to obsolete features.
	/// </summary>
	internal static void ObsoleteFeature(string id, JsonAny json, string message = null)
	{
		if (!string.IsNullOrEmpty(message))
		{
			throw new JsonException($"{id} {message}", json);
			
		}
		else
		{
			throw new JsonException($"{id} is no longer supported.",json);
		}
	}

    /// <summary>Create a player feature that takes an array of bools.</summary>
    public static PlayerFeature<bool[]> PlayerBools(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToBools(JsonHelpers.AssertLength(json, minLength, maxLength)));

    /// <summary>Create a game feature that takes an array of bools.</summary>
    public static GameFeature<bool[]> GameBools(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToBools(JsonHelpers.AssertLength(json, minLength, maxLength)));

    ///<summary>Convert list to <see cref="bool"/>[].</summary>
    public static bool[] ToBools(JsonAny json) => json.TryBool() == null ? [.. from element in json.AsList() select JsonUtils.ToBool(element)] : [json.AsBool()];


	internal static Dictionary<SlugBaseCharacter.FeatureList, bool> throwDLCErrors = [];

	internal static readonly Feature<bool> ignoreDLCErrors = new("ignore_dlc_errors", json => { return json.AsBool(); });

    /// <summary>
    /// Declares if a feature requires a DLC to properly use, regardless of implementation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class RequiresDLC : Attribute
    {
        internal bool needsMSC;
        internal bool needsWatcher;
        internal bool mutualExclusion;

        internal RequiresDLC(bool MSC = false, bool Watcher = false, bool mutuallyExclusive = true)
        {
            needsMSC = MSC;
            needsWatcher = Watcher;
            mutualExclusion = mutuallyExclusive;
        }

        public static bool DLCsEnabled(string featureID)
        {
            if (SlugbaseHelpers.RegisteredFeatures.TryGetValue(featureID, out var info) && info.dlc != null)
            {
                RequiresDLC dlc = info.dlc;
                bool needsMSC = dlc.needsMSC && !ModManager.MSC;
                bool needsWatcher = dlc.needsWatcher && !ModManager.Watcher;
				if (!dlc.mutualExclusion && needsMSC && needsWatcher)
                {
                    return false;
                }
                if (dlc.mutualExclusion && (needsMSC || needsWatcher))
                {
                    return false;
                }
            }
            return true;
        }
    }
 }
