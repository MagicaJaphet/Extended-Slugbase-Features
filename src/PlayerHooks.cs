using System;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using MoreSlugcats;
using SlugBase.Features;
using System.Collections.Generic;
using RWCustom;
using SlugBase;
using System.Linq;
using SlugBase.DataTypes;
using System.Runtime.CompilerServices;
using static MonoMod.InlineRT.MonoModRule;
using static ExtendedSlugbaseFeatures.Resources;
using MonoMod.RuntimeDetour;

namespace ExtendedSlugbaseFeatures
{
	internal class PlayerHooks
	{
		internal static void Apply()
		{
			MechanicHooks.ApplyHooks();
			GraphicHooks.ApplyHooks();
		}

		/// <summary>
		/// Handles hooks to the <see cref="Player"/> class, or classes that reference it.
		/// </summary>
		internal class MechanicHooks
		{
			internal static void ApplyHooks()
			{
				GeneralHooks.Apply();
				SpearmasterHooks.Apply();
				ArtificerHooks.Apply();
				GourmandHooks.Apply();
				RivuletHooks.Apply();
				SaintHooks.Apply();
			}

			internal class GeneralHooks
			{
				internal static void Apply()
				{
					On.Player.ctor += Initialize;
					On.Player.GraspsCanBeCrafted += GraspsFeatureConditions;
					On.Player.CanBeSwallowed += CanPlayerSwallow;
					On.Player.DeathByBiteMultiplier += Player_DeathByBiteMultiplier;
					On.Player.Grabability += Player_Grabability;
				}

