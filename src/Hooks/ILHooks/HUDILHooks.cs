using System;
using ExtendedSlugbase.Features;
using ExtendedSlugbase.Helpers;
using MagicaHookingLibrary.Helpers;
using MagicaHookingLibrary.Interfaces;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using static MagicaHookingLibrary.Helpers.HookHelpers;

namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class HUDILHooks : IOwnHooks
    {
        public void PreApply()
        {
			IL.HUD.Map.Update += ILAction(Map_Update);
            IL.HUD.SubregionTracker.Update += ILAction(SubregionTracker_Update);
        }

        private void Map_Update(ILCursor c)
        {
            static bool LimitedCycles(bool isRed, HUD.Map self)
            {
                return isRed || (self.hud.owner as Player).abstractCreature.world.game.TryGetFeature(GameFeaturesExt.cycleLimit, out var hardMode);
            }

            c.TryMoveToNextSlugcatBool(
                nameof(SlugcatStats.Name.Red).GetSlugcatFieldInfo()
                );
            c.EmitLdarg0Delegate(LimitedCycles);
        }

        private void SubregionTracker_Update(ILCursor c)
        {
            static int CycleLimit(int cycle, HUD.SubregionTracker self)
            {
                if (!Custom.rainWorld.ExpeditionMode && self.textPrompt.hud.owner is Player player && player.abstractCreature.world.game.TryGetFeature(GameFeaturesExt.cycleLimit, out var hardMode))
                {
                    return hardMode.Cycles - player.abstractCreature.world.game.GetStorySession.saveState.cycleNumber;
                }
                return cycle;
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchStloc(5)
                );
            c.GotoNext(
                MoveType.After,
                x => x.MatchStloc(5)
                );
            
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldloc, 5);
            c.EmitLdarg0Delegate(CycleLimit);
            c.Emit(OpCodes.Stloc, 5);
        }

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }


    }
}
