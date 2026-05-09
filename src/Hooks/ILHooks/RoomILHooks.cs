using MagicaHookingLibrary.Interfaces;
using MonoMod.Cil;
using ExtendedSlugbase.Features.GameRelated;

namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class RoomILHooks : IOwnHooks
    {
        public void PreApply()
        {
            IL.ShelterDoor.Update += ShelterDoor_Update;
            IL.Room.Loaded += Room_Loaded;
        }

        private void ShelterDoor_Update(ILContext il)
		{
			ILCursor c = new(il);

			HasGhostPing.Implementation.ShelterDoor_Update(c);
        }

        private static void Room_Loaded(ILContext il)
		{
			ILCursor c = new(il);

			CanProcessBroadcasts.Implementation.Room_Loaded(c);
        }
        

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }

    }
}
