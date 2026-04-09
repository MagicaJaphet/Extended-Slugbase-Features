using ExtendedSlugbase.Features;
using ExtendedSlugbase.Helpers;
using ExtendedSlugbase.Objects;
using MagicaHookingLibrary.Helpers;
using MagicaHookingLibrary.Interfaces;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static MagicaHookingLibrary.Helpers.HookHelpers;
using static MonoMod.InlineRT.MonoModRule;

namespace ExtendedSlugbase.Hooks.ILHooks;
internal class WeaponILHooks : IOwnHooks
{
	public void PreApply()
	{
		IL.SharedPhysics.TraceProjectileAgainstBodyChunks += ILAction(SharedPhysics_TraceProjectileAgainstBodyChunks);
		IL.Spear.HitSomething += ILAction(Spear_HitSomething);
	}

	private void SharedPhysics_TraceProjectileAgainstBodyChunks(ILCursor c)
	{
		static bool CanHitEdibleObject(bool canBeHitByWeapons, SharedPhysics.IProjectileTracer projTracer, PhysicalObject physicalObject, PhysicalObject exemptObject)
		{
			return canBeHitByWeapons ||
				(projTracer is Spear spear && exemptObject is Player player
				&& physicalObject != null 
				&& physicalObject is not SeedCob
				&& physicalObject is not Pomegranate
				&& spear.Spear_NeedleCanFeed() 
				&& PlayerFeatures.Diet.TryGet(player, out var diet) && diet.GetFoodMultiplier(physicalObject) > 0f);
		}

		c.GotoNext(
			MoveType.After,
			x => x.MatchLdfld(typeof(PhysicalObject).GetField(nameof(PhysicalObject.canBeHitByWeapons)))
			);
		c.Emit(OpCodes.Ldarg_0);
		c.Emit(OpCodes.Ldloc, 6);
		c.Emit(OpCodes.Ldarg, 6);
		c.EmitDelegate(CanHitEdibleObject);
	}

