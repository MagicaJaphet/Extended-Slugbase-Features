using System;
using ExtendedSlugbase.Features;
using ExtendedSlugbase.Helpers;
using MagicaHookingLibrary.Interfaces;
using MonoMod.RuntimeDetour;
using RWCustom;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class StoryGameSessiOn : IOwnHooks
    {
        public void PreApply()
        {
            _ = new Hook(typeof(StoryGameSession).GetProperty(nameof(StoryGameSession.RedIsOutOfCycles), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod(), RedIsOutOfCycles);
			_ = new Hook(typeof(StoryGameSession).GetProperty(nameof(StoryGameSession.slugPupMaxCount), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod(), SpawnSlugPups);
        }

        private static bool RedIsOutOfCycles(Func<StoryGameSession, bool> orig, StoryGameSession self)
        {
            if (self.game.TryGetFeature(GameFeaturesExt.cycleLimit, out var hardMode))
            {
                return !Custom.rainWorld.ExpeditionMode && self.saveState.cycleNumber >= hardMode.Cycles;
            }
            return orig(self);
        }
        
        // Max slugpup spawns
        private static int SpawnSlugPups(Func<StoryGameSession, int> orig, StoryGameSession self)
		{
			if (self.game != null && self.game.TryGetFeature(GameFeaturesExt.maxSlugpupSpawns, out int maxPups))
			{
				return maxPups;
			}

			return orig(self);
		}

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
