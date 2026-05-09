using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using ExtendedSlugbase.Features.GameRelated;
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
			On.Menu.SleepAndDeathScreen.AddPassageButton += SleepAndDeathScreen_AddPassageButton;
			On.Menu.SlugcatSelectMenu.CommunicateWithUpcomingProcess += SlugcatSelectMenu_CommunicateWithUpcomingProcess;
            On.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += SlugcatSelectMenu_SlugcatPageContinue_ctor;
            On.Menu.SlugcatSelectMenu.ContinueStartedGame += SlugcatSelectMenu_ContinueStartedGame;
            On.Menu.SlugcatSelectMenu.UpdateStartButtonText += SlugcatSelectMenu_UpdateStartButtonText;
        }
		private void SleepAndDeathScreen_AddPassageButton(On.Menu.SleepAndDeathScreen.orig_AddPassageButton orig, SleepAndDeathScreen self, bool buttonBlack)
		{
			if (DisablePassages.Implementation.SleepAndDeathScreen_AddPassageButton(self))
			{
				return;
			}

			orig(self, buttonBlack);
		}

		private void SlugcatSelectMenu_CommunicateWithUpcomingProcess(On.Menu.SlugcatSelectMenu.orig_CommunicateWithUpcomingProcess orig, SlugcatSelectMenu self, MainLoopProcess nextProcess)
		{
			if (nextProcess.ID == ProcessManager.ProcessID.Statistics)
			{
				KarmaLadderScreen.SleepDeathScreenDataPackage package = new(self.redSaveState.food, new IntVector2(self.redSaveState.deathPersistentSaveData.karma, self.redSaveState.deathPersistentSaveData.karmaCap), self.redSaveState.deathPersistentSaveData.reinforcedKarma, -1, new Vector2(0f, 0f), null, self.redSaveState, 
					new SlugcatStats(ModManager.MSC 
					|| CycleLimit.Implementation.Menu_SlugcatSelectMenu_CommunicateWithUpcomingProcess()
					|| NoContinueAfterAscension.Implementation.Menu_SlugcatSelectMenu_CommunicateWithUpcomingProcess()
					? self.redSaveState.saveStateNumber : SlugcatStats.Name.Red, false), null, false, false);
				(nextProcess as StoryGameStatisticsScreen).GetDataFromGame(package);
				return;
			}

			orig(self, nextProcess);
		}

		private void SlugcatSelectMenu_SlugcatPageContinue_ctor(On.Menu.SlugcatSelectMenu.SlugcatPageContinue.orig_ctor orig, Menu.SlugcatSelectMenu.SlugcatPageContinue self, Menu.Menu menu, Menu.MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
        {
            orig(self, menu, owner, pageIndex, slugcatNumber);

			CycleLimit.Implementation.Menu_SlugcatSelectMenu_SlugcatPageContinue_ctor(self, menu, owner, pageIndex, slugcatNumber);
        }


        private void SlugcatSelectMenu_ContinueStartedGame(On.Menu.SlugcatSelectMenu.orig_ContinueStartedGame orig, Menu.SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
		{
			SlugcatSelectMenu.SlugcatPage page = self.slugcatPages[self.slugcatPageIndex];

			if (CycleLimit.Implementation.ReachedCycleLimit(page)
				|| NoContinueAfterAscension.Implementation.Ascended(page))
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

			if (CycleLimit.Implementation.ReachedCycleLimit(page)
				|| NoContinueAfterAscension.Implementation.Ascended(page))
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
					if (extCustomScene.ExtImages.Count > customScene.Images.IndexOf(image))
					{
						var extImage = extCustomScene.ExtImages[customScene.Images.IndexOf(image)];
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
							self.depthIllustrations[index].fileName = extImage.BackupImage;
							self.depthIllustrations[index].LoadFile(customScene.SceneFolder);
							if (Futile.atlasManager.DoesContainElementWithName(extImage.BackupImage))
							{
								self.depthIllustrations[index].sprite.SetElementByName(extImage.BackupImage);
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
