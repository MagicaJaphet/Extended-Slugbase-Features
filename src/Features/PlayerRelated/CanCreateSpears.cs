using ExtendedSlugbase.Extensions;
using MagicaHookingLibrary.Helpers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using SlugBase;
using SlugBase.DataTypes;
using SlugBase.Features;
using System;
using System.Linq;
using UnityEngine;
using static ExtendedSlugbase.DataTypes.AbstractSpawners;

namespace ExtendedSlugbase.Features.PlayerRelated;
public class CanCreateSpears() : PlayerFeature<CanCreateSpears.SpearCreating>("spear_specks", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static SpearCreating Factory(JsonAny json) => new(json);

	/// <summary>
	/// JSON Object to hold logic for spear creation.
	/// </summary>
	public class SpearCreating
	{
		public IntVector2 Rows { get; } = new(5, 3);
		public bool FeedFromSpears { get; } = true;
		public float GenerationSpeed { get; } = 0.05f;
		public SoundID PullSound { get; } = MoreSlugcatsEnums.MSCSoundID.SM_Spear_Pull;
		public SoundID SnapSound { get; } = MoreSlugcatsEnums.MSCSoundID.SM_Spear_Grab;
		public AbstractObject SpearObj { get; } // Null is fine
		public bool ReactsToSpears { get; } = true;
		public bool Dripping { get; } = true;
		public int FoodCost { get; }

		internal SpearCreating(JsonAny json)
		{
			//lATER: God can't help me now x2
			/*	SPEAR CREATING:
				SPRITE ELEMENT: what sprite are we using for the specks?
				POSITIONING: maybe how do the specks lay on the tail? (the most complex part tbh)
			*/

			if (json.TryParse(out JsonObject obj))
			{
				if (obj.TryGet("speckle_amounts", out IntVector2 rows))
				{
					Rows = rows;
				}
				if (obj.TryGet("feeds_from_spears", out bool feedFromSpears))
				{
					FeedFromSpears = feedFromSpears;
				}
				if (obj.TryGet("generation_speed", out float speed))
				{
					GenerationSpeed = speed;
				}
				if (obj.TryGet("food_cost", out int cost))
				{
					FoodCost = cost;
				}
				if (obj.TryGet("head_shaking", out bool squint))
				{
					ReactsToSpears = squint;
				}
				if (obj.TryGet("pull_sound_id", out SoundID pullID))
				{
					PullSound = pullID;
				}
				if (obj.TryGet("snap_sound_id", out SoundID snapID))
				{
					SnapSound = snapID;
				}
				if (obj.TryGet("dripping", out bool drip))
				{
					Dripping = drip;
				}
				if (obj.TryGet("spear_template", out JsonAny abstractObj))
				{
					SpearObj = new AbstractObject(abstractObj);
				}
			}
		}
	}

	internal static class Implementation
	{
		internal static SpearCreating SpearCreationFeature(Player self) => ExtPlayerFeatures.CanCreateSpears.Get(self);
		private static SpearCreating SpearCreationFeature_Spear(Spear self) => ExtPlayerFeatures.CanCreateSpears.Get(self.thrownBy as Player);

		private static float GenerationSpeed(float speed, SpearCreating specks)
		{
			return specks?.GenerationSpeed ?? speed;
		}
		internal static bool Player_BiteEdibleObject(Player self) => self.TryGetFeature(ExtPlayerFeatures.CanCreateSpears, out var specks) && specks.FeedFromSpears;

		internal static bool Player_CanEatMeat(Player self) => self.TryGetFeature(ExtPlayerFeatures.CanCreateSpears, out var specks) && specks.FeedFromSpears;

		internal static void Player_ClassMechanicsSpearmaster(ILContext il)
		{
			ILCursor c = new(il);

			var specksFeature = ExtPlayerFeatures.CanCreateSpears.ImplementFeatureVariable<SpearCreating, Player>(il, c);

			static bool GeneratesSpears(bool isSpear, SpearCreating specks)
			{
				return isSpear || specks != null;
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear).GetSlugcatFieldInfo()
				);
			c.EmitFeatureDelegate(specksFeature, GeneratesSpears);

			static float GenerationSpeed(float speed, SpearCreating specks)
			{
				return specks?.GenerationSpeed ?? speed;
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchLdcR4(0.05f)
				);
			c.EmitFeatureDelegate(specksFeature, GenerationSpeed);
		}

		internal static void Player_GrabUpdate_1(ILCursor c, VariableDefinition specksFeature)
		{
			static bool GeneratesSpears(bool isSpear, SpearCreating specks)
			{
				return isSpear || specks != null;
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear).GetSlugcatFieldInfo()
				); // if (ModManager.MSC && !this.input[0].pckp && this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear && self.graphicsModule != null)
			c.EmitFeatureDelegate(specksFeature, GeneratesSpears);



			c.GotoNext(
				MoveType.After,
				x => x.MatchLdcR4(0.05f)
				);
			c.EmitFeatureDelegate(specksFeature, GenerationSpeed);


			static bool FeedsFromSpears(bool isNotSpear, SpearCreating specks)
			{
				return isNotSpear && !(specks?.FeedFromSpears ?? false);
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear).GetSlugcatFieldInfo()
				);
			c.EmitFeatureDelegate(specksFeature, FeedsFromSpears);
		}

		internal static void Player_GrabUpdate_2(ILCursor c, VariableDefinition specksFeature)
		{
			static bool CanMakeSpear(bool isSpear, SpearCreating specks, Player self)
			{
				return isSpear || specks != null && self.FoodInStomach >= specks.FoodCost;
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear).GetSlugcatFieldInfo()
				);
			c.EmitFeatureDelegate(specksFeature, CanMakeSpear, true);
		}

		internal static void Player_GrabUpdate_3(ILCursor c, VariableDefinition specksFeature)
		{
			static SoundID PullSoundID(SoundID spearPull, SpearCreating specks)
			{
				return specks?.PullSound ?? spearPull;
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.MSCSoundID).GetField(nameof(MoreSlugcatsEnums.MSCSoundID.SM_Spear_Pull)))
				); // this.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.SM_Spear_Pull, 0f, 1f, 1f + UnityEngine.Random.value * 0.5f);
			c.EmitFeatureDelegate(specksFeature, PullSoundID);

			c.GotoNext(
				MoveType.After,
				x => x.MatchLdcR4(0.05f)
				);
			c.EmitFeatureDelegate(specksFeature, GenerationSpeed);

			// Prevent head shaking if no reaction
			static bool NoHeadShake(SpearCreating specks)
			{
				return !(specks?.ReactsToSpears ?? false);
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchLdloc(16),
				x => x.MatchLdfld(out _),
				x => x.MatchLdcR4(0.6f)
				); // if (tailSpecks2.spearProg > 0.6f)
			ILLabel jumpLabel = (ILLabel)c.Next.Operand;
			c.GotoPrev(
				MoveType.AfterLabel,
				x => x.MatchLdloc(16)
				);
			c.EmitFeatureDelegate(specksFeature, NoHeadShake);
			c.Emit(OpCodes.Brtrue, jumpLabel);

			// Skip past all this nonsense so we have full control over spear spawning lmao
			static bool CraftsSpears(SpearCreating self)
			{
				return self != null;
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchBneUn(out _)
				); // if (tailSpecks2.spearProg == 1f)
			ILCursor jump = c.CloneAndGoToNext(
				MoveType.After,
				x => x.MatchStfld(typeof(Player).GetField(nameof(Player.wantToThrow), ReflectionHelpers.anyFlag))
				);
			c.EmitFeatureDelegate(specksFeature, CraftsSpears);
			c.Emit(OpCodes.Brtrue, jump.MarkLabel());

			// Largely taken from self game code, just made to cater to our needs
			static void CustomSpearCrafting(SpearCreating spearCreatability, Player self, PlayerGraphics.TailSpeckles tailSpecks)
			{
				if (spearCreatability != null)
				{
					self.room.PlaySound(spearCreatability.SnapSound, 0f, 1f, 0.5f + UnityEngine.Random.value * 1.5f);
					self.smSpearSoundReady = false;

					var pGraphics = self.graphicsModule as PlayerGraphics;
					Vector2 pos = pGraphics.tail[(int)(pGraphics.tail.Length / 2f)].pos;

					if (spearCreatability.Dripping)
					{
						for (int j = 0; j < 4; j++)
						{
							Vector2 a = Custom.DirVec(pos, self.bodyChunks[1].pos);
							self.room.AddObject(new WaterDrip(pos + Custom.RNV() * (UnityEngine.Random.value * 1.5f), Custom.RNV() * (3f * UnityEngine.Random.value) + a * Mathf.Lerp(2f, 6f, UnityEngine.Random.value), false));
						}
					}
					for (int k = 0; k < 5; k++)
					{
						Vector2 randomDir = Custom.RNV();
						self.room.AddObject(new Spark(pos + randomDir * (UnityEngine.Random.value * 40f), randomDir * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white, null, 4, 18));
					}
					tailSpecks.setSpearProgress(0f);

					AbstractPhysicalObject abstractSpear = null;
					if (spearCreatability.SpearObj != null && spearCreatability.SpearObj.TryGetObject(self.room.abstractRoom, self.room.GetWorldCoordinate(self.mainBodyChunk.pos), out var spearObj))
					{
						abstractSpear = spearObj;
					}

					// Our default object
					abstractSpear ??= new AbstractSpear(self.room.world, null, self.room.GetWorldCoordinate(self.mainBodyChunk.pos), self.room.game.GetNewID(), false);

					self.room.abstractRoom.AddEntity(abstractSpear);
					abstractSpear.pos = self.abstractCreature.pos;
					abstractSpear.RealizeInRoom();

					self.SubtractFood(spearCreatability.FoodCost);

					Vector2 vector = self.bodyChunks[0].pos;
					Vector2 a3 = Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos);
					if (Mathf.Abs(self.bodyChunks[0].pos.y - self.bodyChunks[1].pos.y) > Mathf.Abs(self.bodyChunks[0].pos.x - self.bodyChunks[1].pos.x) && self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y)
					{
						vector += Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos) * 5f;
						a3 *= -1f;
						a3.x += 0.4f * self.flipDirection;
						a3.Normalize();
					}
					abstractSpear.realizedObject.firstChunk.HardSetPosition(vector);
					abstractSpear.realizedObject.firstChunk.vel = Vector2.ClampMagnitude((a3 * 2f + Custom.RNV() * UnityEngine.Random.value) / abstractSpear.realizedObject.firstChunk.mass, 6f);
					if (self.FreeHand() != -1 && self.CanIPickThisUp(abstractSpear.realizedObject))
					{
						self.SlugcatGrab(abstractSpear.realizedObject, self.FreeHand());
					}
					if (abstractSpear.type == AbstractPhysicalObject.AbstractObjectType.Spear)
					{
						var spear = abstractSpear.realizedObject as Spear;
						var spearCWT = CWTs.SpearCWT.GetData(abstractSpear as AbstractSpear);
						spearCWT.playerNumber = self.ArenaIndex();

						if (spearCreatability.SpearObj == null)
							spear.Spear_makeNeedle(tailSpecks.spearType, spear.grabbedBy != null);
						else if (spearCreatability.FeedFromSpears)
						{
							spear.spearmasterNeedle_hasConnection = spear.grabbedBy != null;
						}

						if (self.TryGetColorSlots(out var slots))
						{
							if (slots.FirstOrDefault(x => x.Name == "Spears") is ColorSlot spearSlot)
							{
								spearCWT.generatedSpearColor = spearSlot;
							}
							if (slots.FirstOrDefault(x => x.Name == "SpearsFade") is ColorSlot spearFadeSlot)
							{
								spearCWT.generatedSpearFadeColor = spearFadeSlot;
							}
						}
					}
					self.wantToThrow = 0;
				}
			}

			jump.Emit(OpCodes.Ldloc, specksFeature);
			jump.Emit(OpCodes.Ldarg_0);
			jump.Emit(OpCodes.Ldloc, 16);
			jump.EmitDelegate(CustomSpearCrafting);
		}

		internal static void PlayerGraphics_AddToContainer(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer midGround)
		{
			if (self.player.TryGetFeature(ExtPlayerFeatures.CanCreateSpears, out _))
			{
				self.tailSpecks.AddToContainer(sLeaser, rCam, midGround);
			}
		}

		internal static void PlayerGraphics_Ctor(PlayerGraphics self, ref int startSprite)
		{
			if (self.player.TryGetFeature(ExtPlayerFeatures.CanCreateSpears, out _))
			{
				self.tailSpecks = new(self, startSprite);
				startSprite += self.tailSpecks.numberOfSprites;
			}
		}

		internal static void PlayerGraphics_InitiateSprites(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			if (self.player.TryGetFeature(ExtPlayerFeatures.CanCreateSpears, out _))
			{
				self.tailSpecks.startSprite = sLeaser.sprites.Length;
				Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + self.tailSpecks.numberOfSprites);

				self.tailSpecks.InitiateSprites(sLeaser, rCam);
			}
		}

		internal static void PlayerGraphics_MSCUpdate(PlayerGraphics self)
		{
			if (self.player.TryGetFeature(ExtPlayerFeatures.CanCreateSpears, out var specks) && self.tailSpecks.spearProg > 0.1f && !specks.ReactsToSpears)
			{
				self.blink = 0;
			}
		}

		internal static void PlayerGraphics_TailSpeckles_ctor(ILContext il)
		{
			ILCursor c = new(il);

			static void HandleRowsAndColumns(PlayerGraphics.TailSpeckles self)
			{
				if (self.pGraphics.player.TryGetFeature(ExtPlayerFeatures.CanCreateSpears, out var specks))
				{
					self.rows = specks.Rows.x;
					self.lines = specks.Rows.y;
				}
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchStfld<PlayerGraphics.TailSpeckles>(nameof(PlayerGraphics.TailSpeckles.lines)),
				x => x.MatchLdarg(0)
				);
			c.EmitLdarg0Delegate(HandleRowsAndColumns);
		}

		internal static void Pomegranate_Update(ILContext il)
		{
			ILCursor c = new(il);

			static bool FeedsOnPopcorn(bool isSpear, Player player)
			{
				return isSpear && !(player.TryGetFeature(ExtPlayerFeatures.CanCreateSpears, out var specks) && specks.FeedFromSpears);
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear).GetSlugcatFieldInfo()
				);
			c.Emit(OpCodes.Ldloc, 10);
			c.EmitDelegate(FeedsOnPopcorn);
		}

		internal static void SeedCob_HitByWeapon(ILContext il)
		{
			ILCursor c = new(il);

			static bool FeedsOnPopcorn(bool isSpear, SeedCob self, Weapon weapon)
			{
				return isSpear || weapon.thrownBy is Player player
					&& player.TryGetFeature(ExtPlayerFeatures.CanCreateSpears, out var specks)
					&& PlayerFeatures.Diet.TryGet(player, out var diet)
					&& diet.GetFoodMultiplier(self) > 0f
					&& specks.FeedFromSpears;
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear).GetSlugcatFieldInfo()
				);
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate(FeedsOnPopcorn);
		}

		internal static void SeedCob_Update(ILContext il)
		{
			ILCursor c = new(il);

			static bool FeedsOnPopcorn(bool isSpear, Player player)
			{
				return isSpear && !(player.TryGetFeature(ExtPlayerFeatures.CanCreateSpears, out var specks) && specks.FeedFromSpears);
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear).GetSlugcatFieldInfo()
				);
			c.Emit(OpCodes.Ldloc, 13);
			c.EmitDelegate(FeedsOnPopcorn);
		}

		internal static void SharedPhysics_TraceProjectileAgainstBodyChunks(ILContext il)
		{
			ILCursor c = new(il);

			static bool CanHitEdibleObject(bool canBeHitByWeapons, SharedPhysics.IProjectileTracer projTracer, PhysicalObject physicalObject, PhysicalObject exemptObject)
			{
				return canBeHitByWeapons ||
					projTracer is Spear spear && exemptObject is Player player
					&& physicalObject != null
					&& physicalObject is not SeedCob
					&& physicalObject is not Pomegranate
					&& spear.Spear_NeedleCanFeed()
					&& PlayerFeatures.Diet.TryGet(player, out var diet) && diet.GetFoodMultiplier(physicalObject) > 0f;
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

		internal static void Spear_HitSomething(ILContext il)
		{
			ILCursor c = new(il);

			var spearFeature = ExtPlayerFeatures.CanCreateSpears.ImplementFeatureVariable<SpearCreating, Spear>(il, c, TypeConverters.GetPlayer);

			static bool CanEatEggBugs(bool canFeedFromSpears, SpearCreating specks, Spear self)
			{
				return canFeedFromSpears || // Return default logic
					specks != null
					&& self.thrownBy is Player player
					&& self.Spear_NeedleCanFeed() && player.FoodInStomach < player.MaxFoodInStomach
					&& (!PlayerFeatures.Diet.TryGet(player, out var diet)
					|| diet.Meat > 0f
					&& (!diet.CreatureOverrides.TryGetValue(CreatureTemplate.Type.EggBug, out var value) || value > 0f)
					&& (!diet.CreatureOverrides.TryGetValue(MoreSlugcatsEnums.CreatureTemplateType.FireBug, out var value2) || value2 > 0f));
			}

			static bool CanCreateSpears(SpearCreating specks)
			{
				return specks != null;
			}

			static bool SpearHitSomethingFeed(Spear self, SharedPhysics.CollisionResult result, bool eu, SpearCreating specks)
			{
				if (self.thrownBy is Player player && SlugBaseCharacter.TryGet(player.SlugCatClass, out _))
				{
					var stickObject = false;
					var defaultFood = 0f;
					Diet diet = null;
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

		internal static bool Spear_Spear_NeedleCanFeed(Spear self) => self.thrownBy is Player player && player.TryGetFeature(ExtPlayerFeatures.CanCreateSpears, out var specks) && specks.FeedFromSpears && self.spearmasterNeedle_hasConnection;
	}
}
