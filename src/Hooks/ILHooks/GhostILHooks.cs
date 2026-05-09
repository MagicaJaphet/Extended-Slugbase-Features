using MagicaHookingLibrary.Interfaces;
using MonoMod.Cil;
using ExtendedSlugbase.Features.GameRelated;

namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class GhostILHooks : IOwnHooks
    {
        public void PreApply()
        {
            IL.Ghost.Update += Ghost_Update;
        }

        // Allows enlightened slugcats to be able to talk to echoes without the mark
        private void Ghost_Update(ILContext il)
        {
			ILCursor c = new(il);

			EnlightenedState.Implementation.Ghost_Update(c);
        }


        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
