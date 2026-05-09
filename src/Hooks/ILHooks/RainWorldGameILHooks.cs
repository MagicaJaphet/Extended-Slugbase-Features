using MagicaHookingLibrary.Interfaces;
using MonoMod.Cil;
using static MagicaHookingLibrary.Helpers.HookHelpers;
using MagicaHookingLibrary.Helpers;
using ExtendedSlugbase.Features.GameRelated;
using ExtendedSlugbase.Features.PlayerRelated;
namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class RainWorldGameILHooks : IOwnHooks
    {

        public void PreApply()
        {
			IL.RainWorldGame.Update += RainWorldGame_Update;
            IL.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
        }

		private static void RainWorldGame_Update(ILContext il)
		{
			ILCursor c = new(il);

			GetKarmaFromScavs.Implementation.RainWorldGame_Update(c);
        }


        private void RainWorldGame_RawUpdate(ILContext il)
		{
			ILCursor c = new(il);

			static float FramesPerSecondMushroom(float orig, RainWorldGame self)
            {
                if (ObjectInteractions.ObjectInteractibility.lastAteMushroomFPS is float fps)
                {
                    return fps;
                }
                return orig;
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchConvR4(),
                x => x.MatchLdcR4(15)
                ); // float num2 = flag ? Mathf.Lerp((float)this.framesPerSecond, 8f, num) : Mathf.Lerp((float)this.framesPerSecond, 15f, num);
            c.EmitLdarg0Delegate(FramesPerSecondMushroom);
        }

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
