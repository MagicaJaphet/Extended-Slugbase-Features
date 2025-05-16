using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using MoreSlugcats;
using MonoMod.RuntimeDetour;
using RWCustom;
using SlugBase;
using Watcher;
using System.Linq;
using SlugBase.Features;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using static ExtendedSlugbaseFeatures.Resources;

namespace ExtendedSlugbaseFeatures
{
	/// <summary>
	/// Handles various hooks to other parts of the game with <see cref="SlugcatStats.Name"/> checks.
	/// </summary>
	public class WorldHooks
	{
		public static void Apply()
		{
			GeneralHooks.Apply();	
			SpearmasterHooks.Apply();
			GourmandHooks.Apply();
			SaintHooks.Apply();
		}
	}

	internal class GeneralHooks
	{
		internal static void Apply()
		{
			On.RainWorldGame.TryGetPlayerStartPos += RainWorldGame_TryGetPlayerStartPos;
			On.Room.Loaded += IntroCutsceneHandler;
			new Hook(typeof(StoryGameSession).GetProperty(nameof(StoryGameSession.slugPupMaxCount), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod(), SpawnSlugPups);
		}

		/// <summary>
		/// Allows <see cref="SlugBaseCharacter"/> to set the character's starting position in a room, in the room tiles measurement.
		/// </summary>
		internal static bool RainWorldGame_TryGetPlayerStartPos(On.RainWorldGame.orig_TryGetPlayerStartPos orig, string room, out IntVector2 pos)
		{
			if (Custom.rainWorld.inGameSlugCat != null && SlugBaseCharacter.TryGet(Custom.rainWorld.inGameSlugCat, out var character) && ExtFeatures.possibleSpawnPositons.TryGet(character, out var startRooms) && startRooms.TryGetValue(room, out pos))
			{
				return pos != null;
			}

			return orig(room, out pos);
		}

		/// <summary>
		/// Allows <see cref="SlugBaseCharacter"/> to create a <see cref="RoomSpecificScript"/> to control the slugcat and spawn external objects for a period of time.
		/// </summary>
		private static void IntroCutsceneHandler(On.Room.orig_Loaded orig, Room self)
		{
			orig(self);

			if (self?.game != null && self.game.IsStorySession && self.game.GetStorySession.saveState.cycleNumber == 0 && ExtFeatures.introCutsceneDict.TryGet(self.game, out var introVariables) && GameFeatures.StartRoom.TryGet(self.game, out string[] rooms) && rooms.Contains(self.abstractRoom.name) && introVariables.TryGetValue(self.abstractRoom.name, out var dict))
			{
				self.AddObject(new Scripts.MovementScript(self, dict));
			}
		}

		/// <summary>
		/// Allows <see cref="SlugBaseCharacter"/> to spawn slugpups in their campaign.
		/// </summary>
		internal static int SpawnSlugPups(Func<StoryGameSession, int> orig, StoryGameSession self)
		{
			if (ModManager.MSC && self.game != null && ExtFeatures.maxSlugpupSpawns.TryGet(self.game, out int maxPups))
			{
				return maxPups;
			}

			return orig(self);
		}
	}
	internal class SpearmasterHooks
	{
		internal static void Apply()
		{
			IL.Room.Loaded += Room_Loaded;
		}

		/// <summary>
		/// Allows <see cref="SlugBaseCharacter"/> to use the Spearmaster broadcast mechanic, if the <see cref="CollectToken.whiteToken"/> object exists in it's world state, and change if <see cref="KarmaFlower"/> spawn.
		/// </summary>
		internal static void Room_Loaded(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				// Spearmaster broadcasts
				if (cursor.TryGotoNext(
					MoveType.After,
					x => x.MatchCallOrCallvirt(out _),
					x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear)),
					x => x.MatchCall(out _)
					))
				{
					ILLabel nextJump = (ILLabel)cursor.Next.Operand;

					cursor.Emit(OpCodes.Brfalse_S, nextJump);
					cursor.Emit(OpCodes.Ldarg_0);
					static bool HasBroadcasts(Room self)
					{
						return self.game.HasFeature(ExtFeatures.canProcessWhiteTokens, false);
					}
					cursor.EmitDelegate(HasBroadcasts);

				}