				/// <summary>
				/// Processes overrides from <see cref="ExtFeatures.objectGrabability"/>.
				/// </summary>
				private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
				{
					if (obj != null && obj is not Spear && self.HasFeature(ExtFeatures.objectGrabability, out var overrides) &&  overrides.TryGetValue(obj.abstractPhysicalObject.type, out var grab))
					{
						return grab;
					}
					return orig(self, obj);
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to spawn with a stomach object upon campaign start, using <see cref="ExtFeatures.spawnStomachObject"/>.
				/// </summary>
				private static void Initialize(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
				{
					orig(self, abstractCreature, world);

					if (self.room != null && self.room.game.IsStorySession && self.room.game.GetStorySession.saveState.cycleNumber == 0 && GameFeatures.StartRoom.TryGet(self.room.game, out string[] rooms) && rooms != null && rooms.Contains(self.room.game.GetStorySession.saveState.denPosition) && self.room.game.HasFeature(ExtFeatures.spawnStomachObject, out var objValues) && objValues != null)
					{
						self.objectInStomach = Miscellaneous.GetAbstractPhysicalObjectsFromDict(self.room.abstractRoom, objValues, default, 1).FirstOrDefault();
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to use various craft features, using <see cref="ExtFeatures.explosiveCraftCost"/>.
				/// </summary>
				private static bool GraspsFeatureConditions(On.Player.orig_GraspsCanBeCrafted orig, Player self)
				{
					return orig(self) || (self.HasFeature(ExtFeatures.explosiveCraftCost, out var explosiveCost) && explosiveCost[0] > -1 &&
						((explosiveCost.Length < 2 && self.FoodInStomach > 0) || (explosiveCost.Length >= 2 && self.FoodInStomach >= explosiveCost[1]))
						&& (explosiveCost.Length < 3 || self.input[0].y == explosiveCost[2])
						&& self.CraftingResults() != null
						&& (self.HasFeature(ExtFeatures.rowsAndColumnsSpearSpecks, false)
						|| ((self.graphicsModule as PlayerGraphics).tailSpecks != null && (self.graphicsModule as PlayerGraphics).tailSpecks.spearProg == 0f)));
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to ignore checks for swallowing objects, using <see cref="ExtFeatures.cantSwallowObjects"/>.
				/// </summary>
				private static bool CanPlayerSwallow(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
				{
					return (self.HasFeature(ExtFeatures.cantSwallowObjects, false) && orig(self, testObj)) || orig(self, testObj);
				}

				/// <summary>
				/// Shifts the <see cref="Player.DeathByBiteMultiplier"/> to the first value, with the session difficulty multiplied by the second provided value, using <see cref="ExtFeatures.deathByBiteMultiplier"/>.
				/// </summary>
				/// <param name="orig"></param>
				/// <param name="self"></param>
				/// <returns></returns>
				private static float Player_DeathByBiteMultiplier(On.Player.orig_DeathByBiteMultiplier orig, Player self)
				{
					if (self.HasFeature(ExtFeatures.deathByBiteMultiplier, out float[] multipliers))
					{
						if (self.room != null && self.room.game.IsStorySession)
						{
							return multipliers[0] + (self.room.game.GetStorySession.difficulty / (multipliers.Length == 1 ? 5f : multipliers[1]));
						}
						return multipliers[0] + 0.05f;
					}
					return orig(self);
				}
			}


			/// <summary>
			/// Applies <see cref="MoreSlugcatsEnums.SlugcatStatsName.Spear"/> specific Hooks.
			/// </summary>
			internal class SpearmasterHooks
			{
				// Shorthand for an otherwise longly-typed method
				private static bool TryNext(ILCursor cursor)
				{
					return cursor.MoveToNextSlugcat(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear)));
				}
				// Various IL bools that we'd insert onto the stack
				private static bool EatsWithMouth(bool result, Player player)
				{
					return result && !(player.HasFeature(ExtFeatures.rowsAndColumnsSpearSpecks) && player.HasFeature(ExtFeatures.forceFeedingFromSpears));
				}
				private static bool HasSpearSpecks(bool result, Player player)
				{
					return result || player.HasFeature(ExtFeatures.rowsAndColumnsSpearSpecks);
				}
				private static bool CanSwallow(bool result, Player player)
				{
					return result && player.HasFeature(ExtFeatures.cantSwallowObjects, false);
				}

				internal static void Apply()
				{
					try
					{
						IL.Player.GrabUpdate += Player_GrabUpdate;
						On.Player.CanEatMeat += Player_CanEatMeat;
						On.Player.Grabability += Player_Grabability;
						On.Player.BiteEdibleObject += Player_BiteEdibleObject;
						IL.Player.ClassMechanicsSpearmaster += Player_ClassMechanicsSpearmaster;
						On.Spear.Spear_NeedleCanFeed += NeedleCanFeed;
						On.Spear.HitSomethingWithoutStopping += SpearHitSomethingNoStopDiet;
						IL.Spear.HitSomething += SpearHitSomethingDiet;
						IL.SeedCob.HitByWeapon += SeedCobReactToFeedSpear;
						IL.SeedCob.Update += SeedCobReactToSpear;
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Changes various Spearmaster related values, allowing use of them.
				/// </summary>
				private static void Player_GrabUpdate(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);

						if (TryNext(cursor)) // PlayerGraphics.TailSpeckles tailSpecks = (base.graphicsModule as PlayerGraphics).tailSpecks; 
						{
							cursor.ImplementILCodeAssumingLdarg0(HasSpearSpecks);
						}
						if (TryNext(cursor)) // if (base.grasps[num8] != null && base.grasps[num8].grabbed is IPlayerEdible && (base.grasps[num8].grabbed as IPlayerEdible).Edible)
						{
							cursor.ImplementILCodeAssumingLdarg0(EatsWithMouth);
						}
						if (TryNext(cursor)) // while (num5 < 0 && num8 < 2 && (!ModManager.MSC || this.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear))
						{
							cursor.ImplementILCodeAssumingLdarg0(CanSwallow);
						}
						if (TryNext(cursor)) // if (ModManager.MSC && this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear && (base.grasps[0] == null || base.grasps[1] == null) && num5 == -1 && this.input[0].y == 0)
						{
							cursor.Emit(OpCodes.Ldarg_0);
							static bool CanSpawnSpearsDualWield(bool isSpear, Player player)
							{
								return isSpear || (player.HasFeature(ExtFeatures.rowsAndColumnsSpearSpecks) && (player.HasFeature(ExtFeatures.canDualWield) || (player.FreeHand() != -1 && !player.grasps.Any(x => x != null && x.grabbed is Spear))));
							}
							cursor.EmitDelegate(CanSpawnSpearsDualWield);

							if (cursor.TryGotoNext(x => x.MatchLdarg(0),
								x => x.MatchCallOrCallvirt(typeof(Player).GetProperty(nameof(Player.input), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod())))
							{
								ILCursor spearJumpCursor = cursor.Clone();

								spearJumpCursor.GotoNext(x => x.MatchBrtrue(out _));
								ILLabel boolJump = spearJumpCursor.Next.Operand as ILLabel;
								spearJumpCursor.Index++;

								ILLabel jumpBr = spearJumpCursor.MarkLabel();

								cursor.Emit(OpCodes.Br, jumpBr);
								spearJumpCursor.Emit(OpCodes.Ldarg_0);
								static bool CustomInputChecks(Player player)
								{
									return player.eatMeat <= 0 && (player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear || (player.HasFeature(ExtFeatures.cantSwallowObjects) && player.input[0].y == 0) || (player.HasFeature(ExtFeatures.cantSwallowObjects, false) && player.input[0].y == 1));
								}
								spearJumpCursor.EmitDelegate(CustomInputChecks);
								spearJumpCursor.Emit(OpCodes.Brfalse, boolJump);

								if (cursor.TryGotoNext(MoveType.After,
									x => x.MatchNewobj<AbstractSpear>(),
									x => x.MatchStloc(19)))
								{
									cursor.Emit(OpCodes.Ldarg_0);
									cursor.Emit(OpCodes.Ldloc, 19);
									static void GetSpearCWT(Player player, AbstractSpear self)
									{
										CWTs.SpearValues cwt = CWTs.spearCWT.GetOrCreateValue(self);
										if (cwt != null)
										{
											if (SlugBaseCharacter.TryGet(player.SlugCatClass, out var character) && PlayerFeatures.CustomColors.TryGet(character, out var slots) && slots.Length > 1)
											{
												ColorSlot slot = slots.Where(x => x.Name == "Spears").FirstOrDefault();
												if (slot != null)
												{
													if (player.room != null && player.room.game.IsArenaSession && slot.Variants != null)
													{
														int playerNum = player.playerState.playerNumber;
														if (slot.Variants.Length >= playerNum + 1)
														{
															cwt.slugColor = slot.Variants[playerNum];
														}
													}
													else if (slot.Default != null)
													{
														cwt.slugColor = slot.Default;
													}
												}
											}
										}
									}
									cursor.EmitDelegate(GetSpearCWT);
								}
							}
						}
						if (TryNext(cursor)) // else if ((num7 > -1 || this.objectInStomach != null || this.isGourmand) && (!ModManager.MSC || this.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear))
						{
							cursor.ImplementILCodeAssumingLdarg0(CanSwallow);
						}
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Ensures the <see cref="SlugBaseCharacter"/> cannot eat from corpses if they feed from needles instead.
				/// </summary>
				private static bool Player_CanEatMeat(On.Player.orig_CanEatMeat orig, Player self, Creature crit)
				{
					if (self.HasFeature(ExtFeatures.forceFeedingFromSpears))
						return false;

					return orig(self, crit);
				}

				/// <summary>
				/// Overrides the grabability of spears to allow dual-wielding.
				/// </summary>
				private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
				{
					if (obj is Spear && self.HasFeature(ExtFeatures.canDualWield))
					{
						return Player.ObjectGrabability.OneHand;
					}

					return orig(self, obj);
				}

				/// <summary>
				/// Overrides the ability to eat foods with mouth, if <see cref="ExtFeatures.forceFeedingFromSpears"/> is true.
				/// </summary>
				private static void Player_BiteEdibleObject(On.Player.orig_BiteEdibleObject orig, Player self, bool eu)
				{
					if (self.HasFeature(ExtFeatures.forceFeedingFromSpears))
						return;

					orig(self, eu);
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to generate spears.
				/// </summary>
				/// <param name="il"></param>
				private static void Player_ClassMechanicsSpearmaster(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);
						
						if (cursor.MoveToNextSlugcat(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear)))) // if ((base.stun >= 1 || base.dead) && this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear)
						{
							cursor.ImplementILCodeAssumingLdarg0(HasSpearSpecks);
						}
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Returns true if <see cref="SlugBaseCharacter"/> feeds from spears.
				/// </summary>
				private static bool NeedleCanFeed(On.Spear.orig_Spear_NeedleCanFeed orig, Spear self)
				{
					if (ModManager.MSC && self.thrownBy != null && self.thrownBy is Player player && player.HasFeature(ExtFeatures.forceFeedingFromSpears))
					{
						return self.spearmasterNeedle && self.spearmasterNeedle_hasConnection;
					}
					return orig(self);
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to feed from Spearmaster needles dynamically.
				/// </summary>
				private static void SpearHitSomethingNoStopDiet(On.Spear.orig_HitSomethingWithoutStopping orig, Spear self, PhysicalObject obj, BodyChunk chunk, PhysicalObject.Appendage appendage)
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
							player.mushroomCounter += 320;
						}
						if (obj is KarmaFlower)
						{
							if (self.room.game.session is StoryGameSession && !self.room.game.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma)
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
								if (self.room.game.GetStorySession.playerSessionRecords != null)
								{
									self.room.game.GetStorySession.playerSessionRecords[(player.abstractCreature.state as PlayerState).playerNumber].AddEat(obj);
								}
							}

							if (diet.GetFoodMultiplier(swarmer) > 0f)
							{
								player.ProcessFood(diet.GetFoodMultiplier(swarmer));
								player.glowing = true;
								if (self.room.game.IsStorySession)
								{
									self.room.game.GetStorySession.saveState.theGlow = true;
								}
								Color color = Color.white;
								if (obj is SSOracleSwarmer)
								{
									color = Custom.HSL2RGB(((obj as SSOracleSwarmer).color.x > 0.5f) ? Custom.LerpMap((obj as SSOracleSwarmer).color.x, 0.5f, 1f, 0.6666667f, 0.99722224f) : 0.6666667f, 1f, Mathf.Lerp(0.75f, 0.9f, (obj as SSOracleSwarmer).color.y));
								}
								self.room.AddObject(new Spark(obj.firstChunk.pos, Custom.RNV() * 60f * UnityEngine.Random.value, color, null, 20, 50));
								obj.Destroy();
							}
							self.firstChunk.vel /= 2.2f;
							foreach (AbstractCreature abstractCreature in self.room.abstractRoom.creatures)
							{
								if (ModManager.DLCShared && abstractCreature != null && abstractCreature.creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.Inspector && abstractCreature.realizedCreature != null && self.thrownBy != null && (abstractCreature.realizedCreature as Inspector).AI.VisualContact(self.thrownBy.firstChunk) && (abstractCreature.realizedCreature as Inspector).AI.VisualContact(self.firstChunk))
								{
									(abstractCreature.realizedCreature as Inspector).AI.preyTracker.AddPrey((abstractCreature.realizedCreature as Inspector).AI.tracker.RepresentationForCreature(self.thrownBy.abstractCreature, true));
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

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to feed from Spearmaster needles dynamically.
				/// </summary>
				private static void SpearHitSomethingDiet(ILContext il)
				{
					try
					{
						// if (result.obj is Creature && (result.obj as Creature).SpearStick(this, Mathf.Lerp(0.55f, 0.62f, UnityEngine.Random.value), result.chunk, result.onAppendagePos, base.firstChunk.vel))
						ILCursor cursor = new(il);
						cursor.GotoNext(MoveType.After,
							x => x.MatchCallOrCallvirt<PhysicalObject.IHaveAppendages>(nameof(PhysicalObject.IHaveAppendages.ApplyForceOnAppendage)));

						// this.SetRandomSpin(); return;
						ILCursor jumpCursor = cursor.Clone();
						jumpCursor.GotoNext(MoveType.Before,
							x => x.MatchLdcI4(1),
							x => x.MatchRet());
						ILLabel jumpTo = jumpCursor.MarkLabel();

						cursor.MoveAfterLabels(); // This tells every past label pointing to our cursor to change to move to OUR code first wowie :D
						cursor.Emit(OpCodes.Ldarg_0);
						ILCursor previousJumpCursor = cursor.Clone();
						cursor.Emit(OpCodes.Ldarg_1);
						cursor.Emit(OpCodes.Ldarg_2);
						cursor.Emit(OpCodes.Ldloc, 0);
						static bool SlugbaseFoodConditions(Spear self, SharedPhysics.CollisionResult result, bool eu, bool stuckInCreature)
						{
							if (self.Spear_NeedleCanFeed() && self.thrownBy is Player player && PlayerFeatures.Diet.TryGet(player, out var diet))
							{
								bool fed = false;
								if (result.obj is Creature creature)
								{
									float multiplier = 1f;
									multiplier = diet.CreatureOverrides.TryGetValue(creature.Template.type, out var mult) ? mult : diet.Meat;
									if (!creature.dead && multiplier > 0f)
									{
										fed = player.ProcessFood(multiplier);
										creature.State.meatLeft -= creature.Template.meatPoints > 0 ? 1 : 0;
									}
									if (creature.dead && diet.Corpses > 0f && creature.State.meatLeft > 0)
									{
										fed = player.ProcessFood(diet.GetMeatMultiplier(player, creature));
										creature.State.meatLeft--;
									}
								}
								if (result.obj is IPlayerEdible edible && edible.Edible && result.obj is not GooieDuck && diet.GetFoodMultiplier(result.obj) > 0f)
								{
									fed = player.ProcessFood(diet.GetFoodMultiplier(result.obj));
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
										self.room.AddObject(new WaterDrip(result.obj.firstChunk.pos, (self.firstChunk.vel / UnityEngine.Random.Range(1.7f, 4f)) + new Vector2(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-2f, 2f)), false));
									}
									self.firstChunk.vel /= 2f;
									if (result.obj is not Creature)
										result.obj.Destroy();
								}
								if (result.obj is GooieDuck duck)
								{
									if (duck.bites == 6)
									{
										self.room.PlaySound(DLCSharedEnums.SharedSoundID.Duck_Pop, result.obj.firstChunk, false, 1f, 0.5f + UnityEngine.Random.value * 0.5f);
									}
									if (!duck.StringsBroke && duck.bites - 2 <= 0)
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
										self.stuckInObject = duck;
										self.stuckInChunkIndex = 0;
									}
								}
								if (result.obj is SeedCob popCorn && diet.GetFoodMultiplier(popCorn) > 0f)
								{
									popCorn.Open();
									fed = player.ProcessFood(diet.GetFoodMultiplier(popCorn));
								}
								if (result.obj is JellyFish jelly)
								{
									if (jelly.dead && diet.Corpses > 0f)
									{
										fed = player.ProcessFood(diet.GetFoodMultiplier(jelly));
										jelly.Destroy();
									}
									else if (!jelly.dead)
									{
										fed = player.ProcessFood(diet.GetFoodMultiplier(jelly));
										jelly.dead = true;
									}
								}
								if (result.obj is Pomegranate pomegranate && pomegranate.smashed)
								{
									fed = player.ProcessFood(diet.GetFoodMultiplier(pomegranate));
									pomegranate.spearmasterStabbed = true;
								}

								if (fed && self.room.game.IsStorySession && self.room.game.GetStorySession.playerSessionRecords != null)
								{
									self.room.game.GetStorySession.playerSessionRecords[(player.abstractCreature.state as PlayerState).playerNumber].AddEat(result.obj);
								}

								self.Spear_NeedleDisconnect();
								self.room.PlaySound(SoundID.Spear_Stick_In_Creature, self.firstChunk);
								self.LodgeInCreature(result, eu, result.obj is JellyFish);
								if (stuckInCreature)
								{
									self.abstractPhysicalObject.world.game.GetArenaGameSession.PlayerLandSpear(self.thrownBy as Player, self.stuckInObject as Creature);
								}
								return true;
							}
							return false;
						}
						cursor.EmitDelegate(SlugbaseFoodConditions);
						cursor.Emit(OpCodes.Brtrue, jumpTo);
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Disallows <see cref="SlugBaseCharacter"/> to pop <see cref="SeedCob"/> objects if they feed from needles.
				/// </summary>
				private static void SeedCobReactToFeedSpear(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);

						if (cursor.MoveToNextSlugcat(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear))))
						{
							cursor.Emit(OpCodes.Ldarg_1);
							static bool FeedsOffSpears(bool isSpear, Weapon spear)
							{
								return isSpear || (spear.thrownBy is Player player && player.HasFeature(ExtFeatures.forceFeedingFromSpears, out bool flag) && flag);
							}
							cursor.EmitDelegate(FeedsOffSpears);
						}
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Disallows <see cref="SlugBaseCharacter"/> to pop <see cref="SeedCob"/> objects if they feed from needles.
				/// </summary>
				private static void SeedCobReactToSpear(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);

						if (cursor.MoveToNextSlugcat(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear))))
						{
							cursor.Emit(OpCodes.Ldloc, 12);
							static bool FeedsOffSpears(bool isNotSpear, Player player)
							{
								return isNotSpear && (!player.HasFeature(ExtFeatures.forceFeedingFromSpears, out bool flag) || !flag);
							}
							cursor.EmitDelegate(FeedsOffSpears);
						}
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}
			}

			/// <summary>
			/// Applies <see cref="MoreSlugcatsEnums.SlugcatStatsName.Artificer"/> specific Hooks.
			/// </summary>
			internal class ArtificerHooks
			{
				// Shorthand for an otherwise longly-typed method
				private static bool TryNext(ILCursor cursor)
				{
					return cursor.MoveToNextSlugcat(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer)));
				}
				// Various IL bools that we'd insert onto the stack
				private static bool ExplosiveCraft(bool result, Player player)
				{
					return result || (player.HasFeature(ExtFeatures.explosiveCraftCost, out var explosiveCost) && (explosiveCost.Length == 1 || explosiveCost[1] == player.FreeHand()));
				}
				private static bool ExplosiveJumps(bool result, Player player)
				{
					return result || (!player.input[5].pckp && player.HasFeature(ExtFeatures.explosiveCraftCost));
				}
				private static bool NoExplosiveJumps(bool result, Player player)
				{
					return result && player.HasFeature(ExtFeatures.explosiveCraftCost, false);
				}
				private static bool ScavCorpseKarma(bool result, Player player)
				{
					return result || player.HasFeature(ExtFeatures.getKarmaFromScavs);
				}

