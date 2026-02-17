using System;
using ExtendedSlugbase.Features;
using ExtendedSlugbase.Helpers;
using MagicaHookingLibrary.Helpers;
using MagicaHookingLibrary.Interfaces;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using UnityEngine;
using MonoMod.RuntimeDetour;
using RWCustom;
using System.Linq;
using UnityEngine.SearchService;
using ExtendedSlugbase.Objects;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class PlayerHooks : IOwnHooks
    {
        public void PreApply()
        {
			On.Player.BiteEdibleObject += Player_BiteEdibleObject;
            On.Player.SwallowObject += Player_SwallowObject;
            On.Player.SpitUpCraftedObject += Player_SpitUpCraftedObject;
            On.Player.GraspsCanBeCrafted += Player_GraspsCanBeCrafted;
            _ = new Hook(typeof(Player.Tongue).GetProperty(nameof(Player.Tongue.totalRope), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod(), SaintTongueTotalRope);
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.Player.DeathByBiteMultiplier += Player_DeathByBiteMultiplier;
            On.Player.Grabability += Player_Grabability;
        }

		private void Player_BiteEdibleObject(On.Player.orig_BiteEdibleObject orig, Player self, bool eu)
		{
			if (self.TryGetFeature(PlayerFeaturesExt.canCreateSpears, out var specks) && specks.FeedFromSpears)
				return;

			orig(self, eu);
		}

		private void Player_SwallowObject(On.Player.orig_SwallowObject orig, Player self, int grasp)
        {
            var obj = self.grasps[grasp]?.grabbed;
            AbstractPhysicalObject abstractObj = null;

            if (obj != null && self.TryGetFeature(PlayerFeaturesExt.canCraftObjects, out var craft) && craft.SwallowRecipeList.Count > 0)
            {
                PlayerObjects.Craftability.Ingredient test = new(obj);
                craft.TryGetOneHandedRecipe(self, obj.abstractPhysicalObject,  test, out abstractObj, true);
            }
            orig(self, grasp);

            if (abstractObj != null)
            {
                self.objectInStomach = abstractObj;
            }
        }


        private void Player_SpitUpCraftedObject(On.Player.orig_SpitUpCraftedObject orig, Player self)
        {
            if (self.TryGetFeature(PlayerFeaturesExt.canCraftObjects, out var craft) && craft.TryGetRecipeResult(self, out bool isOneHanded, out var result, out var chosenGrasp))
            {
		        self.room.PlaySound(craft.CraftSound, self.mainBodyChunk);
                // One handed recipes
                if (isOneHanded)
                {
                    for (int i = 0; i < self.grasps.Length; i++)
                    {
                        AbstractPhysicalObject grabbed = self.grasps[i]?.grabbed.abstractPhysicalObject;
                        if (grabbed != null && self.grasps[i] == chosenGrasp)
                        {
                            self.ReleaseGrasp(i);
                            grabbed.realizedObject.RemoveFromRoom();
                            self.room.abstractRoom.RemoveEntity(grabbed);

                            self.room.abstractRoom.AddEntity(result);
                            result.RealizeInRoom();
                            self.SlugcatGrab(result.realizedObject, self.FreeHand());
                            return;
                        }
                    }   
                }
                else if (craft.EatsMeals && GourmandCombos.CraftingResults_ObjectData(self.grasps[0], self.grasps[1], true) == AbstractPhysicalObject.AbstractObjectType.DangleFruit)
                {
                    while ((self.grasps[0] != null && self.grasps[0].grabbed is IPlayerEdible) || (self.grasps[1] != null && self.grasps[1].grabbed is IPlayerEdible))
                    {
                        self.BiteEdibleObject(true);
                    }
                    self.AddFood(craft.MealBonus);
                }
                else
                {
                    self.room.abstractRoom.AddEntity(result);
                    result.RealizeInRoom();
                    for (int j = 0; j < self.grasps.Length; j++)
                    {
                        AbstractPhysicalObject toDelete = self.grasps[j].grabbed.abstractPhysicalObject;
                        if (self.room.game.session is StoryGameSession game)
                        {
                            game.RemovePersistentTracker(toDelete);
                        }
                        self.ReleaseGrasp(j);
                        for (int k = toDelete.stuckObjects.Count - 1; k >= 0; k--)
                        {
                            if (toDelete.stuckObjects[k] is AbstractPhysicalObject.AbstractSpearStick && toDelete.stuckObjects[k].A.type == AbstractPhysicalObject.AbstractObjectType.Spear && toDelete.stuckObjects[k].A.realizedObject != null)
                            {
                                (toDelete.stuckObjects[k].A.realizedObject as Spear).ChangeMode(Weapon.Mode.Free);
                            }
                        }
                        toDelete.LoseAllStuckObjects();
                        toDelete.realizedObject.RemoveFromRoom();
                        self.room.abstractRoom.RemoveEntity(toDelete);
                    }

                    if (self.FreeHand() != -1)
                    {
                        self.SlugcatGrab(result.realizedObject, self.FreeHand());
                    }
                }
                return;
            }
            orig(self);
        }


        private bool Player_GraspsCanBeCrafted(On.Player.orig_GraspsCanBeCrafted orig, Player self)
        {
            if (self.TryGetFeature(PlayerFeaturesExt.canCraftObjects, out var craft) && craft.TryGetRecipeResult(self, out _, out _, out _))
            {
                return true;
            }
            return orig(self);
        }


        private float SaintTongueTotalRope(Func<Player.Tongue, float> orig, Player.Tongue self)
        {
            if (self.player.TryGetFeature(PlayerFeaturesExt.saintTongue, out var tongue))
            {
                return Mathf.Max(tongue.Length + 50f, tongue.RetractLengths[1] + 30f);
            }
            return orig(self);
        }


        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            CWTs.PlayerCWT.GetData(self);

            if (self.TryGetFeature(PlayerFeaturesExt.saintTongue, out var tongue))
            {
                self.tongue = new(self, 0)
                {
                   minRopeLength = tongue.Retractable ? tongue.RetractLengths[0] : tongue.Length,
                   maxRopeLength = tongue.Retractable ? tongue.RetractLengths[1] : tongue.Length,
                   baseIdealRopeLength = tongue.Length,
                   idealRopeLength = tongue.Length
                };
                self.tongue.rope.thickness = tongue.Thickness; // This value honestly doesn't seem to affect anything but still
            }

            if (!Custom.rainWorld.ExpeditionMode && self.room != null && self.room.game.IsStorySession && self.room.game.GetStorySession?.saveState?.cycleNumber == 0 
                && self.room.game.TryGetFeature(GameFeaturesExt.spawnStomachObject, out var abstractObject) 
                && abstractObject.TryGetObject(self.room.abstractRoom, new(), out var startObject))
            {
            	self.objectInStomach = startObject;
            }

            if (!Custom.rainWorld.ExpeditionMode && self.abstractCreature.world.game.IsStorySession && self.abstractCreature.world.game.TryGetFeature(GameFeaturesExt.cycleLimit, out var hardMode) && self.abstractCreature.world.game.GetStorySession.RedIsOutOfCycles)
            {
                self.redsIllness = new(self, hardMode.Cycles - self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber);
            }
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);

            if (self.room != null && self.room.blizzard && self.TryGetFeature(PlayerFeaturesExt.warmth, out float warmth))
            {
                self.Hypothermia -= Mathf.Lerp(warmth, 0f, self.HypothermiaExposure);
            }

            self.redsIllness?.Update();
        }

        private static float Player_DeathByBiteMultiplier(On.Player.orig_DeathByBiteMultiplier orig, Player self)
        {
            if (self.TryGetFeature(PlayerFeaturesExt.deathByBiteMultiplier, out float[] multipliers))
            {
                if (self.room != null && self.room.game.IsStorySession)
                {
                    return multipliers[0] + self.room.game.GetStorySession.difficulty / (multipliers.Length == 1 ? 5f : multipliers[1]);
                }
                return multipliers[0] + 0.05f;
            }
            return orig(self);
        }


        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            if (self.TryGetFeature(PlayerFeaturesExt.objectGrabability, out var grabability))
            {
                if (obj is Creature creature && grabability.CreatureOverrides.TryGetValue(creature.Template.type, out var creatureGrab))
                    return creatureGrab;
                if (grabability.ObjectOverrides.TryGetValue(obj.abstractPhysicalObject.type, out var abstractGrab))
                    return abstractGrab;
            }
            return orig(self, obj);
        }

        public void OnApply()
		{
			On.Player.CanEatMeat += Player_CanEatMeat;
		}
		private bool Player_CanEatMeat(On.Player.orig_CanEatMeat orig, Player self, Creature crit)
		{
			return orig(self, crit) && !(self.TryGetFeature(PlayerFeaturesExt.canCreateSpears, out var specks) && specks.FeedFromSpears);
		}
		public void PostApply()
        {
        }
    }
}
