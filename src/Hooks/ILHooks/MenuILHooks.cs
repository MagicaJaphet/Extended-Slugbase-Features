using MagicaHookingLibrary.Interfaces;
using MonoMod.Cil;
using ExtendedSlugbase.Features.GameRelated;
using System;
using MoreSlugcats;

namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class MenuILHooks : IOwnHooks
    {
        public void PreApply()
		{
			IL.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;
            IL.Menu.SlugcatSelectMenu.SlugcatPage.GrafUpdate += Menu_SlugcatSelectMenu_SlugcatPage_GrafUpdate;
        }

		private void SleepAndDeathScreen_GetDataFromGame(ILContext il)
		{
			ILCursor c = new(il);

			DisablePassages.Implementation.SleepAndDeathScreen_GetDataFromGame(c);
		}

		private static void Menu_SlugcatSelectMenu_SlugcatPage_GrafUpdate(ILContext il)
		{
			ILCursor c = new(il);

			RevealMarkOverCycles.Implementation.Menu_SlugcatSelectMenu_SlugcatPage_GrafUpdate(c);
        }

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