				internal static void Apply()
				{
					try
					{
						IL.Player.CanIPickThisUp += Player_CanIPickThisUp;
						IL.Player.GrabUpdate += Player_GrabUpdate;
						IL.Player.Update += Player_Update;
						IL.Player.SpitUpCraftedObject += Player_SpitUpCraftedObject;
						IL.Player.CraftingResults += Player_CraftingResults;
						IL.Player.ClassMechanicsArtificer += Player_ClassMechanicsArtificer;
						new ILHook(typeof(RegionGate).GetProperty(nameof(RegionGate.MeetRequirement), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod(), GateMeetRequirement);
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to pull spears out of walls.
				/// </summary>
				private static void Player_CanIPickThisUp(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);

						if (TryNext(cursor))
						{
							static bool CanPullSpearOut(bool isNotArti, Player self)
							{
								return isNotArti && self.HasFeature(ExtFeatures.pullSpearsFromWalls, false);
							}
							cursor.ImplementILCodeAssumingLdarg0(CanPullSpearOut);
						}
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
					
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to use various Artificer mechanics.
				/// </summary>
				private static void Player_GrabUpdate(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);

						if (TryNext(cursor)) // if (ModManager.MSC && (this.FreeHand() == -1 || this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer) && this.GraspsCanBeCrafted())
						{
							cursor.ImplementILCodeAssumingLdarg0(ExplosiveCraft);

							if (TryNext(cursor)) // if (!ModManager.MSC || this.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Artificer || !(base.grasps[num20].grabbed is Scavenger))
							{
								cursor.ImplementILCodeAssumingLdarg0(NoExplosiveJumps);
							}
						}
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
					
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to gain karma from holding <see cref="Scavenger"/> corpses.
				/// </summary>
				/// <param name="il"></param>
				private static void Player_Update(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);

						if (TryNext(cursor)) // base.Hypothermia -= Mathf.Lerp(RainWorldGame.DefaultHeatSourceWarmth, 0f, this.HypothermiaExposure);
						{
							// Introduce Artificer warmth mechanic? Likely just better to introduce a general version that's dynamic

							if (TryNext(cursor)) // else if (ModManager.MSC && this.room.game.IsStorySession && this.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer && this.slugcatStats.name == MoreSlugcatsEnums.SlugcatStatsName.Artificer && this.AI == null && this.room.game.cameras[0] != null && this.room.game.cameras[0].hud != null && this.room.game.cameras[0].hud.karmaMeter != null)
							{
								cursor.ImplementILCodeAssumingLdarg0(ScavCorpseKarma);

								if (TryNext(cursor)) // else if (ModManager.MSC && this.room.game.IsStorySession && this.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer && this.slugcatStats.name == MoreSlugcatsEnums.SlugcatStatsName.Artificer && this.AI == null && this.room.game.cameras[0] != null && this.room.game.cameras[0].hud != null && this.room.game.cameras[0].hud.karmaMeter != null)
								{
									cursor.ImplementILCodeAssumingLdarg0(ScavCorpseKarma);
								}
							}
						}
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to craft explosives.
				/// </summary>
				private static void Player_SpitUpCraftedObject(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);

						if (TryNext(cursor))
						{
							cursor.ImplementILCodeAssumingLdarg0(ExplosiveCraft);
						}
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to craft explosives.
				/// </summary>
				private static void Player_CraftingResults(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);

						if (TryNext(cursor))
						{
							cursor.ImplementILCodeAssumingLdarg0(ExplosiveCraft);
						}
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Changes the explosive jump limits, if <see cref="ExtFeatures.explosiveJumpLimits"/> is true.
				/// </summary>
				private static void Player_ClassMechanicsArtificer(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);

						if (TryNext(cursor))
						{
							cursor.ImplementILCodeAssumingLdarg0(ExplosiveJumps);

							if (cursor.TryGotoNext(MoveType.After, x => x.MatchStloc(2))) // int num = Mathf.Max(1, MoreSlugcats.cfgArtificerExplosionCapacity.Value - 5);
							{
								cursor.Emit(OpCodes.Ldarg_0); // push Player onto stack
								cursor.Emit(OpCodes.Ldloc, 2); // push num onto stack
								static int ActualLimit(Player self, int softExplosiveLimit)
								{
									if (self.HasFeature(ExtFeatures.explosiveJumpLimits, out int[] limits))
									{
										return limits.Length == 2 ? Math.Max(1, limits[1] - limits[0]) : (limits[0] / 3) + (limits[0] / 4);
									}
									return softExplosiveLimit;
								}
								cursor.EmitDelegate(ActualLimit);
								cursor.Emit(OpCodes.Stloc, 2);


								if (cursor.TryGotoNext(MoveType.After, x => x.MatchStloc(4))) // int num2 = Mathf.Max(1, MoreSlugcats.cfgArtificerExplosionCapacity.Value - 3);
								{
									cursor.Emit(OpCodes.Ldarg_0); // push Player onto stack
									cursor.Emit(OpCodes.Ldloc, 4); // push num onto stack
									static int StunLimit(Player self, int stunExplosiveLimit)
									{
										if (self.HasFeature(ExtFeatures.explosiveJumpLimits, out int[] limits))
										{
											return limits.Length == 2 ? Math.Max(1, limits[1] - (limits[0] / 2)) : (limits[0] / 2) + (limits[0] / 4);
										}
										return stunExplosiveLimit;
									}
									cursor.EmitDelegate(StunLimit);
									cursor.Emit(OpCodes.Stloc, 4);

									if (cursor.TryGotoNext(x => x.MatchBlt(out _),
										x => x.MatchLdarg(0),
										x => x.MatchCallOrCallvirt<Player>(nameof(Player.PyroDeath))))
									{
										// if (this.pyroJumpCounter >= MoreSlugcats.cfgArtificerExplosionCapacity.Value)

										ILLabel jumpLabel = cursor.Next.Operand as ILLabel;
										cursor.GotoPrev(x => x.MatchLdarg(0), x => x.MatchLdfld<Player>(nameof(Player.pyroJumpCounter)));

										cursor.MoveAfterLabels();
										cursor.Emit(OpCodes.Ldarg_0);
										static bool ExplosiveLimitReached(Player self)
										{
											return self.HasFeature(ExtFeatures.explosiveJumpLimits, out int[] limits) && self.pyroJumpCounter >= limits.Last();
										}
										cursor.EmitDelegate(ExplosiveLimitReached);
										cursor.Emit(OpCodes.Brfalse, jumpLabel);

										if (cursor.TryGotoNext(MoveType.After, x => x.MatchStloc(12))) // int num7 = Mathf.Max(1, MoreSlugcats.cfgArtificerExplosionCapacity.Value - 3);
										{
											cursor.Emit(OpCodes.Ldarg_0); // push Player onto stack
											cursor.Emit(OpCodes.Ldloc, 12); // push num onto stack
											cursor.EmitDelegate(StunLimit);
											cursor.Emit(OpCodes.Stloc, 12);

											if (cursor.TryGotoNext(x => x.MatchBlt(out _),
												x => x.MatchLdarg(0),
												x => x.MatchCallOrCallvirt<Player>(nameof(Player.PyroDeath))))
											{
												// if (this.pyroJumpCounter >= MoreSlugcats.cfgArtificerExplosionCapacity.Value)

												ILLabel jumpLabel2 = cursor.Next.Operand as ILLabel;
												cursor.GotoPrev(x => x.MatchLdarg(0), x => x.MatchLdfld<Player>(nameof(Player.pyroJumpCounter)));

												cursor.MoveAfterLabels();
												cursor.Emit(OpCodes.Ldarg_0);
												cursor.EmitDelegate(ExplosiveLimitReached);
												cursor.Emit(OpCodes.Brfalse, jumpLabel2);
											}
										}
									}
								}
							}
						}

					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				private static void GateMeetRequirement(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);

						if (TryNext(cursor))
						{
							cursor.Emit(OpCodes.Ldarg_0);
							cursor.Emit(OpCodes.Ldloc_1);
							static bool ScavCorpseKarmaGate(bool isArti, RegionGate gate, Player player)
							{
								return isArti || player.HasFeature(ExtFeatures.getKarmaFromScavs);
							}
							cursor.EmitDelegate(ScavCorpseKarmaGate);
						}
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}
			}

			/// <summary>
			/// Applies <see cref="MoreSlugcatsEnums.SlugcatStatsName.Gourmand"/> specific Hooks.
			/// </summary>
			internal class GourmandHooks
			{
				// Shorthand for an otherwise longly-typed method
				private static bool TryNext(ILCursor cursor)
				{
					return cursor.MoveToNextSlugcat(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Gourmand)));
				}
				// Various IL bools that we'd insert onto the stack
				private static bool CanSlam(bool result, Player player)
				{
					return result || player.HasFeature(ExtFeatures.canSlam);
				}
				// Various IL bools that we'd insert onto the stack
				private static bool CannotSlam(bool result, Player player)
				{
					return result && player.HasFeature(ExtFeatures.canSlam, false);
				}

