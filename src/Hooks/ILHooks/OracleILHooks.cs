using MagicaHookingLibrary.Interfaces;
using MonoMod.Cil;
using ExtendedSlugbase.Features.GameRelated;

namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class OracleILHooks : IOwnHooks
    {
        public void PreApply()
        {
            IL.Oracle.ctor += Oracle_ctor;
        }

        private void Oracle_ctor(ILContext il)
		{
			ILCursor c = new(il);

			EnlightenedState.Implementation.Oracle_ctor(c);
        }


        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
