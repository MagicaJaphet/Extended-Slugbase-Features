using System.Reflection;
using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using ExtendedSlugbase.Features.GameRelated;
using MagicaHookingLibrary.Interfaces;
using MonoMod.RuntimeDetour;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class SaveStateHooks : IOwnHooks
    {
        public void PreApply()
        {
			On.WinState.TrackerAllowedOnSlugcat += WinState_TrackerAllowedOnSlugcat;
            _ = new Hook(typeof(SaveState).GetProperty(nameof(SaveState.SlowFadeIn), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), CycleLimit.Implementation.SaveState_SlowFadeIn);
            On.PlayerProgression.GetOrInitiateSaveState += PlayerProgression_GetOrInitateSaveState;
			_ = new Hook(typeof(SaveState).GetProperty(nameof(SaveState.CanSeeVoidSpawn), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(), EnlightenedState.Implementation.SaveState_CanSeeVoidSpawn);
        }

		private bool WinState_TrackerAllowedOnSlugcat(On.WinState.orig_TrackerAllowedOnSlugcat orig, WinState.EndgameID trackerId, SlugcatStats.Name slugcat)
		{
			return orig(trackerId, slugcat) && (!slugcat.TryGetFeature(ExtGameFeatures.DisablePassages, out var passage) || !passage.ForbiddenPassages.Contains(trackerId));
		}

		private SaveState PlayerProgression_GetOrInitateSaveState(On.PlayerProgression.orig_GetOrInitiateSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber, RainWorldGame game, ProcessManager.MenuSetup setup, bool saveAsDeathOrQuit)
        {
            var result = orig(self, saveStateNumber, game, setup, saveAsDeathOrQuit);
			HasIDDrone.Implementation.PlayerProgression_GetOrInitateSaveState(saveStateNumber, ref result);

            return result;
        }

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
