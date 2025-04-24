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

namespace ExtendedSlugbaseFeatures
{
	partial class Plugin
	{
		internal class PlayerHooks
		{
			internal static void Apply()
			{
				On.Player.ctor += MechanicHooks.Player_ctor;
				IL.Player.Update += MechanicHooks.Player_Update1;
				IL.Player.Collide += MechanicHooks.Player_Collide;
				IL.Player.SlugSlamConditions += MechanicHooks.Player_SlugSlamConditions;
				IL.Player.GrabUpdate += MechanicHooks.Player_GrabUpdate;
				On.Player.Grabability += MechanicHooks.Player_Grabability;
				On.Player.GraspsCanBeCrafted += MechanicHooks.Player_GraspsCanBeCrafted;
				On.Player.CanEatMeat += MechanicHooks.Player_CanEatMeat;
				On.Player.BiteEdibleObject += MechanicHooks.Player_BiteEdibleObject;
				On.Player.CanBeSwallowed += MechanicHooks.Player_CanBeSwallowed;
				IL.Player.SwallowObject += MechanicHooks.Player_SwallowObject;
				IL.Player.SpitUpCraftedObject += MechanicHooks.Player_SpitUpCraftedObject;
				IL.Player.CraftingResults += MechanicHooks.Player_CraftingResults;
				IL.Player.ClassMechanicsSpearmaster += MechanicHooks.Player_ClassMechanicsSpearmaster;
				IL.Player.ClassMechanicsArtificer += MechanicHooks.Player_ClassMechanicsArtificer;

				On.Spear.Spear_NeedleCanFeed += MechanicHooks.Spear_Spear_NeedleCanFeed;
				On.Spear.HitSomethingWithoutStopping += MechanicHooks.Spear_HitSomethingWithoutStopping;
				IL.Spear.HitSomething += MechanicHooks.Spear_HitSomething;
				IL.SeedCob.HitByWeapon += MechanicHooks.SeedCob_HitByWeapon;
				IL.SeedCob.Update += MechanicHooks.SeedCob_Update;

				On.PlayerGraphics.ctor += GraphicHooks.PlayerGraphics_ctor;
				On.PlayerGraphics.Update += GraphicHooks.PlayerGraphics_Update;
				IL.PlayerGraphics.InitiateSprites += GraphicHooks.IL_PlayerGraphics_InitiateSprites;
				On.PlayerGraphics.AddToContainer += GraphicHooks.PlayerGraphics_AddToContainer;
				On.PlayerGraphics.ApplyPalette += GraphicHooks.PlayerGraphics_ApplyPalette;
				IL.PlayerGraphics.DefaultBodyPartColorHex += GraphicHooks.PlayerGraphics_DefaultBodyPartColorHex;
				IL.PlayerGraphics.ColoredBodyPartList += GraphicHooks.PlayerGraphics_ColoredBodyPartList;
				On.PlayerGraphics.DrawSprites += GraphicHooks.PlayerGraphics_DrawSprites;
				On.PlayerGraphics.DefaultFaceSprite_float_int += GraphicHooks.PlayerGraphics_DefaultFaceSprite;
				On.PlayerGraphics.SaintFaceCondition += GraphicHooks.PlayerGraphics_SaintFaceCondition;
				On.PlayerGraphics.TailSpeckles.DrawSprites += GraphicHooks.TailSpeckles_DrawSprites;

				On.Spear.DrawSprites += GraphicHooks.Spear_DrawSprites;
				On.Spear.Umbilical.ApplyPalette += GraphicHooks.Spear_Umbilical_ApplyPalette;
			}

