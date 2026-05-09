using ExtendedSlugbase.Extensions;
using Menu;
using RWCustom;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features.GameRelated;
public class NoContinueAfterAscension() : GameFeature<bool>("no_continue_after_ascending", JsonUtils.ToBool)
{
	internal static class Implementation
	{
		internal static bool Ascended(SlugcatSelectMenu.SlugcatPage page)
		{
			return page is SlugcatSelectMenu.SlugcatPageContinue pageContinue && pageContinue.saveGameData != null && pageContinue.saveGameData.ascended
				&& page.slugcatNumber.TryGetFeature(ExtGameFeatures.NoContinueAfterAscension, out bool noContinue) && noContinue;
		}

		internal static bool Menu_SlugcatSelectMenu_CommunicateWithUpcomingProcess()
		{
			return Custom.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat.TryGetFeature(ExtGameFeatures.NoContinueAfterAscension, out bool noContinue) && noContinue;
		}

		internal static void Menu_SlugcatSelectMenu_ContinueStartedGame(SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter, SlugcatSelectMenu.SlugcatPage page)
		{
			if (page.slugcatNumber.TryGetFeature(ExtGameFeatures.CycleLimit, out var cycleLimit) && page.slugcatImage.sceneID == cycleLimit.DeathSceneID)
			{
				self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(storyGameCharacter, null, self.manager.menuSetup, false);
				self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
				self.PlaySound(SoundID.MENU_Switch_Page_Out);
				return;
			}
		}
	}
}
