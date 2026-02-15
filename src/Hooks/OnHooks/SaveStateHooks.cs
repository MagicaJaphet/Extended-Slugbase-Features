using System;
using System.Reflection;
using ExtendedSlugbase.Features;
using ExtendedSlugbase.Helpers;
using MagicaHookingLibrary.Interfaces;
using MonoMod.RuntimeDetour;
using RWCustom;
using UnityEngine;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class SaveStateHooks : IOwnHooks
    {
        public void PreApply()
        {
            _ = new Hook(typeof(SaveState).GetProperty(nameof(SaveState.SlowFadeIn), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), SaveState_SlowFadeIn);
            On.PlayerProgression.GetOrInitiateSaveState += PlayerProgression_GetOrInitateSaveState;
			_ = new Hook(typeof(SaveState).GetProperty(nameof(SaveState.CanSeeVoidSpawn), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).GetGetMethod(), SpirituallyEnlightened);
        }

        private float SaveState_SlowFadeIn(Func<SaveState, float> orig, SaveState self)
        {
            if (self.saveStateNumber.TryGetFeature(GameFeaturesExt.cycleLimit, out var cycleLimit))
            {
                return Mathf.Max(self.malnourished ? 4f : 0.8f, (self.cycleNumber >= cycleLimit.Cycles && !Custom.rainWorld.ExpeditionMode) ? Custom.LerpMap((float)self.cycleNumber, (float)cycleLimit.Cycles, (float)(cycleLimit.Cycles + 5), 4f, 15f) : 0.8f);
            }
            return orig(self);
        }

        private SaveState PlayerProgression_GetOrInitateSaveState(On.PlayerProgression.orig_GetOrInitiateSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber, RainWorldGame game, ProcessManager.MenuSetup setup, bool saveAsDeathOrQuit)
        {
            var result = orig(self, saveStateNumber, game, setup, saveAsDeathOrQuit);
            if (saveStateNumber.TryGetFeature(GameFeaturesExt.hasIDDrone, out bool hasID))
            {
                result.hasRobo = hasID;
            }

            return result;
        }

        // Allows enlightened scugs to see void spawn without the glow

        private static bool SpirituallyEnlightened(Func<SaveState, bool> orig, SaveState save)
		{
			return orig(save) || (save.saveStateNumber.TryGetFeature(GameFeaturesExt.enlightenedState, out bool enlightened) && enlightened);
		}

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