				// if (this.game.StoryCharacter != SlugcatStats.Name.Red && (!(this.game.session is StoryGameSession) || !(this.game.session as StoryGameSession).saveState.ItemConsumed(this.world, true, this.abstractRoom.index, num21)))
				cursor.MoveToNextSlugcat(typeof(SlugcatStats.Name).GetField(nameof(SlugcatStats.Name.Red)));

				cursor.Emit(OpCodes.Ldarg_0);
				static bool DontSpawnKarmaFlowers(bool isRed, Room self)
				{
					return isRed && (!ExtFeatures.shouldSpawnKarmaFlowers.TryGet(self.game, out bool canSpawn) || canSpawn);
				}
				cursor.EmitDelegate(DontSpawnKarmaFlowers);
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogException(ex);
			}
		}
	}
	internal class GourmandHooks
	{
		internal static void Apply()
		{
			On.RegionGate.customOEGateRequirements += RegionGate_customOEGateRequirements;
		}

		/// <summary>
		/// Allows <see cref="SlugBaseCharacter"/> to open the OE gate, depending on if Gourmand should have been beaten or not.
		/// </summary>
		/// <param name="orig"></param>
		/// <param name="self"></param>
		/// <returns></returns>
		private static bool RegionGate_customOEGateRequirements(On.RegionGate.orig_customOEGateRequirements orig, RegionGate self)
		{
			if (self.room.game.HasFeature(ExtFeatures.openOEGate, out bool[] flags) && flags[0] && ((flags.Length == 2 && !flags[1]) || (self.room.game.IsStorySession && (self.room.game.rainWorld.progression.miscProgressionData.beaten_Gourmand || self.room.game.rainWorld.progression.miscProgressionData.beaten_Gourmand_Full || MoreSlugcats.MoreSlugcats.chtUnlockOuterExpanse.Value))))
			{
				return true;
			}
			return orig(self);
		}
	}
	internal class SaintHooks
	{
		internal static void Apply()
		{
			IL.Ghost.Update += Ghost_Update;
			new Hook(typeof(SaveState).GetProperty(nameof(SaveState.CanSeeVoidSpawn), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).GetGetMethod(), SpirituallyEnlightened);
		}

		/// <summary>
		/// Allows <see cref="SlugBaseCharacter"/> to talk to <see cref="Ghost"/> without the mark.
		/// </summary>
		private static void Ghost_Update(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				static bool CanTalkToGhosts(bool isSlugcat, Ghost self)
				{
					return isSlugcat || self.room.game.HasFeature(ExtFeatures.enlightenedState);
				}

				// if (this.room.game.session is StoryGameSession && ((this.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.theMark || (ModManager.MSC && this.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint) || (ModManager.Watcher && this.room.game.StoryCharacter == WatcherEnums.SlugcatStatsName.Watcher)))
				if (cursor.MoveToNextSlugcat(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint))))
				{
					cursor.ImplementILCodeAssumingLdarg0(CanTalkToGhosts);
					// if (this.room.game.session is StoryGameSession && ((this.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.theMark || (ModManager.MSC && this.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint) || (ModManager.Watcher && this.room.game.StoryCharacter == WatcherEnums.SlugcatStatsName.Watcher)))
					if (cursor.MoveToNextSlugcat(typeof(WatcherEnums.SlugcatStatsName).GetField(nameof(WatcherEnums.SlugcatStatsName.Watcher))))
					{
						cursor.ImplementILCodeAssumingLdarg0(CanTalkToGhosts);
					}
				}
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogException(ex);
			}
		}

		/// <summary>
		/// Allows <see cref="SlugBaseCharacter"/> to see void spawn without the mark.
		/// </summary>
		internal static bool SpirituallyEnlightened(Func<SaveState, bool> orig, SaveState save)
		{
			return orig(save) || (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.HasFeature(ExtFeatures.enlightenedState));
		}
	}
}