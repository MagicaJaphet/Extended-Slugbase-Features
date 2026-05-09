using ExtendedSlugbase.Features.GameRelated;
using MagicaHookingLibrary.Interfaces;

namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class ProcessManagerILHooks : IOwnHooks
    {
        public void PreApply()
        {
            IL.ProcessManager.CreateValidationLabel += CycleLimit.Implementation.ProcessManager_CreateValidationLabel;
        }


        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