			/// <summary>
			/// Handles hooks to the <see cref="Player"/> class.
			/// </summary>
			internal class MechanicHooks
			{
				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to spawn with a stomach object upon campaign start.
				/// </summary>
				internal static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
				{
					orig(self, abstractCreature, world);

					if (self.room != null && self.room.game.IsStorySession && self.room.game.GetStorySession.saveState.cycleNumber == 0 && GameFeatures.StartRoom.TryGet(self.room.game, out string[] rooms) && rooms != null && rooms.Contains(self.room.game.GetStorySession.saveState.denPosition) && Resources.spawnStomachObject.TryGet(self.room.game, out var objValues) && objValues != null)
					{
						if (objValues.Keys.Count > 0)
						{
							AbstractPhysicalObject obj = null;
							foreach (var key in objValues.Keys)
							{
								if (objValues[key] != null)
								{
									obj = Resources.ParseDictToObject(self.room, key, objValues[key]);
								}
								if (obj != null)
								{
									self.objectInStomach = obj;
									break;
								}
							}
						}
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to inflict damage to creatures with slamming.
				/// </summary>
				internal static void Player_Collide(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);

						cursor.GotoNext(MoveType.After,
							x => x.MatchCallOrCallvirt<Player>(typeof(Player).GetProperty(nameof(Player.isGourmand), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod().Name));
						cursor.MoveAfterLabels();

						cursor.Emit(OpCodes.Ldarg_0);
						cursor.EmitDelegate(Resources.ILSlam);

						// if (this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Gourmand && this.animation == Player.AnimationIndex.Roll && this.gourmandAttackNegateTime <= 0)
						cursor.MoveCursorToNextSlugcatInstance(x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Gourmand)));

						cursor.Emit(OpCodes.Ldarg_0);
						cursor.EmitDelegate(Resources.ILSlam);
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to inflict damage to creatures with slamming.
				/// </summary>
				internal static void Player_SlugSlamConditions(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);

						// if (this.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
						cursor.MoveCursorToNextSlugcatInstance(x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Gourmand)));

						cursor.Emit(OpCodes.Ldarg_0);
						static bool CanSlam(bool isNotGourm, Player player)
						{
							return isNotGourm && !player.HasFeature(Resources.canSlam);
						}
						cursor.EmitDelegate(CanSlam);
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to gain karma from holding scavenger corpses.
				/// </summary>
				internal static void Player_Update1(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);

						for (int i = 0; i < 3; i++)
						{
							// else if (ModManager.MSC && this.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer && this.slugcatStats.name == MoreSlugcatsEnums.SlugcatStatsName.Artificer && this.AI == null && this.room.game.cameras[0] != null && this.room.game.cameras[0].hud != null && this.room.game.cameras[0].hud.karmaMeter != null)
							cursor.MoveCursorToNextSlugcatInstance(x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer)));

							if (i != 0)
							{
								cursor.Emit(OpCodes.Ldarg_0);
								cursor.EmitDelegate(Resources.ILScavCorpseKarma);
							}
						}
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to make spears and craft explosives.
				/// </summary>
				internal static void Player_GrabUpdate(ILContext il)
				{
					try
					{
						ILCursor spearCursor = new(il);
						// if (ModManager.MSC && !this.input[0].pckp && this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear)
						spearCursor.MoveCursorToNextSlugcatInstance(x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear)));

						spearCursor.Emit(OpCodes.Ldarg_0);
						spearCursor.EmitDelegate(Resources.ILSpearSpecks);

