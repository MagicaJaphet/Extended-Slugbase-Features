using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using MagicaHookingLibrary.Helpers;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using SlugBase;
using SlugBase.Assets;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtendedSlugbase.Features.GameRelated;
public class CycleLimit() : GameFeature<CycleLimit.HardMode>("limited_cycles", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static HardMode Factory(JsonAny json) => new(json);

	/// <summary>
	/// JSON Object class to store cycle limit information.
	/// </summary>
	public class HardMode
	{
		public int Cycles { get; } = 20;
		public MenuScene.SceneID DeathSceneID { get; } = MenuScene.SceneID.Slugcat_Dead_Red;
		
		// TODO: Implement
		public bool HardLimit { get; } = false;
		
		// LATER: Implement
		public int BonusCycles { get; } = 5;

		public HardMode(JsonAny json)
		{
			if (json.TryParse(out JsonObject obj))
			{
				if (obj.TryGet("cycles", out int[] cycles, 1, 2))
				{
					Cycles = cycles[0];
					if (cycles.Length > 1)
					{
						BonusCycles = cycles[1];
					}
				}
				if (obj.TryGet("death_menu_scene", out MenuScene.SceneID deathSceneID))
				{
					DeathSceneID = deathSceneID;
				}
				if (obj.TryGet("hard_cycle_limit", out bool hardLimit))
				{
					HardLimit = hardLimit;
				}
			}
		}
	}

	internal static class Implementation
	{
		/// <summary>
		/// Corrects <see cref="SlugcatStats.Name.Red"/> check.
		/// </summary>
		internal static void HUD_Map_Update(ILContext il)
		{
			ILCursor c = new(il);

			static bool LimitedCycles(bool isRed, HUD.Map self)
			{
				return isRed || (self.hud.owner as Player).abstractCreature.world.game.TryGetFeature(ExtGameFeatures.CycleLimit, out var hardMode);
			}

			c.TryMoveToNextSlugcatBool(
				nameof(SlugcatStats.Name.Red).GetSlugcatFieldInfo()
				);
			c.EmitLdarg0Delegate(LimitedCycles);
		}

		/// <summary>
		/// Updates the cycle number displayed when viewing the map.
		/// </summary>
		internal static void HUD_Map_CycleLabel_UpdateCycleText(HUD.Map.CycleLabel self)
		{
			if (self.owner.hud.owner is Player player && player.abstractCreature.world.game.TryGetFeature(ExtGameFeatures.CycleLimit, out var hardMode))
			{
				int cycles = hardMode.Cycles - player.abstractCreature.world.game.GetStorySession.saveState.cycleNumber;

				self.red = cycles <= 0 ? 1 : -1;
				self.label.text = $"{self.owner.hud.rainWorld.inGameTranslator.Translate("Cycle")} {cycles}";
			}
		}

		/// <summary>
		/// Updates the cycle number displayed on the bottom of the screen.
		/// </summary>
		internal static void HUD_SubregionTracker_Update(ILContext il)
		{
			ILCursor c = new(il);

			static int CycleLimit(int cycle, HUD.SubregionTracker self)
			{
				if (!Custom.rainWorld.ExpeditionMode && self.textPrompt.hud.owner is Player player && player.abstractCreature.world.game.TryGetFeature(ExtGameFeatures.CycleLimit, out var hardMode))
				{
					return hardMode.Cycles - player.abstractCreature.world.game.GetStorySession.saveState.cycleNumber;
				}
				return cycle;
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchStloc(5)
				);
			c.GotoNext(
				MoveType.After,
				x => x.MatchStloc(5)
				);

			c.MoveAfterLabels();
			c.Emit(OpCodes.Ldloc, 5);
			c.EmitLdarg0Delegate(CycleLimit);
			c.Emit(OpCodes.Stloc, 5);
		}

		/// <summary>
		/// Sends game data to the statistics screen after the slugcat has died.
		/// </summary>
		internal static bool Menu_SlugcatSelectMenu_CommunicateWithUpcomingProcess()
		{
			return Custom.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat.TryGetFeature(ExtGameFeatures.CycleLimit, out _);
		}

		/// <summary>
		/// Updates cycle number on slugcats who have cycle limits.
		/// </summary>
		internal static void Menu_SlugcatSelectMenu_SlugcatPageContinue_ctor(SlugcatSelectMenu.SlugcatPageContinue self, Menu.Menu menu, MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
		{
			if (slugcatNumber.TryGetFeature(ExtGameFeatures.CycleLimit, out var cycleLimit))
			{
				string text = Region.GetRegionFullName(self.saveGameData.shelterName.Substring(0, self.saveGameData.shelterName.IndexOf("_")), slugcatNumber);
				if (text.Length > 0)
				{
					text = menu.Translate(text);
					text = string.Concat(
					[
						text,
						" - ",
						menu.Translate("Cycle"),
						" ",
						(cycleLimit.Cycles - self.saveGameData.cycle).ToString()
					]);
					SpeedRunTimer.CampaignTimeTracker campaignTimeTracker = SpeedRunTimer.GetCampaignTimeTracker(slugcatNumber);
					if (campaignTimeTracker != null)
					{
						if (campaignTimeTracker.TotalFreeTime == 0.0 || campaignTimeTracker.TotalFixedTime == 0.0)
						{
							campaignTimeTracker.LoadOldTimings(self.saveGameData.gameTimeAlive, self.saveGameData.gameTimeDead);
						}
						if (ModManager.MMF)
						{
							text = text + " (" + campaignTimeTracker.TotalFreeTimeSpan.GetIGTFormat(MMF.cfgSpeedrunTimer.Value || menu.manager.rainWorld.options.validation) + ")";
						}
					}
				}
				self.regionLabel.text = text;
			}
		}

		/// <summary>
		/// Initializes the illness effect <see cref="SlugcatStats.Name.Red"/> uses when near or over the cycle limit.
		/// </summary>
		/// <param name="self"></param>
		internal static void Player_ctor(Player self)
		{
			if (!Custom.rainWorld.ExpeditionMode && self.abstractCreature.world.game.IsStorySession && self.abstractCreature.world.game.TryGetFeature(ExtGameFeatures.CycleLimit, out var hardMode) && self.abstractCreature.world.game.GetStorySession.RedIsOutOfCycles)
			{
				self.redsIllness = new(self, hardMode.Cycles - self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber);
			}
		}

		/// <summary>
		/// Corrects number of cycles in the loading screen validation label.
		/// </summary>
		internal static void ProcessManager_CreateValidationLabel(ILContext il)
		{
			ILCursor c = new(il);

			static int CycleNumber(int orig, ProcessManager self, SlugcatSelectMenu.SaveGameData saveData)
			{
				if (self.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat.TryGetFeature(ExtGameFeatures.CycleLimit, out var cycleLimit))
				{
					return cycleLimit.Cycles - saveData.cycle;
				}
				return orig;
			}

			c.GotoNext(x => x.MatchStloc(3));
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloc, 2);
			c.EmitDelegate(CycleNumber);
		}

		/// <summary>
		/// Updates the select screen art to indicate that the slugcat has died.
		/// </summary>
		internal static void RainWorldGame_GoToRedsGameOver(RainWorldGame self)
		{
			if (self.Players[0].realizedCreature is Player red)
			{
				if (red.redsIllness != null)
				{
					red.redsIllness.fadeOutSlow = true;
				}
			}
			if (self.TryGetFeature(ExtGameFeatures.CycleLimit, out var hardMode))
			{
				CustomScene.SetSelectMenuScene(self.GetStorySession.saveState, hardMode.DeathSceneID);
				if (ModManager.CoopAvailable)
				{
					int num = 0;
					using IEnumerator<Player> enumerator = (from x in self.Players select x.realizedCreature as Player).GetEnumerator();
					while (enumerator.MoveNext())
					{
						Player player = enumerator.Current;
						self.GetStorySession.saveState.AppendCycleToStatistics(player, self.GetStorySession, true, num);
						num++;
					}
				}
				else
					self.GetStorySession.saveState.AppendCycleToStatistics(self.Players[0].realizedCreature as Player, self.GetStorySession, true, 0);
			}
		}

		internal static bool ReachedCycleLimit(SlugcatSelectMenu.SlugcatPage page)
		{
			return page.slugcatNumber.TryGetFeature(ExtGameFeatures.CycleLimit, out var cycleLimit) && page.slugcatImage.sceneID == cycleLimit.DeathSceneID;
		}

		/// <summary>
		/// Use slow fade in that <see cref="SlugcatStats.Name.Red"/> uses when over cycle limit.
		/// </summary>
		internal static float SaveState_SlowFadeIn(Func<SaveState, float> orig, SaveState self)
		{
			if (self.saveStateNumber.TryGetFeature(ExtGameFeatures.CycleLimit, out var cycleLimit))
			{
				return Mathf.Max(self.malnourished ? 4f : 0.8f, self.cycleNumber >= cycleLimit.Cycles && !Custom.rainWorld.ExpeditionMode ? Custom.LerpMap(self.cycleNumber, cycleLimit.Cycles, cycleLimit.Cycles + 5, 4f, 15f) : 0.8f);
			}
			return orig(self);
		}

		/// <summary>
		/// Tells the game when dying that the slugcat is out of cycles.
		/// </summary>
		internal static bool StoryGameSession_RedIsOutOfCycles(Func<StoryGameSession, bool> orig, StoryGameSession self)
		{
			if (self.game.TryGetFeature(ExtGameFeatures.CycleLimit, out var hardMode))
			{
				return !Custom.rainWorld.ExpeditionMode && self.saveState.cycleNumber >= hardMode.Cycles;
			}
			return orig(self);
		}
	}
}