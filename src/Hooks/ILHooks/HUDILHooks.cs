using ExtendedSlugbase.Features.GameRelated;
using MagicaHookingLibrary.Interfaces;

namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class HUDILHooks : IOwnHooks
    {
		public void PreApply()
		{
			IL.HUD.Map.Update += CycleLimit.Implementation.HUD_Map_Update;
			IL.HUD.SubregionTracker.Update += CycleLimit.Implementation.HUD_SubregionTracker_Update;
		}

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }


    }
}
