using MagicaHookingLibrary.Interfaces;
using ExtendedSlugbase.Features.GameRelated;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class RoomHooks : IOwnHooks
    {
        public void PreApply()
        {
            On.RegionGate.customOEGateRequirements += UnlockOEGate.Implementation.RegionGate_customOEGateRequirements;
        }

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }

    }
}