				internal static void Apply()
				{
					try
					{
						IL.Player.Collide += Player_Collide;
						IL.Player.SlugSlamConditions += Player_SlugSlamConditions;
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to inflict damage from momentum.
				/// </summary>
				private static void Player_Collide(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);
						// if (!this.isGourmand && this.animation == Player.AnimationIndex.BellySlide)
						if (cursor.TryGotoNext(MoveType.After,
							x => x.MatchCallOrCallvirt(typeof(Player).GetProperty(nameof(Player.isGourmand), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod())))
						{
							cursor.MoveAfterLabels();
							cursor.ImplementILCodeAssumingLdarg0(CanSlam);

							if (TryNext(cursor)) // if (this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Gourmand && this.animation == Player.AnimationIndex.Roll && this.gourmandAttackNegateTime <= 0)
							{
								cursor.ImplementILCodeAssumingLdarg0(CanSlam);
							}
						}
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to inflict damage from momentum.
				/// </summary>
				private static void Player_SlugSlamConditions(ILContext il)
				{
					ILCursor cursor = new(il);

					if (TryNext(cursor)) // if (this.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
					{
						cursor.ImplementILCodeAssumingLdarg0(CannotSlam);
					}
				}
			}

			internal class RivuletHooks
			{
				private static bool TryNext(ILCursor cursor)
				{
					return cursor.MoveToNextSlugcat(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Rivulet)));
				}

				internal static void Apply()
				{
					IL.WaterNut.Update += WaterNut_Update;
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to pop <see cref="WaterNut"/> by holding them.
				/// </summary>
				/// <param name="il"></param>
				private static void WaterNut_Update(ILContext il)
				{
					ILCursor cursor = new(il);

					if (TryNext(cursor))
					{
						static bool CanPopBubbleFruit(bool isRivulet, int loop, WaterNut self)
						{
							return isRivulet || (self.grabbedBy[loop].grabber is Player player && player.HasFeature(ExtFeatures.popBubbleFruit));
						}

						cursor.Emit(OpCodes.Ldloc_1);
						cursor.ImplementILCodeAssumingLdarg0(CanPopBubbleFruit);
					}
				}
			}

			/// <summary>
			/// Applies <see cref="MoreSlugcatsEnums.SlugcatStatsName.Saint"/> specific Hooks.
			/// </summary>
			internal class SaintHooks
			{
				private static bool TryNext(ILCursor cursor)
				{
					return cursor.MoveToNextSlugcat(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint)));
				}

