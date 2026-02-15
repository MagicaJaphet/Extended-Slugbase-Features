using MagicaHookingLibrary.Interfaces;
using MonoMod.Cil;
using ExtendedSlugbase.Features;
using static ExtendedSlugbase.Objects.PlayerObjects;
using static MagicaHookingLibrary.Helpers.HookHelpers;
using Mono.Cecil.Cil;
using ExtendedSlugbase.Helpers;
using MagicaHookingLibrary.Helpers;
using MoreSlugcats;
using System;
using System.Linq;
using SlugBase.Features;
namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class ConsumableILHooks : IOwnHooks
    {
        public void PreApply()
		{
			IL.Pomegranate.Update += ILAction(Pomegranate_Update);
			IL.SeedCob.Update += ILAction(SeedCob_Update);
			IL.SeedCob.HitByWeapon += ILAction(SeedCob_HitByWeapon);
            IL.BubbleGrass.Update += ILAction(BubbleGrass_Update);
            IL.WaterNut.Update += ILAction(WaterNut_Update);
            IL.Explosion.Update += ILAction(Explosion_Update);
            IL.Mushroom.BitByPlayer += ILAction(Mushroom_BitByPlayer);
        }

		private void Pomegranate_Update(ILCursor c)
		{
			static bool FeedsOnPopcorn(bool isSpear, Player player)
			{
				return isSpear && !(player.TryGetFeature(PlayerFeaturesExt.canCreateSpears, out var specks) && specks.FeedFromSpears);
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear).GetSlugcatFieldInfo()
				);
			c.Emit(OpCodes.Ldloc, 10);
			c.EmitDelegate(FeedsOnPopcorn);
		}

		private void SeedCob_Update(ILCursor c)
		{
			static bool FeedsOnPopcorn(bool isSpear, Player player)
			{
				return isSpear && !(player.TryGetFeature(PlayerFeaturesExt.canCreateSpears, out var specks) && specks.FeedFromSpears);
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear).GetSlugcatFieldInfo()
				);
			c.Emit(OpCodes.Ldloc, 13);
			c.EmitDelegate(FeedsOnPopcorn);
		}

		private void SeedCob_HitByWeapon(ILCursor c)
		{
			static bool FeedsOnPopcorn(bool isSpear, SeedCob self, Weapon weapon)
			{
				return isSpear || (weapon.thrownBy is Player player
					&& player.TryGetFeature(PlayerFeaturesExt.canCreateSpears, out var specks)
					&& specks.FeedFromSpears && PlayerFeatures.Diet.TryGet(player, out var diet) 
					&& diet.GetFoodMultiplier(self) > 0f);
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear).GetSlugcatFieldInfo()
				);
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate(FeedsOnPopcorn);
		}

		private void BubbleGrass_Update(ILCursor c)
        {
            static float BubbleWeedMultiplier(float orig, BubbleGrass self)
            {
                if (self.grabbedBy?.FirstOrDefault(x => x.grabber is Player)?.grabber is Player player && player.TryGetFeature(PlayerFeaturesExt.objectInteractions, out var objectInteractions))
                {
                    return orig * objectInteractions.BubbleWeedUsageMultiplier;
                }
                return orig;
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdcR4(0.0009090909f)
                );
            c.EmitLdarg0Delegate(BubbleWeedMultiplier);
        }


        private void WaterNut_Update(ILCursor c)
        {
            static bool PopWaterNut(bool isRivulet, WaterNut self, int grasp)
            {
                return isRivulet || ((self.grabbedBy[grasp].grabber as Player).TryGetFeature(PlayerFeaturesExt.objectInteractions, out var objectInteractions) && objectInteractions.PopBubbleFruit);
            }

            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Rivulet).GetSlugcatFieldInfo()
                );
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 1);
            c.EmitDelegate(PopWaterNut);
        }


        private void Explosion_Update(ILCursor c)
        {
            static bool ExplosionImmunity(bool isArti, Explosion self, int j, int k)
            {
                return isArti || (self.room.physicalObjects[j][k] is Player player && player.TryGetFeature(PlayerFeaturesExt.objectInteractions, out var interactions) && interactions.ExplosiveImmune);
            }
            static bool NoExplosionImmunity(bool isNotArti, Explosion self, int j, int k)
            {
                return isNotArti && !(self.room.physicalObjects[j][k] is Player player && player.TryGetFeature(PlayerFeaturesExt.objectInteractions, out var interactions) && interactions.ExplosiveImmune);
            }

            for (int i = 0; i < 4; i++)
            {
                c.TryMoveToNextSlugcatBool(
                    nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer).GetSlugcatFieldInfo()
                    );
                
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, 2);
                c.Emit(OpCodes.Ldloc, 3);
                c.EmitDelegate<Func<bool, Explosion, int, int, bool>>(i == 2 ? NoExplosionImmunity : ExplosionImmunity);
            }
        }


        private void Mushroom_BitByPlayer(ILCursor c)
        {
            static int MushroomCounter(int timer, Creature.Grasp grasp)
            {
                if (grasp.grabber is Player player && player.TryGetFeature(PlayerFeaturesExt.objectInteractions, out var objInteractions))
                {
                    ObjectInteractions.lastAteMushroomFPS = objInteractions.MushroomFPS;
                    return objInteractions.MushroomTimer;
                }
                ObjectInteractions.lastAteMushroomFPS = null;
                return timer;
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdcI4(320)
                ); // (grasp.grabber as Player).mushroomCounter += 320;
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate(MushroomCounter);
        }

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
