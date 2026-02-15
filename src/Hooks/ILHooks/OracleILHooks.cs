using System;
using MagicaHookingLibrary.Interfaces;
using static MagicaHookingLibrary.Helpers.HookHelpers;
using MonoMod.Cil;
using ExtendedSlugbase.Features;
using Mono.Cecil.Cil;
using ExtendedSlugbase.Helpers;

namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class OracleILHooks : IOwnHooks
    {
        public void PreApply()
        {
            IL.Oracle.ctor += ILAction(Oracle_ctor);
        }

        // Uses SLOracleBehaviorHasMark if the slugcat is enlightened
        private void Oracle_ctor(ILCursor c)
        {
            static bool CanTalkToGhosts(bool hasMark, Oracle self)
            {
                return hasMark || (self.room.game.StoryCharacter.TryGetFeature(GameFeaturesExt.enlightenedState, out bool enlightened) && enlightened);
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdfld(typeof(DeathPersistentSaveData).GetField(nameof(DeathPersistentSaveData.theMark)))
                ); // if (this.room.game.session is StoryGameSession && ((this.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.theMark
            c.EmitLdarg0Delegate(CanTalkToGhosts);
        }


        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
