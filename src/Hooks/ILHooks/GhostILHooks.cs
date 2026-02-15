using ExtendedSlugbase.Features;
using ExtendedSlugbase.Helpers;
using MagicaHookingLibrary.Interfaces;
using static MagicaHookingLibrary.Helpers.HookHelpers;
using MonoMod.Cil;

namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class GhostILHooks : IOwnHooks
    {
        public void PreApply()
        {
            IL.Ghost.Update += ILAction(Ghost_Update);
        }

        // Allows enlightened slugcats to be able to talk to echoes without the mark
        private void Ghost_Update(ILCursor c)
        {
            static bool CanTalkToGhosts(bool hasMark, Ghost self)
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
