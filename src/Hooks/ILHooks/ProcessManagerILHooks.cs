using System;
using ExtendedSlugbase.Features;
using ExtendedSlugbase.Helpers;
using MagicaHookingLibrary.Interfaces;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using static MagicaHookingLibrary.Helpers.HookHelpers;

namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class ProcessManagerILHooks : IOwnHooks
    {
        public void PreApply()
        {
            IL.ProcessManager.CreateValidationLabel += ILAction(ProcessManager_CreateValidationLabel);
        }

        private void ProcessManager_CreateValidationLabel(ILCursor c)
        {
            static int CycleNumber(int orig, ProcessManager self, SlugcatSelectMenu.SaveGameData saveData)
            {
                if (self.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat.TryGetFeature(GameFeaturesExt.cycleLimit, out var cycleLimit))
                {
                    return cycleLimit.Cycles - saveData.cycle;
                }
                return orig;
            }
            
            c.GotoNext(x => x.MatchStloc(3));
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 2);
            c.EmitDelegate(CycleNumber);
        }


        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
