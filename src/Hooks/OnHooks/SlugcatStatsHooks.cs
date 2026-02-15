using MagicaHookingLibrary.Interfaces;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class SlugcatStatsHooks : IOwnHooks
    {
        public void PreApply()
        {
        }


        public void OnApply()
        {
            On.SlugcatStats.HiddenOrUnplayableSlugcat += SlugcatStats_HiddenOrUnplayableSlugcat;
        }

        private bool SlugcatStats_HiddenOrUnplayableSlugcat(On.SlugcatStats.orig_HiddenOrUnplayableSlugcat orig, SlugcatStats.Name i)
        {
            return orig(i) || (i == Plugin.Prototype && !ModOptions.ShowPrototype.Value);
        }

        public void PostApply()
        {
        }
    }
}
