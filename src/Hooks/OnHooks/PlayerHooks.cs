using MagicaHookingLibrary.Interfaces;
using MonoMod.RuntimeDetour;
using ExtendedSlugbase.Features.GameRelated;
using ExtendedSlugbase.Features.PlayerRelated;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class PlayerHooks : IOwnHooks
    {
        public void PreApply()
        {
			On.Player.BiteEdibleObject += Player_BiteEdibleObject;
            On.Player.SwallowObject += CanCraftObjects.Implementation.Player_SwallowObject;
            On.Player.SpitUpCraftedObject += CanCraftObjects.Implementation.Player_SpitUpCraftedObject;
            On.Player.GraspsCanBeCrafted += Player_GraspsCanBeCrafted;
            _ = new Hook(typeof(Player.Tongue).GetProperty(nameof(Player.Tongue.totalRope), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod(), SaintTongue.Implementation.Player_Tongue_TotalRope);
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.Player.DeathByBiteMultiplier += DeathByBiteMultiplier.Implementation.Player_DeathByBiteMultiplier;
            On.Player.Grabability += ObjectGrabOverrides.Implementation.Player_Grabability;
        }

		private void Player_BiteEdibleObject(On.Player.orig_BiteEdibleObject orig, Player self, bool eu)
		{
			if (CanCreateSpears.Implementation.Player_BiteEdibleObject(self))
				return;

			orig(self, eu);
		}

        private bool Player_GraspsCanBeCrafted(On.Player.orig_GraspsCanBeCrafted orig, Player self)
        {
            if (CanCraftObjects.Implementation.Player_GraspsCanBeCrafted(self))
            {
                return true;
            }
            return orig(self);
        }

        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            CWTs.PlayerCWT.GetData(self);

            SaintTongue.Implementation.Player_ctor(self);

            StartingStomachObject.Implementation.Player_Ctor(self);

			CycleLimit.Implementation.Player_ctor(self);

			IsSlugpup.Implementation.Player_ctor(self);
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);

			Warmth.Implementation.Player_Update(self);

            self.redsIllness?.Update();
        }


        public void OnApply()
		{
			On.Player.CanEatMeat += Player_CanEatMeat;
		}
		private bool Player_CanEatMeat(On.Player.orig_CanEatMeat orig, Player self, Creature crit)
		{
			return orig(self, crit) && !CanCreateSpears.Implementation.Player_CanEatMeat(self);
		}
		public void PostApply()
        {
        }
    }
}
