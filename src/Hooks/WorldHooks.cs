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
using UnityEngine;
using ExtendedSlugbaseFeatures.Resources;
using static ExtendedSlugbaseFeatures.Helpers.RoomSpecificScriptHelpers;

namespace ExtendedSlugbaseFeatures.Hooks
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
			IL.Room.Loaded += Room_Loaded;
			new Hook(typeof(StoryGameSession).GetProperty(nameof(StoryGameSession.slugPupMaxCount), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod(), SpawnSlugPups);
			new Hook(typeof(OverseerGraphics).GetProperty(nameof(OverseerGraphics.MainColor), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod(), OverseerColorOverride);
		}

		/// <summary>
		/// Allows <see cref="SlugBaseCharacter"/> to set the character's starting position in a room, in the room tiles measurement.
		/// </summary>
		internal static bool RainWorldGame_TryGetPlayerStartPos(On.RainWorldGame.orig_TryGetPlayerStartPos orig, string room, out IntVector2 pos)
		{
			if (Custom.rainWorld.inGameSlugCat != null && SlugBaseCharacter.TryGet(Custom.rainWorld.inGameSlugCat, out var character) && Features.possibleSpawnPositons.TryGet(character, out var startRooms) && startRooms.TryGetValue(room, out pos))
			{
				return pos != null;
			}

			return orig(room, out pos);
		}

		private static void Room_Loaded(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				if (cursor.TryGotoNext(x => x.MatchLdarg(0),
					x => x.MatchCallOrCallvirt(out _),
					x => x.MatchLdcI4(0),
					x => x.MatchStfld<AbstractRoom>(nameof(AbstractRoom.firstTimeRealized))))
				{
					cursor.MoveAfterLabels();
					cursor.Emit(OpCodes.Ldarg_0);
					static void IntroHandler(Room self)
					{
						if (self?.game != null && self.game.GetStorySession?.saveState.cycleNumber == 0 &&
						Features.introCutscene.TryGet(self.game, out var introCutscene) &&
						self.abstractRoom.firstTimeRealized && GameFeatures.StartRoom.TryGet(self.game, out var startRooms) && startRooms.Contains(self.abstractRoom.name)
						&& CustomCutscene.Registry.TryGet(introCutscene, out var cutscene))
						{
							if (cutscene != null)
							{ 
								UnityEngine.Debug.Log("Intro cutscene found!");
								self.AddObject(new ScriptTrigger(self, cutscene));
							}
							else
							{
								Plugin.Logger.LogError($"Could not find cutscene with ID {introCutscene.value}!");
							}
						}
					}
					cursor.EmitDelegate(IntroHandler);
				}
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogException(ex);
			}
		}

		/// <summary>
		/// Allows <see cref="SlugBaseCharacter"/> to spawn slugpups in their campaign.
		/// </summary>
		internal static int SpawnSlugPups(Func<StoryGameSession, int> orig, StoryGameSession self)
		{
			if (ModManager.MSC && self.game != null && Features.maxSlugpupSpawns.TryGet(self.game, out int maxPups))
			{
				return maxPups;
			}

			return orig(self);
		}

		internal static Color OverseerColorOverride(Func<OverseerGraphics, Color> orig, OverseerGraphics self)
		{
			if (!self.overseer.SafariOverseer && !self.overseer.SandboxOverseer && self.overseer.abstractCreature.world.game.HasFeature(Features.overseerOverwrite, out var overrides) && overrides.TryGetValue((self.overseer.abstractCreature.abstractAI as OverseerAbstractAI).ownerIterator, out var overrideColor))
			{
				return overrideColor;
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
						return self.game.HasFeature(Features.canProcessWhiteTokens, false);
					}
					cursor.EmitDelegate(HasBroadcasts);

				}

				// if (this.game.StoryCharacter != SlugcatStats.Name.Red && (!(this.game.session is StoryGameSession) || !(this.game.session as StoryGameSession).saveState.ItemConsumed(this.world, true, this.abstractRoom.index, num21)))
				cursor.MoveToNextSlugcat(typeof(SlugcatStats.Name).GetField(nameof(SlugcatStats.Name.Red)));

				cursor.Emit(OpCodes.Ldarg_0);
				static bool DontSpawnKarmaFlowers(bool isRed, Room self)
				{
					return isRed && (!Features.shouldSpawnKarmaFlowers.TryGet(self.game, out bool canSpawn) || canSpawn);
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
			return orig(self)
				|| (self.room.game.HasFeature(Features.openOEGate, out bool[] flags) && flags[0] 
				&& (flags.Length == 2 && !flags[1] || self.room.game.IsStorySession && (self.room.game.rainWorld.progression.miscProgressionData.beaten_Gourmand || self.room.game.rainWorld.progression.miscProgressionData.beaten_Gourmand_Full || MoreSlugcats.MoreSlugcats.chtUnlockOuterExpanse.Value)));
		}
	}
	internal class SaintHooks
	{
		internal static void Apply()
		{
			On.HUD.RainMeter.Update += RainMeter_Update;
			On.HUD.RainMeter.ctor += RainMeter_ctor;
			On.HUD.RainMeter.Draw += RainMeter_Draw;
			IL.Ghost.Update += Ghost_Update;
			new Hook(typeof(SaveState).GetProperty(nameof(SaveState.CanSeeVoidSpawn), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).GetGetMethod(), SpirituallyEnlightened);
		}

		/// <summary>
		/// Fixes Slugbase issue where the <see cref="SlugcatStats.Timeline.Saint"/> doesn't recognize <see cref="SlugBaseCharacter"/>'s timeline point.
		/// </summary>
		private static void RainMeter_Update(On.HUD.RainMeter.orig_Update orig, HUD.RainMeter self)
		{
			if (ModManager.MSC && self.hud.owner.GetOwnerType() == HUD.HUD.OwnerType.Player && (self.hud.owner as Player).abstractCreature.world.game.TimelinePoint == SlugcatStats.Timeline.Saint && self.hud.map.RegionName != "HR")
			{
				self.halfTimeShown = true;
			}
			orig(self);
		}

		/// <summary>
		/// Fixes Slugbase issue where the <see cref="SlugcatStats.Timeline.Saint"/> doesn't recognize <see cref="SlugBaseCharacter"/>'s timeline point.
		/// </summary>
		private static void RainMeter_ctor(On.HUD.RainMeter.orig_ctor orig, HUD.RainMeter self, HUD.HUD hud, FContainer fContainer)
		{
			orig(self, hud, fContainer);
			if (ModManager.MSC && self.hud.owner.GetOwnerType() == HUD.HUD.OwnerType.Player && (self.hud.owner as Player).abstractCreature.world.game.TimelinePoint == SlugcatStats.Timeline.Saint && self.hud.map.RegionName != "HR")
			{
				self.halfTimeShown = true;
			}
		}

		/// <summary>
		/// Fixes Slugbase issue where the <see cref="SlugcatStats.Timeline.Saint"/> doesn't recognize <see cref="SlugBaseCharacter"/>'s timeline point.
		/// </summary>
		private static void RainMeter_Draw(On.HUD.RainMeter.orig_Draw orig, HUD.RainMeter self, float timeStacker)
		{
			if (ModManager.MSC && self.hud.owner.GetOwnerType() == HUD.HUD.OwnerType.Player && (self.hud.owner as Player).abstractCreature.world.game.TimelinePoint == SlugcatStats.Timeline.Saint && self.hud.map.RegionName != "HR")
			{
				return;
			}
			orig(self, timeStacker);
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
					return isSlugcat || self.room.game.HasFeature(Features.enlightenedState);
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
			return orig(save) || Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.HasFeature(Features.enlightenedState);
		}
	}
}