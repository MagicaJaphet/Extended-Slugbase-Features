using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ExtendedSlugbaseFeatures.Resources;
internal static class Extensions
{
	internal static Color GetWatcherColor(this Player self, RoomPalette palette, Color color, int i)
	{
		if (self.HasFeature(ExtFeatures.blackColorFade, out var values) && values.TryGetValue("fades", out var fades) && fades.Length + 1 > i)
		{
			return Color.Lerp(color == Color.black ? Custom.HSL2RGB(0.63055557f, 0.54f, 0.5f) : color, palette.blackColor, (values.TryGetValue("variance", out var variances) ? Mathf.Lerp(variances.Length >= 1 ? variances[0] : 0.08f, variances.Length >= 2 ? variances[1] : 0.04f, palette.darkness) : Mathf.Lerp(0.08f, 0.04f, palette.darkness)) * fades[i]);
		}
		return color == Color.black ? palette.blackColor : color;
	}

	/// <summary>
	/// Adds <paramref name="food"/> into the <see cref="Player"/>'s food meter. Returns true if <paramref name="food"/> is more than 0.
	/// </summary>
	internal static bool ProcessFood(this Player player, float food)
	{
		int quarterPips = Mathf.RoundToInt(food * 4f);

		for (; quarterPips >= 4; quarterPips -= 4)
			player.AddFood(1);

		for (; quarterPips >= 1; quarterPips--)
			player.AddQuarterFood();

		return food > 0f;
	}

	#warning This method do not currently work properly due to the HUD not updating for quarter steps
	internal static bool UnprocessFood(this Player player, float food)
	{
		int quarterPips = Mathf.RoundToInt(food * 4f);

		for (; quarterPips >= 4; quarterPips -= 4)
			player.SubtractFood(1);

		for (; quarterPips >= 1; quarterPips--)
			player.SubtractQuarterFood();

		return food > 0f;
	}

	#warning This method do not currently work properly due to the HUD not updating for quarter steps
	internal static void SubtractQuarterFood(this Player player)
	{
		if (player.redsIllness != null)
		{
			player.redsIllness.AddQuarterFood();
		}
		else if (player.FoodInStomach < player.MaxFoodInStomach)
		{
			player.playerState.quarterFoodPoints--;
			if (ModManager.CoopAvailable && player.abstractCreature.world.game.IsStorySession && player.abstractCreature.world.game.Players[0] != player.abstractCreature && !player.isNPC)
			{
				PlayerState obj = player.abstractCreature.world.game.Players[0].state as PlayerState;
				obj.quarterFoodPoints--;
			}

			if (player.playerState.quarterFoodPoints < 0)
			{
				player.SubtractFood(1);
				player.playerState.quarterFoodPoints = 3;
			}
		}
	}

	/// <summary>
	/// Attempts to move the <see cref="ILCursor"/> after the next instance of <see cref="SlugcatStats.Name"/>. Returns true if successful.
	/// </summary>
	internal static bool MoveToNextSlugcat(this ILCursor cursor, FieldInfo info, [CallerMemberName] string method = "")
	{
		try
		{
			Func<Instruction, bool> isSlugcat = info.IsStatic ?
				(x => x.MatchLdsfld(info)) :
				x => x.MatchLdfld(info);

			if (cursor.TryGotoNext(MoveType.After,
				isSlugcat,
				x => x.MatchCallOrCallvirt(out _)))
			{
				cursor.MoveAfterLabels();
				return true;
			}
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.LogException(ex);
		}
		Plugin.Logger.LogError($"IL HOOK MATCH FAILED AT: {method}");
		return false;
	}

	// Auto inserts our IL based on the type of delegate we use
	internal static void ImplementILCodeAssumingLdarg0(this ILCursor cursor, Delegate implementation)
	{
		cursor.Emit(OpCodes.Ldarg_0);
		cursor.EmitDelegate(implementation);
	}

	/// <summary>
	/// Explicit check for if a <see cref="Feature"/>'s return value is not default by our standards.
	/// </summary>
	/// <returns></returns>
	internal static bool HasFeature<T>(this Player player, PlayerFeature<T> feature, bool shouldReturnTrue = true)
	{
		if (player.HasFeature(feature, out var value))
		{
			if (value is bool boolValue)
			{
				return (shouldReturnTrue && boolValue) || (!shouldReturnTrue && !boolValue);
			}
			else if (value is bool[] boolValues)
			{
				return (shouldReturnTrue && boolValues.Any(x => x)) || (!shouldReturnTrue && !boolValues.Any(x => x));
			}
			else if (value is int intValue)
			{
				return (shouldReturnTrue && intValue > -1) || (!shouldReturnTrue && intValue < 0);
			}
			else if (value is int[] intValues)
			{
				return (shouldReturnTrue && intValues.Any(x => x > -1)) || (!shouldReturnTrue && !intValues.Any(x => x > -1));
			}
			else if (value is float floatValue)
			{
				return (shouldReturnTrue && floatValue > -1) || (shouldReturnTrue && floatValue < 0);
			}
			else
			{
				return (shouldReturnTrue && value != null) || (!shouldReturnTrue && value == null);
			}
		}
		return !shouldReturnTrue;
	}
	/// <summary>
	/// Explicit check for if a <see cref="Feature"/>'s return value is not default by our standards.
	/// </summary>
	/// <returns></returns>
	internal static bool HasFeature<T>(this RainWorldGame game, GameFeature<T> feature, bool shouldReturnTrue = true)
	{
		if (game.HasFeature(feature, out var value))
		{
			if (value is bool boolValue)
			{
				return (shouldReturnTrue && boolValue) || (!shouldReturnTrue && !boolValue);
			}
			else if (value is bool[] boolValues)
			{
				return (shouldReturnTrue && boolValues.Any(x => x)) || (!shouldReturnTrue && !boolValues.Any(x => x));
			}
			else if (value is int intValue)
			{
				return (shouldReturnTrue && intValue > -1) || (!shouldReturnTrue && intValue < 0);
			}
			else if (value is int[] intValues)
			{
				return (shouldReturnTrue && intValues.Any(x => x > -1)) || (!shouldReturnTrue && !intValues.Any(x => x > -1));
			}
			else if (value is float floatValue)
			{
				return (shouldReturnTrue && floatValue > -1) || (shouldReturnTrue && floatValue < 0);
			}
			else
			{
				return (shouldReturnTrue && value != null) || (!shouldReturnTrue && value == null);
			}
		}
		return !shouldReturnTrue;
	}

	/// <summary>
	/// Default shorthand for <see cref="PlayerFeature{T}.TryGet(Player, out T)"/>.
	/// </summary>
	/// <returns></returns>
	internal static bool HasFeature<T>(this Player player, PlayerFeature<T> feature, out T value)
	{
		return feature.TryGet(player, out value);
	}
	/// <summary>
	/// Default shorthand for <see cref="GameFeature{T}.TryGet(RainWorldGame, out T)"/>.
	/// </summary>
	/// <returns></returns>
	internal static bool HasFeature<T>(this RainWorldGame game, GameFeature<T> feature, out T value)
	{
		return feature.TryGet(game, out value);
	}

	/// <summary>
	/// Used mainly for JSON formatting.
	/// </summary>
	public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
	{
		foreach (var i in ie)
		{
			action(i);
		}
	}
}
