using ExtendedSlugbase.Features;
using ExtendedSlugbase.Helpers;
using MagicaHookingLibrary.Interfaces;
using Menu;
using MoreSlugcats;
using RWCustom;
using SlugBase.Assets;
using SlugBase.Features;
using System.Linq;
using UnityEngine;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class MenuHooks : IOwnHooks
    {
        public void PreApply()
        {
			On.Menu.SlugcatSelectMenu.CommunicateWithUpcomingProcess += SlugcatSelectMenu_CommunicateWithUpcomingProcess;
            On.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += SlugcatSelectMenu_SlugcatPageContinue_ctor;
            On.Menu.SlugcatSelectMenu.ContinueStartedGame += SlugcatSelectMenu_ContinueStartedGame;
            On.Menu.SlugcatSelectMenu.UpdateStartButtonText += SlugcatSelectMenu_UpdateStartButtonText;
        }

		private void SlugcatSelectMenu_CommunicateWithUpcomingProcess(On.Menu.SlugcatSelectMenu.orig_CommunicateWithUpcomingProcess orig, SlugcatSelectMenu self, MainLoopProcess nextProcess)
		{
			if (nextProcess.ID == ProcessManager.ProcessID.Statistics)
			{
				KarmaLadderScreen.SleepDeathScreenDataPackage package = new(self.redSaveState.food, new IntVector2(self.redSaveState.deathPersistentSaveData.karma, self.redSaveState.deathPersistentSaveData.karmaCap), self.redSaveState.deathPersistentSaveData.reinforcedKarma, -1, new Vector2(0f, 0f), null, self.redSaveState, new SlugcatStats(ModManager.MSC || Custom.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat.TryGetFeature(GameFeaturesExt.cycleLimit, out _) ? self.redSaveState.saveStateNumber : SlugcatStats.Name.Red, false), null, false, false);
				(nextProcess as StoryGameStatisticsScreen).GetDataFromGame(package);
				return;
			}

			orig(self, nextProcess);
		}

		private void SlugcatSelectMenu_SlugcatPageContinue_ctor(On.Menu.SlugcatSelectMenu.SlugcatPageContinue.orig_ctor orig, Menu.SlugcatSelectMenu.SlugcatPageContinue self, Menu.Menu menu, Menu.MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
        {
            orig(self, menu, owner, pageIndex, slugcatNumber);

            if (slugcatNumber.TryGetFeature(GameFeaturesExt.cycleLimit, out var cycleLimit))
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


        private void SlugcatSelectMenu_ContinueStartedGame(On.Menu.SlugcatSelectMenu.orig_ContinueStartedGame orig, Menu.SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
        {
			SlugcatSelectMenu.SlugcatPage page = self.slugcatPages[self.slugcatPageIndex];
			if (page.slugcatNumber.TryGetFeature(GameFeaturesExt.cycleLimit, out var cycleLimit) && page.slugcatImage.sceneID == cycleLimit.DeathSceneID)
            {
                self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(storyGameCharacter, null, self.manager.menuSetup, false);
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
                self.PlaySound(SoundID.MENU_Switch_Page_Out);
                return;
            }
            orig(self, storyGameCharacter);
        }

        private void SlugcatSelectMenu_UpdateStartButtonText(On.Menu.SlugcatSelectMenu.orig_UpdateStartButtonText orig, Menu.SlugcatSelectMenu self)
        {
            orig(self);

			SlugcatSelectMenu.SlugcatPage page = self.slugcatPages[self.slugcatPageIndex];
			if (page.slugcatNumber.TryGetFeature(GameFeaturesExt.cycleLimit, out var cycleLimit) && page.slugcatImage.sceneID == cycleLimit.DeathSceneID)
            {
                self.startButton.menuLabel.text = self.Translate("STATISTICS");
            }
        }


        public void OnApply()
		{
			On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;
		}

		private void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
		{
			orig(self);

			if (CustomScene.Registry.TryGet(self.sceneID, out var customScene) && customScene.TryGetExtCustomScene(out var extCustomScene))
			{
				// Use the custom menuscene type if the mod is installed
				foreach (var image in customScene.Images)
				{
					if (extCustomScene.ExtImages.TryGetValue(image, out var extImage))
					{
						if (Plugin.ExtendedMenuScenes && (extImage.SlotIndex != null || extImage.SlotName != null || extImage.ImageColor != null))
						{
							bool hasSlots = Custom.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat.TryGetFeature(PlayerFeatures.CustomColors, out var slots);
							int? index = null;
							Color? color = null;
							if (extImage.SlotName != null && hasSlots)
							{
								index = slots.FirstOrDefault(x => string.Compare(x.Name, extImage.SlotName, true) == 0)?.Index;
								if (index != null)
									color = slots[index.Value].GetSlotColor(null, null);
							}
							else if (extImage.SlotIndex is int extIndex && hasSlots)
							{
								index = extIndex;
								color = slots[index.Value].GetSlotColor(null, null);
							}
							else
							{
								color = extImage.ImageColor;
							}
							ExternalWrappers.ExtendedMenuscenes.TryApplyColoredMenuIllustration(self, image, color, extImage.Opacity, index, Custom.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat);
						}
						else if (!string.IsNullOrEmpty(extImage.BackupImage))
						{
							int index = self.depthIllustrations.IndexOf(self.depthIllustrations.Find(x => x.fileName.Contains(image.Name)));
							self.depthIllustrations[index] = new(self.menu, self, customScene.SceneFolder, extImage.BackupImage, image.Position, image.Depth, image.Shader);
							for (int i = 0; i < self.depthIllustrations.Count; i++)
							{
								self.depthIllustrations[i].sprite.MoveToFront();
							}
						}
					}
				}
			}
		}

		public void PostApply()
        {
        }
    }
}
