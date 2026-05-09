using SlugBase;
using SlugBase.DataTypes;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Drawing.Drawing2D;
using RWCustom;

namespace ExtendedSlugbase.Extensions;
public static class PlayerExtensions
{
	/// <summary>
	/// Returns the arena index of the slugcat, if the current active session is an arena.
	/// </summary>
	public static int? ArenaIndex(this Player player)
	{
		if (player?.abstractCreature.world?.game.IsArenaSession is bool arena && arena && player.abstractCreature.Room.world.game.GetArenaGameSession.arenaSitting.gameTypeSetup.gameType != DLCSharedEnums.GameTypeID.Challenge)
		{
			return player.playerState.playerNumber;
		}
		return null;
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
}
