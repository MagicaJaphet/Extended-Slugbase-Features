using System;
using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using ExtendedSlugbase.Features.PlayerRelated;
using MagicaHookingLibrary.Interfaces;
using MoreSlugcats;
using RWCustom;
using SlugBase.DataTypes;
using SlugBase.Features;
using UnityEngine;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class WeaponHooks : IOwnHooks
    {
        public void PreApply()
        {
			On.Spear.HitSomethingWithoutStopping += Spear_HitSomethingWithoutStopping;
			On.Spear.Spear_NeedleCanFeed += Spear_Spear_NeedleCanFeed;
            On.ExplosiveSpear.DrawSprites += ExplosiveSpear_DrawSprites;
            On.Spear.DrawSprites += Spear_DrawSprites;
        }

		private void Spear_HitSomethingWithoutStopping(On.Spear.orig_HitSomethingWithoutStopping orig, Spear self, PhysicalObject obj, BodyChunk chunk, PhysicalObject.Appendage appendage)
		{
			if (self.Spear_NeedleCanFeed() && self.thrownBy is Player player && PlayerFeatures.Diet.TryGet(player, out var diet))
			{
				if (obj.abstractPhysicalObject.rippleLayer != self.abstractPhysicalObject.rippleLayer && !obj.abstractPhysicalObject.rippleBothSides && !self.abstractPhysicalObject.rippleBothSides)
					return;

				if (self.room.game.IsStorySession && self.room.game.GetStorySession.playerSessionRecords != null)
					self.room.game.GetStorySession.playerSessionRecords[(player.abstractCreature.state as PlayerState).playerNumber].AddEat(obj);

				if (obj is Creature creature && !creature.dead)
				{
					player.ProcessFood(diet.GetMeatMultiplier(player, creature));
				}
				if (obj is Mushroom)
				{
					ObjectInteractions.Implementation.Spear_HitSomethingWithoutStopping(player);
				}
				if (obj is KarmaFlower)
				{
					if (self.room.game.IsStorySession && !self.room.game.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma)
					{
						self.room.game.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma = true;
						int i = 0;
						while (i < self.room.game.cameras.Length)
						{
							if (self.room.game.cameras[i].followAbstractCreature == player.abstractCreature || ModManager.CoopAvailable)
							{
								if (self.room.game.cameras[i].hud != null)
								{
									self.room.game.cameras[i].hud.karmaMeter.reinforceAnimation = 0;
									break;
								}
								break;
							}
							else
							{
								i++;
							}
						}
					}
					obj.Destroy();
				}
				else if (obj is OracleSwarmer swarmer)
				{
					self.room.PlaySound(SoundID.Centipede_Shock, obj.firstChunk, false, 1f, 1.5f + UnityEngine.Random.value);
					if (self.room.game.IsStorySession)
					{
						self.room.game.GetStorySession.playerSessionRecords?[(player.abstractCreature.state as PlayerState).playerNumber].AddEat(obj);
					}

					var mass = obj.firstChunk.mass;

					if (diet.GetFoodMultiplier(swarmer) > 0f)
					{
						player.ProcessFood(diet.GetFoodMultiplier(swarmer));
						player.glowing = true;
						if (self.room.game.IsStorySession)
						{
							self.room.game.GetStorySession.saveState.theGlow = true;
						}
						Color color = Color.white;
						if (obj is SSOracleSwarmer ssSwarmer)
						{
							color = Custom.HSL2RGB(ssSwarmer.color.x > 0.5f ? Custom.LerpMap(ssSwarmer.color.x, 0.5f, 1f, 0.6666667f, 0.99722224f) : 0.6666667f, 1f, Mathf.Lerp(0.75f, 0.9f, ssSwarmer.color.y));
						}
						self.room.AddObject(new Spark(obj.firstChunk.pos, Custom.RNV() * 60f * UnityEngine.Random.value, color, null, 20, 50));
						obj.Destroy();
					}
					self.firstChunk.vel /= mass;
					foreach (AbstractCreature abstractCreature in self.room.abstractRoom.creatures)
					{
						if (ModManager.DLCShared && abstractCreature != null && abstractCreature.creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.Inspector 
							&& abstractCreature.realizedCreature is Inspector inspector && self.thrownBy != null 
							&& inspector.AI.VisualContact(self.thrownBy.firstChunk) && inspector.AI.VisualContact(self.firstChunk))
						{
							inspector.AI.preyTracker.AddPrey(inspector.AI.tracker.RepresentationForCreature(self.thrownBy.abstractCreature, true));
						}
					}
				}
				else if (obj is IPlayerEdible edible && edible.Edible && obj is not Creature)
				{
					player.ProcessFood(diet.GetFoodMultiplier(obj));
					obj.Destroy();
				}
				return;
			}

			orig(self, obj, chunk, appendage);
		}

		private bool Spear_Spear_NeedleCanFeed(On.Spear.orig_Spear_NeedleCanFeed orig, Spear self)
		{
			return orig(self) || CanCreateSpears.Implementation.Spear_Spear_NeedleCanFeed(self);
		}

		private void ExplosiveSpear_DrawSprites(On.ExplosiveSpear.orig_DrawSprites orig, ExplosiveSpear self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);

            if (CWTs.SpearCWT.TryGetData(self.abstractPhysicalObject as AbstractSpear, out var cwt))
            {
                float lerp = self.spearmasterNeedle_fadecounter / (float)self.spearmasterNeedle_fadecounter_max;
                if (self.spearmasterNeedle_hasConnection || !self.IsNeedle)
                {
                    lerp = 1f;
                }
                sLeaser.sprites[1].color = Color.Lerp(cwt.generatedSpearColor?.GetSlotColor(cwt.playerNumber, rCam.paletteTexture) ?? self.color, cwt.generatedSpearFadeColor?.GetSlotColor(cwt.playerNumber, rCam.paletteTexture) ?? self.color, 1f - Mathf.Max(0.01f, lerp));
            }
        }


        private void Spear_DrawSprites(On.Spear.orig_DrawSprites orig, Spear self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);

            if (CWTs.SpearCWT.TryGetData(self.abstractPhysicalObject as AbstractSpear, out var cwt) && cwt.generatedSpearColor is ColorSlot slot)
            {
                float lerp = self.spearmasterNeedle_fadecounter / (float)self.spearmasterNeedle_fadecounter_max;
                if (self.spearmasterNeedle_hasConnection)
                {
                    lerp = 1f;
                }
                sLeaser.sprites[0].color = Color.Lerp(cwt.generatedSpearColor?.GetSlotColor(cwt.playerNumber, rCam.paletteTexture) ?? self.color, cwt.generatedSpearFadeColor?.GetSlotColor(cwt.playerNumber, rCam.paletteTexture) ?? self.color, 1f - Mathf.Max(0.01f, lerp));
            }
        }


        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
