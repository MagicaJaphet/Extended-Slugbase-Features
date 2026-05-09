using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using MagicaHookingLibrary.Helpers;
using MonoMod.Cil;
using MoreSlugcats;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features.GameRelated;
public class HasGhostPing() : GameFeature<bool>("ghost_pings", JsonUtils.ToBool)
{
	internal static class Implementation
	{
		/// <summary>
		/// Implements ghost pings when entering a new region.
		/// </summary>
		internal static void Player_ClassMechanicsSaint(ILCursor c)
		{
			static bool GhostPings(bool isSaint, Player self)
			{
				return self.room != null && (isSaint || self.room.game.TryGetFeature(ExtGameFeatures.HasGhostPing, out bool pings) && pings);
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchLdloc(0)
				);
			c.EmitLdarg0Delegate(GhostPings);

			// Patch the region name assignment when "" so it skips if it's the Player's spawning room which isn't a shelter
			static bool IsNotStartingRoom(bool isNullOrEmpty, Player self)
			{
				return isNullOrEmpty && (self.room.abstractRoom.shelter || self.room.abstractRoom.name == "SI_SAINTINTRO" || self.AI != null);
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchLdstr(""),
				x => x.MatchCallOrCallvirt(out _)
				);
			c.EmitLdarg0Delegate(IsNotStartingRoom);

			static SlugcatStats.Name GhostForSlugcat(SlugcatStats.Name saint, Player self)
			{
				return new(SlugcatStats.Name.values.entries.FirstOrDefault(slug => slug == self.room.game.TimelinePoint.value));
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchBrfalse(out _),
				x => x.MatchLdsfld(nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint).GetSlugcatFieldInfo())
				); // if (this.room != null && World.CheckForRegionGhost(MoreSlugcatsEnums.SlugcatStatsName.Saint, this.room.world.region.name))
			c.EmitLdarg0Delegate(GhostForSlugcat); // Removes the hardcoded World.CheckForRegionGhost for Saint
		}

		/// <summary>
		/// Implements ghost pings when starting a new cycle from a shelter.
		/// </summary>
		internal static void ShelterDoor_Update(ILCursor c)
		{
			static bool GhostPings(bool isSaint, ShelterDoor self)
			{
				return isSaint || self.room.game.TryGetFeature(ExtGameFeatures.HasGhostPing, out bool pings) && pings;
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint).GetSlugcatFieldInfo()
				); // if (ModManager.MSC && this.room.game.IsStorySession && this.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint && this.room.world.region != null && World.CheckForRegionGhost(MoreSlugcatsEnums.SlugcatStatsName.Saint, this.room.world.region.name))
			c.EmitLdarg0Delegate(GhostPings);

			// Then unhardcode the ghost check
			static SlugcatStats.Name CheckGhosts(SlugcatStats.Name saint, ShelterDoor self)
			{
				return self.room.game.StoryCharacter;
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchLdsfld(nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint).GetSlugcatFieldInfo())
				);
			c.EmitLdarg0Delegate(CheckGhosts);
		}
	}
}
