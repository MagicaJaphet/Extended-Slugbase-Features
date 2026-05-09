using MagicaHookingLibrary.Interfaces;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using ExtendedSlugbase.Features.PlayerRelated;
namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class PlayerGraphicsILHooks : IOwnHooks
    {
        public void PreApply()
        {
            IL.PlayerGraphics.Update += CanCraftObjects.Implementation.PlayerGraphics_Update;
            IL.PlayerGraphics.AxolotlGills.ctor += RivuletGills.Implementation.PlayerGraphics_AxolotlGills_ctor;
            IL.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            IL.PlayerGraphics.AxolotlScale.Update += RivuletGills.Implementation.PlayerGraphics_AxolotlScale_Update;
			IL.PlayerGraphics.MSCUpdate += PlayerGraphics_MSCUpdate;
            IL.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
			IL.PlayerGraphics.TailSpeckles.ctor += CanCreateSpears.Implementation.PlayerGraphics_TailSpeckles_ctor;
        }

        private void PlayerGraphics_InitiateSprites(ILContext il)
		{
			ILCursor c = new(il);

			static void AddMoreSprites(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                RivuletGills.Implementation.PlayerGraphics_InitiateSprites(self, sLeaser, rCam);

				CanCreateSpears.Implementation.PlayerGraphics_InitiateSprites(self, sLeaser, rCam);

				SaintTongue.Implementation.PlayerGraphics_InitiateSprites(self, sLeaser);
            }

            c.GotoNext(
                x => x.MatchCallOrCallvirt(typeof(GraphicsModule).GetMethod(nameof(GraphicsModule.AddToContainer)))
                ); // this.AddToContainer(sLeaser, rCam, null);
            c.GotoPrev(
                MoveType.AfterLabel,
                x => x.MatchLdarg(0)
                ); // BEFORE: this.AddToContainer(sLeaser, rCam, null);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.Emit(OpCodes.Ldarg_2);
            c.EmitDelegate(AddMoreSprites);
        }
        

        private void PlayerGraphics_MSCUpdate(ILContext il)
		{
			ILCursor c = new(il);

			SaintTongue.Implementation.PlayerGraphics_MSCUpdate(c);
        }

        private void PlayerGraphics_DrawSprites(ILContext il)
		{
			ILCursor c = new(il);

			SaintTongue.Implementation.PlayerGraphics_DrawSprites(c, il);
        }

        public void OnApply()
        {
        }


        public void PostApply()
        {
        }
    }
}
