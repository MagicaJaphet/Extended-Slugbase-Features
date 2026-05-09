using MagicaHookingLibrary.Interfaces;
using MonoMod.RuntimeDetour;
using ExtendedSlugbase.Features.GameRelated;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class OverseerHooks : IOwnHooks
    {

        public void PreApply()
        {
            _ = new Hook(typeof(OverseerGraphics).GetProperty(nameof(OverseerGraphics.MainColor), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod(), OverseerColorOverrides.Implementation.OverseerGraphics_MainColor);
        }
        
        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
