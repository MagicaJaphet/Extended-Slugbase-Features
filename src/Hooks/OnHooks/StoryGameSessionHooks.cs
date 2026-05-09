using ExtendedSlugbase.Features.GameRelated;
using MagicaHookingLibrary.Interfaces;
using MonoMod.RuntimeDetour;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class StoryGameSessiOn : IOwnHooks
    {
        public void PreApply()
        {
            _ = new Hook(typeof(StoryGameSession).GetProperty(nameof(StoryGameSession.RedIsOutOfCycles), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod(), CycleLimit.Implementation.StoryGameSession_RedIsOutOfCycles);
			_ = new Hook(typeof(StoryGameSession).GetProperty(nameof(StoryGameSession.slugPupMaxCount), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod(), MaxSlugpupSpawns.Implementation.StoryGameSession_slugPupMaxCount);
        }

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