				internal static void Apply()
				{
					IL.Player.ThrowObject += Player_ThrowObject;
				}

				/// <summary>
				/// Forces <see cref="SlugBaseCharacter"/> to toss spears like Saint, when <see cref="ExtFeatures.tossSpears"/> is true.
				/// </summary>
				/// <param name="il"></param>
				private static void Player_ThrowObject(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);

						if (TryNext(cursor))
						{
							static bool TossSpears(bool isSaint, Player self)
							{
								return isSaint || self.HasFeature(ExtFeatures.tossSpears);
							}
							cursor.ImplementILCodeAssumingLdarg0(TossSpears);
						}
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}
			}
		}

		/// <summary>
		/// Handles hooks to the <see cref="PlayerGraphics"/> class, or classes that reference it.
		/// </summary>
		public class GraphicHooks
		{
			private static Color lastBlackColor;

			internal static void ApplyHooks()
			{
				GeneralHooks.Apply();
				SpearmasterHooks.Apply();
				RivuletHooks.Apply();
				SaintHooks.Apply();
			}

			internal class GeneralHooks
			{
				internal static void Apply()
				{
					On.PlayerGraphics.ctor += Initialize;
					On.PlayerGraphics.Update += PlayerGraphics_Update;
					IL.PlayerGraphics.InitiateSprites += InitateCustomSprites;
					On.PlayerGraphics.AddToContainer += AddToContainer;
					On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
					On.PlayerGraphics.DrawSprites += DrawSprites;
					IL.PlayerGraphics.DefaultBodyPartColorHex += DefaultBodyPartColor;
					IL.PlayerGraphics.ColoredBodyPartList += DefaultColoredBodyPartList;
					On.PlayerGraphics.DefaultFaceSprite_float_int += PlayerGraphics_DefaultFaceSprite;
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to have gills, a fade-in mark, and tail specks.
				/// </summary>
				private static void Initialize(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
				{
					orig(self, ow);

					if (SlugBaseCharacter.TryGet(self.player.SlugCatClass, out _))
					{
						int startSprite = 12;
						if (ModManager.MSC && self.player.HasFeature(ExtFeatures.numOfRivGills))
						{
							self.gills = new(self, startSprite);
							startSprite += self.gills.numberOfSprites;
						}

						if (ModManager.MSC && self.player.HasFeature(ExtFeatures.rowsAndColumnsSpearSpecks))
						{
							self.tailSpecks = new PlayerGraphics.TailSpeckles(self, startSprite);
							startSprite += self.tailSpecks.numberOfSprites;
						}

						if (self.player.abstractCreature.world.game.IsStorySession && GameFeatures.TheMark.TryGet(self.player.abstractCreature.world.game, out bool hasMark) && hasMark && self.player.abstractCreature.world.game.HasFeature(ExtFeatures.revealMarkOverTotalCycles, out int cycles))
						{
							self.markBaseAlpha = Mathf.Pow(Mathf.InverseLerp(4f, (float)cycles, (float)self.player.abstractCreature.world.game.GetStorySession.saveState.cycleNumber), 3.5f);
						}
					}
				}

				/// <summary>
				/// Updates various body parts for custom graphics.
				/// </summary>
				private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
				{
					orig(self);

					if (ModManager.MSC && self.player.HasFeature(ExtFeatures.numOfRivGills))
					{
						self.gills.Update();
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to have gills, tail specks, and a fluffy head.
				/// </summary>
				private static void InitateCustomSprites(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);
						// gown.InitiateSprite(this.gownIndex, sLeaser, rCam);
						if (cursor.TryGotoNext(MoveType.After, x => x.MatchCall<PlayerGraphics.Gown>(nameof(PlayerGraphics.Gown.InitiateSprite))))
						{
							cursor.Emit(OpCodes.Ldarg_0);
							cursor.Emit(OpCodes.Ldarg_1);
							cursor.Emit(OpCodes.Ldarg_2);
							static void InitiateSprites(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
							{
								if (ModManager.MSC && self.player.HasFeature(ExtFeatures.hasSaintHead))
								{
									sLeaser.sprites[3].SetElementByName("HeadB0");
								}

								if (ModManager.MSC && self.player.HasFeature(ExtFeatures.numOfRivGills))
								{
									self.gills.startSprite = sLeaser.sprites.Length;
									Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + self.gills.numberOfSprites);

									self.gills.InitiateSprites(sLeaser, rCam);
								}

								if (ModManager.MSC && self.player.HasFeature(ExtFeatures.rowsAndColumnsSpearSpecks))
								{
									self.tailSpecks.startSprite = sLeaser.sprites.Length;
									Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + self.tailSpecks.numberOfSprites);

									self.tailSpecks.InitiateSprites(sLeaser, rCam);
								}
							}
							cursor.EmitDelegate(InitiateSprites);
						}

					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to have gills and tail specks.
				/// </summary>
				private static void AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
				{
					orig(self, sLeaser, rCam, newContatiner);

					if (ModManager.MSC && self.player.HasFeature(ExtFeatures.numOfRivGills) && sLeaser.sprites.Length > self.gills.startSprite)
					{
						self.gills.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Midground"));
					}

					if (ModManager.MSC && self.player.HasFeature(ExtFeatures.rowsAndColumnsSpearSpecks) && sLeaser.sprites.Length > self.tailSpecks.startSprite)
					{
						self.tailSpecks.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Midground"));
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to color gills. Also automatically replaces pure black (transparent) with the room palette's black color.
				/// </summary>
				private static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
				{
					if (SlugBaseCharacter.TryGet(self.player.SlugCatClass, out var character2) && PlayerFeatures.CustomColors.TryGet(character2, out var slots2))
					{
						float lerp = ExtFeatures.watcherBlackLerpAmount.TryGet(self.player, out float amount) ? amount : 1f;
						Color watcherCol = Color.Lerp(palette.blackColor, Custom.HSL2RGB(0.63055557f, 0.54f, 0.5f), Mathf.Lerp(0.08f, 0.04f, palette.darkness) * lerp);
						for (int i = 0; i < slots2.Length; i++)
						{
							if (slots2[i] != null)
							{
								if (slots2[i].Default == Color.black || slots2[i].Default == lastBlackColor)
									slots2[i].Default = watcherCol;

								for (int j = 0; j < 4; j++)
								{
									if (slots2[i].Variants.Length > j + 1 && (slots2[i].Variants[j] == Color.black || slots2[i].Variants[j] == lastBlackColor))
									{
										slots2[i].Variants[j] = watcherCol;
									}
								}

							}
						}
						lastBlackColor = watcherCol;
					}

					orig(self, sLeaser, rCam, palette);


					if (ModManager.MSC && self.player.HasFeature(ExtFeatures.numOfRivGills) && SlugBaseCharacter.TryGet(self.player.SlugCatClass, out var character))
					{
						if (PlayerFeatures.CustomColors.TryGet(character, out var slots) && slots.Length > 1)
						{
							ColorSlot slot = slots.Where(x => x.Name == "Gills").FirstOrDefault();
							Color defaultCol = slots[0].Default;

							if (slot != null)
							{
								if (self.player.abstractCreature.world.game.IsArenaSession && slot != null)
								{
									int playerNum = self.player.playerState.playerNumber;
									if (slot.Variants.Length >= playerNum + 1)
									{
										self.gills.SetGillColors(defaultCol, slot.Variants[playerNum]);
									}
								}
								else if (defaultCol != null)
								{
									self.gills.SetGillColors(defaultCol, slot.Default);
								}
							}
						}

						self.gills.ApplyPalette(sLeaser, rCam, palette);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to have gills, tail specks, and a fluffy head.
				/// </summary>
				private static void DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
				{
					orig(self, sLeaser, rCam, timeStacker, camPos);

					if (ModManager.MSC && self.player.HasFeature(ExtFeatures.hasSaintHead) && !sLeaser.sprites[3].element.name.Contains("HeadB"))
					{
						sLeaser.sprites[3].SetElementByName($"HeadB{sLeaser.sprites[3].element.name.Substring("HeadA".Length)}");
					}

					if (ModManager.MSC && self.player.HasFeature(ExtFeatures.numOfRivGills))
					{
						self.gills.DrawSprites(sLeaser, rCam, timeStacker, camPos);
					}

					if (ModManager.MSC && self.player.HasFeature(ExtFeatures.rowsAndColumnsSpearSpecks))
					{
						self.tailSpecks.DrawSprites(sLeaser, rCam, timeStacker, camPos);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to have gills and tail specks colors on custom color menu.
				/// </summary>
				private static void DefaultBodyPartColor(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);
						// return list;
						cursor.GotoNext(x => x.MatchRet());

						cursor.Emit(OpCodes.Ldarg_0);
						cursor.Emit(OpCodes.Ldloc, 0);
						static void CustomColors(SlugcatStats.Name name, List<string> colors)
						{
							if (SlugBaseCharacter.TryGet(name, out var character) && PlayerFeatures.CustomColors.TryGet(character, out var slots) && slots.Any(x => x.Name == "Gills"))
							{
								colors.Add(Custom.colorToHex(slots.Where(x => x.Name == "Gills").FirstOrDefault().Default));
							}

							if (SlugBaseCharacter.TryGet(name, out var character2) && PlayerFeatures.CustomColors.TryGet(character2, out var slots2) && slots2.Any(x => x.Name == "Spears"))
							{
								colors.Add(Custom.colorToHex(slots2.Where(x => x.Name == "Spears").FirstOrDefault().Default));
							}
						}
						cursor.EmitDelegate(CustomColors);
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to have gills and tail specks colors on custom color menu.
				/// </summary>
				private static void DefaultColoredBodyPartList(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);
						// return list;
						cursor.GotoNext(x => x.MatchRet());

						cursor.Emit(OpCodes.Ldarg_0);
						cursor.Emit(OpCodes.Ldloc_0);
						static void HasFeatures(SlugcatStats.Name name, List<string> parts)
						{
							if (SlugBaseCharacter.TryGet(name, out var character) && PlayerFeatures.CustomColors.TryGet(character, out var slots) && slots.Any(x => x.Name == "Gills"))
							{
								parts.Add("Gills");
							}

							if (SlugBaseCharacter.TryGet(name, out var character2) && PlayerFeatures.CustomColors.TryGet(character2, out var slots2) && slots2.Any(x => x.Name == "Spears"))
							{
								parts.Add("Spears");
							}
						}
						cursor.EmitDelegate(HasFeatures);
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to use Artificer's face.
				/// </summary>
				private static string PlayerGraphics_DefaultFaceSprite(On.PlayerGraphics.orig_DefaultFaceSprite_float_int orig, PlayerGraphics self, float eyeScale, int imgIndex)
				{
					bool artiEyes = self.player.HasFeature(ExtFeatures.hasArtiFace);
					if (!(ModManager.MSC && self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Slugpup && self.player.room != null && self.player.room.world.game.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel) && artiEyes)
					{
						int num = 0;
						if (self.RenderAsPup && ModManager.MSC)
						{
							num = 1;
						}
						int j;
						if (self.blink <= 0 && !self.SaintFaceCondition())
						{
							if (ModManager.MSC && artiEyes && num != 1)
							{
								if (eyeScale < 0f)
								{
									j = 3;
								}
								else
								{
									j = 2;
								}
							}
							else
							{
								j = 0;
							}
						}
						else
						{
							j = 1;
						}

						return self._cachedFaceSpriteNames[num, j, imgIndex];
					}

					return orig(self, eyeScale, imgIndex);
				}
			}

			internal class SpearmasterHooks
			{
				internal static void Apply()
				{
					IL.PlayerGraphics.TailSpeckles.ctor += TailSpeckles_ctor;
					On.PlayerGraphics.TailSpeckles.DrawSprites += TailSpeckles_DrawSprites;
					On.Spear.DrawSprites += Spear_DrawSprites;
					On.Spear.Umbilical.ApplyPalette += Spear_Umbilical_ApplyPalette;
				}

				/// <summary>
				/// Changes the amount of <see cref="PlayerGraphics.TailSpeckles.rows"/> and <see cref="PlayerGraphics.TailSpeckles.lines"/> the <see cref="Player"/> spawns with.
				/// </summary>
				private static void TailSpeckles_ctor(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);

						if (cursor.TryGotoNext(MoveType.After,
							x => x.MatchStfld<PlayerGraphics.TailSpeckles>(nameof(PlayerGraphics.TailSpeckles.lines)),
							x => x.MatchLdarg(0)))
						{
							cursor.Emit(OpCodes.Ldarg_0);
							cursor.Emit(OpCodes.Ldarg_1);
							static void HandleRowsAndColumns(PlayerGraphics.TailSpeckles self, PlayerGraphics pGraphics)
							{
								if (pGraphics.player.HasFeature(ExtFeatures.rowsAndColumnsSpearSpecks, out var specks))
								{
									if (specks[0] > -1)
										self.rows = specks[0];

									if (specks.Length > 1 && specks[1] > -1)
										self.lines = specks[1];
								}
							}
							cursor.EmitDelegate(HandleRowsAndColumns);
						}
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to color tail specks.
				/// </summary>
				private static void TailSpeckles_DrawSprites(On.PlayerGraphics.TailSpeckles.orig_DrawSprites orig, PlayerGraphics.TailSpeckles self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
				{
					orig(self, sLeaser, rCam, timeStacker, camPos);

					if (self.pGraphics.player.HasFeature(ExtFeatures.rowsAndColumnsSpearSpecks) && SlugBaseCharacter.TryGet(self.pGraphics.player.SlugCatClass, out var character))
					{
						Color color = new();
						if (PlayerFeatures.CustomColors.TryGet(character, out var slots) && slots.Length > 1)
						{
							ColorSlot slot = slots.Where(x => x.Name == "Spears").FirstOrDefault();
							if (slot != null)
							{
								if (self.pGraphics.player.abstractCreature.world.game.IsArenaSession && slot.Variants != null)
								{
									int playerNum = self.pGraphics.player.playerState.playerNumber;
									if (slot.Variants.Length >= playerNum + 1)
									{
										color = slot.Variants[playerNum];
									}
								}
								else if (slot.Default != null)
								{
									color = slot.Default;
								}

								for (int i = 0; i < self.rows; i++)
								{
									for (int j = 0; j < self.lines; j++)
									{
										sLeaser.sprites[self.startSprite + i * self.lines + j].color = color;

										if (i == self.spearRow && j == self.spearLine)
										{
											sLeaser.sprites[self.startSprite + self.lines * self.rows].color = color;
										}
									}
								}
							}
						}
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to color spearmaster spears.
				/// </summary>
				private static void Spear_DrawSprites(On.Spear.orig_DrawSprites orig, Spear self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
				{
					orig(self, sLeaser, rCam, timeStacker, camPos);


					if (CWTs.spearCWT.TryGetValue(self.abstractSpear, out var cwt) && cwt.slugColor != null)
					{
						float lerp = (float)self.spearmasterNeedle_fadecounter / (float)self.spearmasterNeedle_fadecounter_max;
						if (self.spearmasterNeedle_hasConnection)
						{
							lerp = 1f;
						}
						if (lerp < 0.01f)
						{
							lerp = 0.01f;
						}
						sLeaser.sprites[0].color = Color.Lerp(cwt.slugColor.Value, self.color, 1f - lerp);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to color spearmaster umbilical cords.
				/// </summary>
				private static void Spear_Umbilical_ApplyPalette(On.Spear.Umbilical.orig_ApplyPalette orig, Spear.Umbilical self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
				{
					orig(self, sLeaser, rCam, palette);

					if (self.spider.HasFeature(ExtFeatures.forceFeedingFromSpears) && CWTs.spearCWT.TryGetValue(self.maggot.abstractSpear, out var col) && col.slugColor != null)
					{
						self.threadCol = col.slugColor.Value;
						self.fogColor = Color.Lerp(palette.fogColor, col.slugColor.Value, 0.8f);
					}
				}
			}

			internal class RivuletHooks
			{
				internal static void Apply()
				{
					IL.PlayerGraphics.AxolotlGills.ctor += AxolotlGills_ctor;
				}

				/// <summary>
				/// Changes the amount of <see cref="PlayerGraphics.AxolotlGills.graphic"/>, or gill rows the <see cref="Player"/> spawns with.
				/// </summary>
				/// <param name="il"></param>
				private static void AxolotlGills_ctor(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);

						if (cursor.TryGotoNext(MoveType.After,
							x => x.MatchStloc(1)))
						{
							cursor.Emit(OpCodes.Ldloc_1);
							cursor.Emit(OpCodes.Ldarg_1);
							static int RivuletGills(int gillAmount, PlayerGraphics pGraphics)
							{
								if (pGraphics.player.HasFeature(ExtFeatures.numOfRivGills, out var gillsRows))
									gillAmount = gillsRows;

								return gillAmount;
							}
							cursor.EmitDelegate(RivuletGills);
							cursor.Emit(OpCodes.Stloc_1);
						}
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}
			}

			internal class SaintHooks
			{
				internal static void Apply()
				{
					On.PlayerGraphics.SaintFaceCondition += PlayerGraphics_SaintFaceCondition;
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to have Saint's closed eyes.
				/// </summary>
				private static bool PlayerGraphics_SaintFaceCondition(On.PlayerGraphics.orig_SaintFaceCondition orig, PlayerGraphics self)
				{
					return orig(self) || self.player.HasFeature(ExtFeatures.usesSaintFaceCondition);
				}
			}
		}
	}
}