						for (int i = 0; i < 4; i++)
						{
							// if (ModManager.MSC && this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear && (base.grasps[0] == null || base.grasps[1] == null) && num5 == -1 && this.input[0].y == 0)
							spearCursor.MoveCursorToNextSlugcatInstance(x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear)));

							if (i == 0)
							{
								// while (num5 < 0 && num8 < 2 && (!ModManager.MSC || this.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear))
								spearCursor.Emit(OpCodes.Ldarg_0);
								static bool FeedsFromSpears(bool isNotSpear, Player player)
								{
									return isNotSpear && !player.HasFeature(Resources.feedFromSpears);
								}
								spearCursor.EmitDelegate(FeedsFromSpears);
							}
							if (i == 1)
							{
								// else if ((num7 > -1 || this.objectInStomach != null || this.isGourmand) && (!ModManager.MSC || this.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear))
								spearCursor.Emit(OpCodes.Ldarg_0);
								static bool CantSwallowObjects(bool isNotSpear, Player player)
								{
									return isNotSpear && !player.HasFeature(Resources.cantSwallowObjects);
								}
								spearCursor.EmitDelegate(CantSwallowObjects);
							}

							if (i == 2)
							{
								spearCursor.Emit(OpCodes.Ldarg_0);
								static bool CanSpawnSpearsDualWield(bool isSpear, Player player)
								{
									return isSpear ||
										(player.HasFeature(Resources.spearSpecks) && (player.HasFeature(Resources.dualWield) || (player.FreeHand() != -1 && !player.grasps.Any(x => x != null && x.grabbed is Spear))));
								}
								spearCursor.EmitDelegate(CanSpawnSpearsDualWield);

								spearCursor.GotoNext(x => x.MatchLdarg(0),
									x => x.MatchCallOrCallvirt<Player>(typeof(Player).GetProperty(nameof(Player.input), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod().Name));

								ILCursor spearJumpCursor = spearCursor.Clone();

								spearJumpCursor.GotoNext(x => x.MatchBrtrue(out _));
								ILLabel boolJump = spearJumpCursor.Next.Operand as ILLabel;
								spearJumpCursor.Index++;

								ILLabel jumpBr = spearJumpCursor.MarkLabel();

								spearCursor.Emit(OpCodes.Br, jumpBr);
								spearJumpCursor.Emit(OpCodes.Ldarg_0);
								static bool CustomInputChecks(Player player)
								{
									return player.eatMeat <= 0 && (((player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear || player.HasFeature(Resources.cantSwallowObjects)) && player.input[0].y == 0) 
										|| (!player.HasFeature(Resources.cantSwallowObjects) && player.input[0].y == 1));
								}
								spearJumpCursor.EmitDelegate(CustomInputChecks);
								spearJumpCursor.Emit(OpCodes.Brfalse, boolJump);

								spearCursor.GotoNext(MoveType.After,
									x => x.MatchNewobj<AbstractSpear>(),
									x => x.MatchStloc(19));

								spearCursor.Emit(OpCodes.Ldarg_0);
								spearCursor.Emit(OpCodes.Ldloc, 19);
								static void GetSpearCWT(Player player, AbstractSpear self)
								{
									GraphicHooks.SpearValues cwt = GraphicHooks.spearCWT.GetOrCreateValue(self);
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
								spearCursor.EmitDelegate(GetSpearCWT);

							}

							if (i == 3)
							{
								spearCursor.Emit(OpCodes.Ldarg_0);
								static bool CantSwallow(bool isSpear, Player player)
								{
									return isSpear || player.HasFeature(Resources.cantSwallowObjects);
								}
								spearCursor.EmitDelegate(CantSwallow);
							}
						}

							ILCursor artiCursor = new(il);
						// if (ModManager.MSC && (this.FreeHand() == -1 || this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer) && this.GraspsCanBeCrafted())
						artiCursor.MoveCursorToNextSlugcatInstance(x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer)));

						artiCursor.Emit(OpCodes.Ldarg_0);
						artiCursor.EmitDelegate(Resources.ILCraftExplosives);

						// if (!ModManager.MSC || this.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Artificer || !(base.grasps[num20].grabbed is Scavenger))
						artiCursor.MoveCursorToNextSlugcatInstance(x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer)));

						artiCursor.Emit(OpCodes.Ldarg_0);
						artiCursor.EmitDelegate(Resources.ILExplosiveJump);
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to dual wield.
				/// </summary>
				internal static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
				{
					if (obj is Spear && self.HasFeature(Resources.dualWield))
					{
						return Player.ObjectGrabability.OneHand;
					}

					return orig(self, obj);
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to craft explosives.
				/// </summary>
				internal static bool Player_GraspsCanBeCrafted(On.Player.orig_GraspsCanBeCrafted orig, Player self)
				{
					return orig(self) || (self.HasFeature(Resources.explosionCraft) && self.CraftingResults() != null && (!self.HasFeature(Resources.spearSpecks) || ((self.graphicsModule as PlayerGraphics).tailSpecks != null && (self.graphicsModule as PlayerGraphics).tailSpecks.spearProg == 0f)));
				}

				/// <summary>
				/// If <see cref="SlugBaseCharacter"/> feeds from spears, ensure they cannot bite objects.
				/// </summary>
				internal static void Player_BiteEdibleObject(On.Player.orig_BiteEdibleObject orig, Player self, bool eu)
				{
					if (self.HasFeature(Resources.feedFromSpears))
						return;

					orig(self, eu);
				}

				/// <summary>
				/// If <see cref="SlugBaseCharacter"/> feeds from spears, ensure they cannot eat creatures by mouth.
				/// </summary>
				internal static bool Player_CanEatMeat(On.Player.orig_CanEatMeat orig, Player self, Creature crit)
				{
					if (self.HasFeature(Resources.feedFromSpears))
						return false;

					return orig(self, crit);
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to convert objects into explosives.
				/// </summary>
				internal static void Player_SwallowObject(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);
						// if (ModManager.MSC && this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer && this.FoodInStomach > 0)
						cursor.MoveCursorToNextSlugcatInstance(x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer)));

						cursor.Emit(OpCodes.Ldarg_0);
						cursor.EmitDelegate(Resources.ILCraftExplosives);
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to ignore checks for swallowing objects.
				/// </summary>
				internal static bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
				{
					return (!self.HasFeature(Resources.cantSwallowObjects) && orig(self, testObj)) || orig(self,testObj);
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to craft explosives.
				/// </summary>
				internal static void Player_SpitUpCraftedObject(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);
						// if (this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
						cursor.MoveCursorToNextSlugcatInstance(x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer)));

						cursor.Emit(OpCodes.Ldarg_0);
						cursor.EmitDelegate(Resources.ILCraftExplosives);
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to craft explosives.
				/// </summary>
				internal static void Player_CraftingResults(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);
						// if (this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
						cursor.MoveCursorToNextSlugcatInstance(x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer)));

						cursor.Emit(OpCodes.Ldarg_0);
						cursor.EmitDelegate(Resources.ILCraftExplosives);
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to use Spearmaster mechanics.
				/// </summary>
				internal static void Player_ClassMechanicsSpearmaster(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);
						// if ((base.stun >= 1 || base.dead) && this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear)
						cursor.MoveCursorToNextSlugcatInstance(x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear)));

						cursor.Emit(OpCodes.Ldarg_0);
						cursor.EmitDelegate(Resources.ILSpearSpecks);
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to use Artificer mechanics.
				/// </summary>
				internal static void Player_ClassMechanicsArtificer(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);
						// if (this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer || (ExpeditionGame.explosivejump && !this.isSlugpup))
						cursor.MoveCursorToNextSlugcatInstance(x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer)));

						cursor.Emit(OpCodes.Ldarg_0);
						cursor.EmitDelegate(Resources.ILExplosiveJump);
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Returns true if <see cref="SlugBaseCharacter"/> feeds from spears.
				/// </summary>
				internal static bool Spear_Spear_NeedleCanFeed(On.Spear.orig_Spear_NeedleCanFeed orig, Spear self)
				{
					if (ModManager.MSC && self.thrownBy != null && self.thrownBy is Player player && player.HasFeature(Resources.feedFromSpears))
					{
						return self.spearmasterNeedle && self.spearmasterNeedle_hasConnection;
					}
					return orig(self);
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to feed from Spearmaster needles dynamically.
				/// </summary>
				internal static void Spear_HitSomethingWithoutStopping(On.Spear.orig_HitSomethingWithoutStopping orig, Spear self, PhysicalObject obj, BodyChunk chunk, PhysicalObject.Appendage appendage)
				{
					if (self.Spear_NeedleCanFeed() && self.thrownBy is Player player && PlayerFeatures.Diet.TryGet(player, out var diet))
					{
						if (obj.abstractPhysicalObject.rippleLayer != self.abstractPhysicalObject.rippleLayer && !obj.abstractPhysicalObject.rippleBothSides && !self.abstractPhysicalObject.rippleBothSides)
							return;

						if (self.room.game.IsStorySession && self.room.game.GetStorySession.playerSessionRecords != null)
							self.room.game.GetStorySession.playerSessionRecords[(player.abstractCreature.state as PlayerState).playerNumber].AddEat(obj);

						if (obj is Creature creature && !creature.dead)
						{
							Resources.HandleFood(player, diet.GetMeatMultiplier(player, creature));
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
								Resources.HandleFood(player, diet.GetFoodMultiplier(swarmer));
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
						else if (obj is IPlayerEdible edible && edible.Edible)
						{
							Resources.HandleFood(player, diet.GetFoodMultiplier(obj));
							obj.Destroy();
						}
						
						return;
					}

					orig(self, obj, chunk, appendage);
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to feed from Spearmaster needles dynamically.
				/// </summary>
				internal static void Spear_HitSomething(ILContext il)
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
										fed = Resources.HandleFood(player, multiplier);
										creature.State.meatLeft -= creature.Template.meatPoints > 0 ? 1 : 0;
									}
									if (creature.dead && diet.Corpses > 0f && creature.State.meatLeft > 0)
									{
										fed = Resources.HandleFood(player, diet.GetMeatMultiplier(player, creature));
										creature.State.meatLeft--;
									}
								}
								if (result.obj is IPlayerEdible edible && edible.Edible && result.obj is not GooieDuck && diet.GetFoodMultiplier(result.obj) > 0f)
								{
									fed = Resources.HandleFood(player, diet.GetFoodMultiplier(result.obj));
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
									fed = Resources.HandleFood(player, diet.GetFoodMultiplier(popCorn));
								}
								if (result.obj is JellyFish jelly)
								{
									if (jelly.dead && diet.Corpses > 0f)
									{
										fed = Resources.HandleFood(player, diet.GetFoodMultiplier(jelly));
										jelly.Destroy();
									}
									else if (!jelly.dead)
									{
										fed = Resources.HandleFood(player, diet.GetFoodMultiplier(jelly));
										jelly.dead = true;
									}
								}
                                if (result.obj is Pomegranate pomegranate && pomegranate.smashed)
                                {
									fed = Resources.HandleFood(player, diet.GetFoodMultiplier(pomegranate));
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

						previousJumpCursor.GotoPrev(x => x.MatchLdarg(0));
						ILLabel changeJump = previousJumpCursor.MarkLabel();

						previousJumpCursor.GotoPrev(MoveType.After,
							x => x.MatchBr(out _),
							x => x.MatchLdarg(1),
							x => x.MatchLdfld<SharedPhysics.CollisionResult>(nameof(SharedPhysics.CollisionResult.onAppendagePos)));
						previousJumpCursor.Next.Operand = changeJump;

						previousJumpCursor.GotoPrev(x => x.MatchBr(out _));
						previousJumpCursor.Next.Operand = changeJump;

						previousJumpCursor.GotoPrev(MoveType.After,
							x => x.MatchBr(out _),
							x => x.MatchLdarg(1),
							x => x.MatchLdfld<SharedPhysics.CollisionResult>(nameof(SharedPhysics.CollisionResult.chunk)));
						previousJumpCursor.Next.Operand = changeJump;

						previousJumpCursor.GotoPrev(x => x.MatchBr(out _));
						previousJumpCursor.Next.Operand = changeJump;

						previousJumpCursor.GotoPrev(x => x.MatchBltUn(out _));
						previousJumpCursor.Next.Operand = changeJump;

						previousJumpCursor.GotoPrev(MoveType.After,
							x => x.MatchLdarg(1),
							x => x.MatchLdfld<SharedPhysics.CollisionResult>(nameof(SharedPhysics.CollisionResult.obj)),
							x => x.MatchIsinst<Player>());
						previousJumpCursor.Next.Operand = changeJump;

						previousJumpCursor.GotoPrev(x => x.MatchBrfalse(out _));
						previousJumpCursor.Next.Operand = changeJump;

						previousJumpCursor.GotoPrev(x => x.MatchBrfalse(out _));
						previousJumpCursor.Next.Operand = changeJump;

						previousJumpCursor.GotoPrev(MoveType.After,
							x => x.MatchStloc(1),
							x => x.MatchLdarg(1),
							x => x.MatchLdfld<SharedPhysics.CollisionResult>(nameof(SharedPhysics.CollisionResult.obj)),
							x => x.MatchIsinst<Creature>());

						previousJumpCursor.Next.Operand = changeJump;

						UnityEngine.Debug.Log(il.ToString());
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Disallows <see cref="SlugBaseCharacter"/> to pop <see cref="SeedCob"/> objects if they feed from needles.
				/// </summary>
				internal static void SeedCob_HitByWeapon(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);

						cursor.MoveCursorToNextSlugcatInstance(x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear)));

						cursor.Emit(OpCodes.Ldarg_1);
						static bool FeedsOffSpears(bool isSpear, Weapon spear)
						{
							return isSpear || (spear.thrownBy is Player player && player.HasFeature(Resources.feedFromSpears));
						}
						cursor.EmitDelegate(FeedsOffSpears);
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}

				/// <summary>
				/// Disallows <see cref="SlugBaseCharacter"/> to pop <see cref="SeedCob"/> objects if they feed from needles.
				/// </summary>
				internal static void SeedCob_Update(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);

						cursor.MoveCursorToNextSlugcatInstance(x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear)));

						cursor.Emit(OpCodes.Ldloc, 12);
						static bool FeedsOffSpears(bool isNotSpear, Player player)
						{
							return isNotSpear && !player.HasFeature(Resources.feedFromSpears);
						}
						cursor.EmitDelegate(FeedsOffSpears);
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
					}
				}
			}

			/// <summary>
			/// Handles hooks to the <see cref="PlayerGraphics"/> class.
			/// </summary>
			public class GraphicHooks
			{
				private static Color lastBlackColor;

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to have gills, a fade in mark, and tail specks.
				/// </summary>
				internal static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
				{
					orig(self, ow);

					int startSprite = 12;
					if (ModManager.MSC && self.player.HasFeature(Resources.rivuletGills))
					{
						self.gills = new(self, startSprite);
						startSprite += self.gills.numberOfSprites;
					}

					if (ModManager.MSC && self.player.HasFeature(Resources.spearSpecks))
					{
						self.tailSpecks = new PlayerGraphics.TailSpeckles(self, startSprite);
						startSprite += self.tailSpecks.numberOfSprites;
					}

					if (self.player.abstractCreature.world.game.IsStorySession && GameFeatures.TheMark.TryGet(self.player.abstractCreature.world.game, out bool hasMark) && hasMark && self.player.HasFeature(Resources.revealMark))
					{
						self.markBaseAlpha = Mathf.Pow(Mathf.InverseLerp(4f, 14f, (float)self.player.abstractCreature.world.game.GetStorySession.saveState.cycleNumber), 3.5f);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to have gills.
				/// </summary>
				internal static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
				{
					orig(self);

					if (ModManager.MSC && self.player.HasFeature(Resources.rivuletGills))
					{
						self.gills.Update();
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to have gills, tail specks, and a fluffy head.
				/// </summary>
				internal static void IL_PlayerGraphics_InitiateSprites(ILContext il)
				{
					try
					{
						ILCursor cursor = new(il);
						// gown.InitiateSprite(this.gownIndex, sLeaser, rCam);
						cursor.GotoNext(MoveType.After, x => x.MatchCall<PlayerGraphics.Gown>(nameof(PlayerGraphics.Gown.InitiateSprite)));

						cursor.Emit(OpCodes.Ldarg_0);
						cursor.Emit(OpCodes.Ldarg_1);
						cursor.Emit(OpCodes.Ldarg_2);
						cursor.EmitDelegate((PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) =>
						{
							if (ModManager.MSC && self.player.HasFeature(Resources.saintFluff))
							{
								sLeaser.sprites[3].SetElementByName("HeadB0");
							}

							if (ModManager.MSC && self.player.HasFeature(Resources.rivuletGills))
							{
								self.gills.startSprite = sLeaser.sprites.Length;
								Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + self.gills.numberOfSprites);

								self.gills.InitiateSprites(sLeaser, rCam);
							}

							if (ModManager.MSC && self.player.HasFeature(Resources.spearSpecks))
							{
								self.tailSpecks.startSprite = sLeaser.sprites.Length;
								Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + self.tailSpecks.numberOfSprites);

								self.tailSpecks.InitiateSprites(sLeaser, rCam);
							}
						});
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogException(ex);
						throw;
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to have gills and tail specks.
				/// </summary>
				internal static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
				{
					orig(self, sLeaser, rCam, newContatiner);

					if (ModManager.MSC && self.player.HasFeature(Resources.rivuletGills) && sLeaser.sprites.Length > self.gills.startSprite)
					{
						self.gills.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Midground"));
					}

					if (ModManager.MSC && self.player.HasFeature(Resources.spearSpecks) && sLeaser.sprites.Length > self.tailSpecks.startSprite)
					{
						self.tailSpecks.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Midground"));
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to color gills. Also automatically replaces pure black (transparent) with the room palette's black color.
				/// </summary>
				internal static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
				{
					if (SlugBaseCharacter.TryGet(self.player.SlugCatClass, out var character2) && PlayerFeatures.CustomColors.TryGet(character2, out var slots2))
					{
						float lerp = Resources.watcherBlackAmount.TryGet(self.player, out float amount) ? amount : 1f;
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


					if (ModManager.MSC && self.player.HasFeature(Resources.rivuletGills, out var character))
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
				/// Allows <see cref="SlugBaseCharacter"/> to have gills and tail specks colors on custom color menu.
				/// </summary>
				internal static void PlayerGraphics_DefaultBodyPartColorHex(ILContext il)
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
							if (name.HasFeature(Resources.rivuletGills, out var character) && PlayerFeatures.CustomColors.TryGet(character, out var slots) && slots.Any(x => x.Name == "Gills"))
							{
								colors.Add(Custom.colorToHex(slots.Where(x => x.Name == "Gills").FirstOrDefault().Default));
							}

							if (name.HasFeature(Resources.spearSpecks, out var character2) && PlayerFeatures.CustomColors.TryGet(character2, out var slots2) && slots2.Any(x => x.Name == "Spears"))
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
				internal static void PlayerGraphics_ColoredBodyPartList(ILContext il)
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
							if (name.HasFeature(Resources.rivuletGills, out _))
							{
								parts.Add("Gills");
							}

							if (name.HasFeature(Resources.spearSpecks, out _))
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
				/// Allows <see cref="SlugBaseCharacter"/> to have gills, tail specks, and a fluffy head.
				/// </summary>
				internal static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
				{
					orig(self, sLeaser, rCam, timeStacker, camPos);

					if (ModManager.MSC && self.player.HasFeature(Resources.saintFluff) && !sLeaser.sprites[3].element.name.Contains("HeadB"))
					{
						sLeaser.sprites[3].SetElementByName($"HeadB{sLeaser.sprites[3].element.name.Substring("HeadA".Length)}");
					}

					if (ModManager.MSC && self.player.HasFeature(Resources.rivuletGills))
					{
						self.gills.DrawSprites(sLeaser, rCam, timeStacker, camPos);
					}

					if (ModManager.MSC && self.player.HasFeature(Resources.spearSpecks))
					{
						self.tailSpecks.DrawSprites(sLeaser, rCam, timeStacker, camPos);
					}
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to use Artificer's face.
				/// </summary>
				internal static string PlayerGraphics_DefaultFaceSprite(On.PlayerGraphics.orig_DefaultFaceSprite_float_int orig, PlayerGraphics self, float eyeScale, int imgIndex)
				{
					bool artiEyes = self.player.HasFeature(Resources.artiEyes);
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

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to have Saint's closed eyes.
				/// </summary>
				internal static bool PlayerGraphics_SaintFaceCondition(On.PlayerGraphics.orig_SaintFaceCondition orig, PlayerGraphics self)
				{
					return orig(self) || self.player.HasFeature(Resources.saintEyes);
				}

				/// <summary>
				/// Allows <see cref="SlugBaseCharacter"/> to color tail specks.
				/// </summary>
				internal static void TailSpeckles_DrawSprites(On.PlayerGraphics.TailSpeckles.orig_DrawSprites orig, PlayerGraphics.TailSpeckles self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
				{
					orig(self, sLeaser, rCam, timeStacker, camPos);

					if (self.pGraphics.player.HasFeature(Resources.spearSpecks, out var character) && character != null)
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
				internal static void Spear_DrawSprites(On.Spear.orig_DrawSprites orig, Spear self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
				{
					orig(self, sLeaser, rCam, timeStacker, camPos);


					if (spearCWT.TryGetValue(self.abstractSpear, out var cwt) && cwt.slugColor != null)
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
				internal static void Spear_Umbilical_ApplyPalette(On.Spear.Umbilical.orig_ApplyPalette orig, Spear.Umbilical self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
				{
					orig(self, sLeaser, rCam, palette);

					if (self.spider.HasFeature(Resources.feedFromSpears) && spearCWT.TryGetValue(self.maggot.abstractSpear, out var col) && col.slugColor != null)
					{
						self.threadCol = col.slugColor.Value;
						self.fogColor = Color.Lerp(palette.fogColor, col.slugColor.Value, 0.8f);
					}
				}

				public static ConditionalWeakTable<AbstractSpear, SpearValues> spearCWT = new();

				public class SpearValues
				{
					public Color? slugColor;
				}
			}
		}
	}
}