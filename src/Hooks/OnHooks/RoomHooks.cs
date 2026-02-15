using System;
using BepInEx.Logging;
using ExtendedSlugbase.Features;
using ExtendedSlugbase.Helpers;
using MagicaHookingLibrary.Helpers;
using static MagicaHookingLibrary.Helpers.HookHelpers;
using MagicaHookingLibrary.Interfaces;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class RoomHooks : IOwnHooks
    {
        public void PreApply()
        {
            On.RegionGate.customOEGateRequirements += RegionGate_customOEGateRequirements;
        }

        // Open OE Gate
        private static bool RegionGate_customOEGateRequirements(On.RegionGate.orig_customOEGateRequirements orig, RegionGate self)
		{
            bool gourmandUnlockedOE = self.room.game.IsStorySession && (self.room.game.rainWorld.progression.miscProgressionData.beaten_Gourmand || self.room.game.rainWorld.progression.miscProgressionData.beaten_Gourmand_Full || MoreSlugcats.MoreSlugcats.chtUnlockOuterExpanse.Value);
			return orig(self) || (self.room.game.TryGetFeature(GameFeaturesExt.openOEGate, out bool[] flags) && flags[0] && (!(flags.Length == 2 && flags[1]) || gourmandUnlockedOE));
		}
        

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }

    }
}
