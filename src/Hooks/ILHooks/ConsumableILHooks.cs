using MagicaHookingLibrary.Interfaces;
using ExtendedSlugbase.Features.PlayerRelated;
namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class ConsumableILHooks : IOwnHooks
    {
        public void PreApply()
		{
			IL.Pomegranate.Update += CanCreateSpears.Implementation.Pomegranate_Update;
			IL.SeedCob.Update += CanCreateSpears.Implementation.SeedCob_Update;
			IL.SeedCob.HitByWeapon += CanCreateSpears.Implementation.SeedCob_HitByWeapon;
            IL.BubbleGrass.Update += ObjectInteractions.Implementation.BubbleGrass_Update;
            IL.WaterNut.Update += ObjectInteractions.Implementation.WaterNut_Update;
            IL.Explosion.Update += ObjectInteractions.Implementation.Explosion_Update;
            IL.Mushroom.BitByPlayer += ObjectInteractions.Implementation.Mushroom_BitByPlayer;
        }

        

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
