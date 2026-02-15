using ExtendedSlugbase.Features;
using ExtendedSlugbase.Helpers;
using MagicaHookingLibrary.Interfaces;
using static MagicaHookingLibrary.Helpers.HookHelpers;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;
using System;

namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class MenuILHooks : IOwnHooks
    {
        public void PreApply()
        {
            IL.Menu.SlugcatSelectMenu.SlugcatPage.GrafUpdate += ILAction(MarkFadeOnMenu);
        }

        // Reveal Mark Over Total Cycles: Implement alpha fade on menu (Surprisingly Rivulet doesn't do this???)
        private static void MarkFadeOnMenu(ILCursor c)
        {
            static float MarkFadeOnMenu(SlugcatSelectMenu.SlugcatPage self, float markAlpha)
            {
                float mult = 0f;
                if (self is SlugcatSelectMenu.SlugcatPageContinue page
                && self.slugcatNumber.TryGetFeature(GameFeaturesExt.revealMarkOverTotalCycles, out int cycles)) 
                {
                    mult = Mathf.Pow(Mathf.InverseLerp(4f, cycles, page.saveGameData.cycle), 3.5f);
                }
                markAlpha *= mult;
                return markAlpha;
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchMul(),
                x => x.MatchStloc(2),
                x => x.MatchLdarg(0)
                ); // AFTER: num3 *= ((this is SlugcatSelectMenu.SlugcatPageContinue) ? Mathf.Pow(Mathf.InverseLerp(4f, 14f, (float)(this as SlugcatSelectMenu.SlugcatPageContinue).saveGameData.cycle), 3.5f) : 0f);
            c.Emit(OpCodes.Ldloc, 2);
            c.EmitDelegate(MarkFadeOnMenu);
            c.Emit(OpCodes.Stloc, 2);
            c.Emit(OpCodes.Ldarg_0); // Place back onto the stack
        }

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
