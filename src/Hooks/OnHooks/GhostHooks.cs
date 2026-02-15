using System;
using MagicaHookingLibrary.Interfaces;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class GhostHooks : IOwnHooks
    {
        public void PreApply()
        {
            On.MoreSlugcats.GhostPing.Update += GhostPing_Update;
        }

        // Patch ghost pings so they don't play if the game isn't loaded lmao
        private void GhostPing_Update(On.MoreSlugcats.GhostPing.orig_Update orig, MoreSlugcats.GhostPing self, bool eu)
        {
            if (self.room?.game?.manager.loadingLabel == null)
            {
                orig(self, eu);
            }
        }


        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