	private void Spear_HitSomething(ILCursor c, ILContext il)
	{
		static PlayerObjects.SpearCreatability SpearFeature(Spear self)
		{
			if (self.thrownBy is Player player && player.TryGetFeature(PlayerFeaturesExt.canCreateSpears, out var result))
			{
				return result;
			}
			return null;
		}
		var spearFeature = il.GetFeature<PlayerObjects.SpearCreatability, Spear>(c, SpearFeature);

		static bool CanEatEggBugs(bool canFeedFromSpears, PlayerObjects.SpearCreatability specks, Spear self)
		{
			return canFeedFromSpears || // Return default logic
				(specks != null
				&& self.thrownBy is Player player 
				&& self.Spear_NeedleCanFeed() && player.FoodInStomach < player.MaxFoodInStomach
				&& (!PlayerFeatures.Diet.TryGet(player, out var diet) 
				|| (diet.Meat > 0f
				&& (!diet.CreatureOverrides.TryGetValue(CreatureTemplate.Type.EggBug, out var value) || value > 0f)
				&& (!diet.CreatureOverrides.TryGetValue(MoreSlugcatsEnums.CreatureTemplateType.FireBug, out var value2) || value2 > 0f))));
		}

		static bool CanCreateSpears(PlayerObjects.SpearCreatability specks)
		{
			return specks != null;
		}

		static bool SpearHitSomethingFeed(Spear self, SharedPhysics.CollisionResult result, bool eu, PlayerObjects.SpearCreatability specks)
		{
			if (self.thrownBy is Player player && SlugBaseCharacter.TryGet(player.SlugCatClass, out _))
			{
				var stickObject = false;
				var defaultFood = 0f;
				SlugBase.DataTypes.Diet diet = null;
				if (self.Spear_NeedleCanFeed() && specks != null
					&& specks.FeedFromSpears && PlayerFeatures.Diet.TryGet(player, out diet))
				{
					defaultFood = diet.GetFoodMultiplier(result.obj);
				}

				if (result.obj is Creature creature && creature.SpearStick(self, Mathf.Lerp(0.55f, 0.62f, UnityEngine.Random.value), result.chunk, result.onAppendagePos, self.firstChunk.vel))
				{
					if (diet?.GetMeatMultiplier(player, creature) is float meat && meat > 0f && (!creature.dead || diet.Corpses > 0f) && creature.State.meatLeft > 0f)
					{
						player.ProcessFood(meat);
						creature.State.meatLeft -= 1;
						if (self.room.game.IsStorySession && self.room.game.GetStorySession.playerSessionRecords != null)
						{
							self.room.game.GetStorySession.playerSessionRecords[player.playerState.playerNumber].AddEat(result.obj);
						}
					}
					if (self.abstractPhysicalObject.world.game.IsArenaSession)
					{
						self.abstractPhysicalObject.world.game.GetArenaGameSession.PlayerLandSpear(player, creature);
					}
					stickObject = true;
				}
				else if (diet != null && defaultFood > 0f)
				{
					bool processFood = false;
					// Handle default cases
					if (result.obj is IPlayerEdible && player.FoodInStomach < player.MaxFoodInStomach)
					{
						processFood = true;
						if (result.obj is DangleFruit fruit && fruit.stalk != null)
						{
							for (int i = 0; i < fruit.stalk.segs.GetLength(0); i++)
							{
								fruit.stalk.segs[i, 2] += self.firstChunk.vel.normalized * 3.5f;
							}
						}
						result.obj.firstChunk.vel = self.firstChunk.vel;
						for (int i = 0; i < 10; i++)
						{
							self.room.AddObject(new WaterDrip(result.obj.firstChunk.pos, self.firstChunk.vel / UnityEngine.Random.Range(1.7f, 4f) + new Vector2(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-2f, 2f)), false));
						}
						self.firstChunk.vel /= 2f;
						if (result.obj is GooieDuck duck)
						{
							if (duck.bites == 6)
							{
								self.room.PlaySound(DLCSharedEnums.SharedSoundID.Duck_Pop, result.obj.firstChunk, false, 1f, 0.5f + UnityEngine.Random.value * 0.5f);
							}
							else if (!duck.StringsBroke && duck.bites - 2 <= 0)
								self.room.PlaySound(SoundID.Lizard_Jaws_Shut_Miss_Creature, self.firstChunk, false, 0.8f, 1.6f + UnityEngine.Random.value / 10f);

							duck.bites -= 2;
							if (duck.bites == 0)
							{
								duck.Destroy();
								duck = null;
							}
							if (duck != null)
							{
								duck.firstChunk.vel = self.firstChunk.vel / 1.8f;
								for (int i = 0; i < 3; i++)
								{
									self.room.AddObject(new WaterDrip(result.obj.firstChunk.pos, Custom.DegToVec(UnityEngine.Random.value * 360f) * Mathf.Lerp(4f, 21f, UnityEngine.Random.value), false));
								}
								self.firstChunk.vel.x /= 5f;
							}
						}
						else if (result.obj.abstractPhysicalObject is AbstractConsumable consumable)
						{
							if (!consumable.isConsumed)
								consumable.Consume();
							result.obj.Destroy();
						}
					}

					if (result.obj is SeedCob seedCob)
					{
						seedCob.Open();
						processFood = true;
						stickObject = true;
					}
					if (result.obj is JellyFish jellyFish)
					{
						if (!jellyFish.dead)
						{
							(result.obj as JellyFish).dead = true;
							processFood = true;
							stickObject = true;
						}
						else
						{
							jellyFish.Destroy();
							processFood = diet.Corpses > 0f;
						}
					}
					if (result.obj is Pomegranate pomegranate && pomegranate.smashed)
					{
						(result.obj as Pomegranate).spearmasterStabbed = true;
						processFood = true;
						stickObject = true;
					}

					if (processFood)
					{
						player.ProcessFood(defaultFood);
						if (self.room.game.IsStorySession && self.room.game.GetStorySession.playerSessionRecords != null)
						{
							self.room.game.GetStorySession.playerSessionRecords[player.playerState.playerNumber].AddEat(result.obj);
						}
						stickObject = true;
					}
				}

				self.Spear_NeedleDisconnect();
				if (stickObject)
				{
					self.room.PlaySound(SoundID.Spear_Stick_In_Creature, self.firstChunk);
					self.LodgeInCreature(result, eu);
					return true;
				}
			}

			self.room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, self.firstChunk);
			self.vibrate = 20;
			self.ChangeMode(Weapon.Mode.Free);
			self.firstChunk.vel = self.firstChunk.vel * -0.5f + Custom.DegToVec(UnityEngine.Random.value * 360f) * (Mathf.Lerp(0.1f, 0.4f, UnityEngine.Random.value) * self.firstChunk.vel.magnitude);
			self.SetRandomSpin();
			return false;
		}

		// Edit eggbug bool to ensure we can actually eat the eggbug before telling the game not to throw eggs
		c.GotoNext(
			MoveType.After,
			x => x.MatchCallOrCallvirt(typeof(Spear).GetMethod(nameof(Spear.Spear_NeedleCanFeed)))
			);
		c.EmitFeatureDelegate(spearFeature, CanEatEggBugs, true);

		// Spear diet
		c.GotoNext(
			MoveType.After,
			x => x.MatchCallOrCallvirt(typeof(PhysicalObject.IHaveAppendages).GetMethod(nameof(PhysicalObject.IHaveAppendages.ApplyForceOnAppendage)))
			);
		c.MoveAfterLabels();
		c.EmitFeatureDelegate(spearFeature, CanCreateSpears);

		// then jump over spearmaster logic because we don't need it lol
		ILCursor jump = c.CloneAndGoToNext(
			x => x.MatchLdsfld(typeof(SoundID).GetField(nameof(SoundID.Spear_Bounce_Off_Creauture_Shell)))
			);
		jump.GotoPrev(
			x => x.MatchLdarg(0)
			);
		jump.MoveAfterLabels();
		c.Emit(OpCodes.Brtrue, jump.MarkLabel());

		jump.Emit(OpCodes.Ldarg_0);
		jump.Emit(OpCodes.Ldarg_1);
		jump.Emit(OpCodes.Ldarg_2);
		jump.Emit(OpCodes.Ldloc, spearFeature);
		jump.EmitDelegate(SpearHitSomethingFeed);
		jump.Emit(OpCodes.Ret);
	}

	public void OnApply()
	{
	}

	public void PostApply()
	{
	}
}
