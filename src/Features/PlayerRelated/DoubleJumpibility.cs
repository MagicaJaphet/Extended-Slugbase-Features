using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using MagicaHookingLibrary.Helpers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static ExtendedSlugbase.Features.PlayerRelated.CanCraftObjects;

namespace ExtendedSlugbase.Features.PlayerRelated;
public class DoubleJump() : PlayerFeature<DoubleJump.DoubleJumpibility>("explosive_jump", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static DoubleJumpibility Factory(JsonAny json) => new(json);

	/// <summary>
	/// JSON Object to hold the logic for double jumping.
	/// </summary>
	public class DoubleJumpibility
	{
		public int[] JumpLimit { get; } = [7, 10];
		public SoundID JumpSoundID { get; } = SoundID.Fire_Spear_Explode;
		public LimitResult LimitReachedResult { get; } = LimitResult.Die;
		public bool Parry { get; } = true;
		public int FoodCost { get; } = 0;
		public float[] JumpBoost { get; } = [8f];
		public int[] StunTimers { get; } = [60];
		public bool JumpEffect { get; }

		public enum LimitResult
		{
			LongStun,
			Die, // Like artificer
			ConsumeFood // Like the Wanderer
		}

		internal DoubleJumpibility(JsonAny json)
		{
			/*	EXPLOSIVE JUMPS
				EFFECT: the type of effect that plays on the jump
				LOOPING EFFECT: The visual that continuously plays when the soft limit is reached
				LIMIT EFFECT: the visual effect that plays when the slugcat is exhausted
				PARRY EFFECT: The visual that plays when parrying
			*/

			//LATER: Implement some sort of abstract helper for spawning custom effects for all use cases

			if (json.TryParse(out JsonObject obj))
			{
				if (obj.TryGet("jump_speed", out float[] jumpBoost))
				{
					JumpBoost = jumpBoost;
				}
				if (obj.TryGet("jump_sound_id", out SoundID soundID))
				{
					JumpSoundID = soundID;
				}
				if (obj.TryGet("limits", out int[] limits, 1, 2))
				{
					JumpLimit = limits;
				}
				if (obj.TryGet("limit_reached_result", out LimitResult effect))
				{
					LimitReachedResult = effect;
				}
				if (obj.TryGet("stun_timers", out int[] stunTimers, 1, 2))
				{
					StunTimers = stunTimers;
				}
				if (obj.TryGet("food_cost", out int cost))
				{
					FoodCost = cost;
				}
				if (obj.TryGet("parry", out bool parry))
				{
					Parry = parry;
				}
				if (obj.TryGet("jump_effect", out bool jumpEffect))
				{
					//LATER: Replace
					JumpEffect = jumpEffect;
				}
			}
		}

	}

	internal static class Implementation
	{
		internal static void Player_ClassMechanicsArtificer(ILContext il)
		{
			ILCursor c = new(il);

			var doubleJump = ExtPlayerFeatures.DoubleJump.ImplementFeatureVariable<DoubleJumpibility, Player>(il, c);

			// Allows double jumping
			static bool ExplosiveJump(bool isArtificer, DoubleJump self)
			{
				return isArtificer || self != null;
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer).GetSlugcatFieldInfo()
				);
			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer).GetSlugcatFieldInfo()
				);
			c.EmitFeatureDelegate(doubleJump, ExplosiveJump);

			// Controls when the visuals start playing for the exhaustion
			static int JumpSoftLimit(int artiJumps, DoubleJumpibility doubleJump)
			{
				if (doubleJump != null)
				{
					return Math.Max(1, doubleJump.JumpLimit[0] - (doubleJump.JumpLimit.Length > 1 ? 2 : 5));
				}
				return artiJumps;
			}

			c.GotoNext(
				x => x.MatchStloc(2)
				);
			c.EmitFeatureDelegate(doubleJump, JumpSoftLimit);

			// Control what effects happen
			//LATER: Replace with several functions related to the type of effect being called
			static bool CustomJumpEffect(DoubleJumpibility doubleJump)
			{
				if (doubleJump != null)
				{
					return doubleJump.JumpEffect; //LATER: Replace with bool when properly implemented
				}
				return false;
			}

			c.GotoNext(
				x => x.MatchCallOrCallvirt(typeof(UnityEngine.Random).GetProperty(nameof(UnityEngine.Random.value)).GetGetMethod())
			);
			ILCursor jump = c.CloneAndGoToNext(
				x => x.MatchLdloc(0)
				);
			c.EmitFeatureDelegate(doubleJump, CustomJumpEffect);
			c.Emit(OpCodes.Brtrue, jump.MarkLabel());

			c.GotoNext(
				x => x.MatchLdloc(5)
				);
			jump = c.CloneAndGoToNext(
				x => x.MatchLdarg(0),
				x => x.MatchLdfld(out _),
				x => x.MatchLdsfld(typeof(SoundID).GetField(nameof(SoundID.Fire_Spear_Explode)))
				);
			c.EmitFeatureDelegate(doubleJump, CustomJumpEffect);
			c.Emit(OpCodes.Brtrue, jump.MarkLabel()); // Jump over the other effects if true

			static SoundID JumpSound(SoundID jump, DoubleJumpibility doubleJump)
			{
				return doubleJump?.JumpSoundID ?? jump;
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchLdsfld(typeof(SoundID).GetField(nameof(SoundID.Fire_Spear_Explode)))
				);
			c.EmitFeatureDelegate(doubleJump, JumpSound);

			static int FirstJumpLimit(int artiJumps, DoubleJumpibility doubleJump)
			{
				if (doubleJump != null)
				{
					return Math.Max(1, doubleJump.JumpLimit.Length > 1 ? doubleJump.JumpLimit[0] : doubleJump.JumpLimit[0] - 3);
				}
				return artiJumps;
			}

			c.GotoNext(
				x => x.MatchStloc(4)
				);
			c.EmitFeatureDelegate(doubleJump, FirstJumpLimit);

			// Jump boost time woohoo!
			static float JumpBoost(float orig, DoubleJumpibility doubleJump, Player self)
			{
				if (doubleJump != null)
				{
					var jump = self.Malnourished && doubleJump.JumpBoost.Length > 1 ? doubleJump.JumpBoost[1] : doubleJump.JumpBoost[0];
					return Mathf.Max(0f, jump + (orig - 8f));
				}
				return orig;
			}

			// All of the floats we need to modify!

			void EnumerateFloats(ILCursor c, IEnumerable<float> valuesToChange)
			{
				using var values = valuesToChange.GetEnumerator();
				while (values.MoveNext())
				{
					float toChange = values.Current;

					c.GotoNext(
					MoveType.After,
					x => x.MatchLdcR4(toChange)
					);
					c.EmitFeatureDelegate(doubleJump, JumpBoost, true);
				}
			}

			EnumerateFloats(c, [
				9f, 9f, 8f, 8f,

			8f, 7f, 6f,

			16f, 15f, 10f,

			11f, 10f, 8f,

			10f, 8f,

			15f, 13f
			]);

			// Then fix up the hard jump limit
			static int JumpLimit(int artiJumps, DoubleJumpibility doubleJump)
			{
				if (doubleJump != null)
				{
					return Math.Max(1, doubleJump.JumpLimit.Length > 1 ? doubleJump.JumpLimit[1] : doubleJump.JumpLimit[0]);
				}
				return artiJumps;
			}

			static void DeathScenerio(DoubleJumpibility doubleJump, Player self)
			{
				if (doubleJump != null)
				{
					switch (doubleJump.LimitReachedResult)
					{
						case DoubleJumpibility.LimitResult.LongStun:
							self.Stun(doubleJump.StunTimers.Length > 1 ? doubleJump.StunTimers[1] : doubleJump.StunTimers[0] * 3);
							break;

						case DoubleJumpibility.LimitResult.Die:
							self.PyroDeath();
							break;

						case DoubleJumpibility.LimitResult.ConsumeFood:
							if (self.FoodInStomach >= doubleJump.FoodCost)
							{
								self.SubtractFood(doubleJump.FoodCost);
							}
							else
							{
								self.Stun(doubleJump.StunTimers[0]);
								self.SetMalnourished(true);
							}
							break;
					}
					return;
				}
				self.PyroDeath();
			}

			static void StunTimer(int jumps, DoubleJumpibility doubleJump, Player self)
			{
				if (doubleJump != null)
				{
					if (doubleJump.StunTimers[0] > 0)
					{
						self.Stun(doubleJump.StunTimers[0] * (self.pyroJumpCounter - (doubleJump.JumpLimit[0] - 1)));
					}
					return;
				}
				self.Stun(60 * (self.pyroJumpCounter - (jumps - 1)));
			}

			c.GotoNext(
				x => x.MatchLdarg(0),
				x => x.MatchLdcI4(60)
				);
			jump = c.CloneAndGoToNext(
				MoveType.After,
				x => x.MatchCallOrCallvirt(typeof(Creature).GetMethod(nameof(Creature.Stun)))
				);
			c.Emit(OpCodes.Ldloc, 12);
			c.EmitFeatureDelegate(doubleJump, StunTimer, true);
			c.Emit(OpCodes.Br, jump.MarkLabel());

			c.GotoNext(
				MoveType.After,
				x => x.MatchLdsfld(typeof(MoreSlugcats.MoreSlugcats).GetField(nameof(MoreSlugcats.MoreSlugcats.cfgArtificerExplosionCapacity))),
				x => x.MatchCallOrCallvirt(out _)
				);
			c.EmitFeatureDelegate(doubleJump, JumpLimit);

			c.GotoNext(
				x => x.MatchLdarg(0),
				x => x.MatchCallOrCallvirt(typeof(Player).GetMethod(nameof(Player.PyroDeath)))
				);
			jump = c.CloneAndGoToNext(
				MoveType.After,
				x => x.MatchCallOrCallvirt(typeof(Player).GetMethod(nameof(Player.PyroDeath)))
				);
			c.EmitFeatureDelegate(doubleJump, DeathScenerio, true);
			c.Emit(OpCodes.Br, jump.MarkLabel());

			// Enable/disable parrying
			static bool CanParry(bool flag, DoubleJumpibility doubleJump)
			{
				return flag && (doubleJump == null || doubleJump.Parry);
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchLdloc(0)
				);
			c.EmitFeatureDelegate(doubleJump, CanParry);

			EnumerateFloats(c, [
				8f, 6f, 6f
			]);

			// Edit parry effects
			c.GotoNext(
				MoveType.After,
				x => x.MatchStloc(10)
				);
			jump = c.CloneAndGoToNext(
				x => x.MatchLdarg(0),
				x => x.MatchLdfld(out _),
				x => x.MatchLdsfld(typeof(SoundID).GetField(nameof(SoundID.Fire_Spear_Explode)))
				);
			c.EmitFeatureDelegate(doubleJump, CustomJumpEffect); //LATER: replace with its own local check
			c.Emit(OpCodes.Brtrue, jump.MarkLabel());

			// Change parry sound
			c.GotoNext(
				MoveType.After,
				x => x.MatchLdsfld(typeof(SoundID).GetField(nameof(SoundID.Fire_Spear_Explode)))
				);
			c.EmitFeatureDelegate(doubleJump, JumpSound);

			// Change soft limit on parry
			c.GotoNext(
				x => x.MatchStloc(12)
				);
			c.EmitFeatureDelegate(doubleJump, JumpSoftLimit);

			c.GotoNext(
				x => x.MatchLdarg(0),
				x => x.MatchLdcI4(0x3C)
				);
			jump = c.CloneAndGoToNext(
				MoveType.After,
				x => x.MatchCallOrCallvirt(typeof(Creature).GetMethod(nameof(Creature.Stun)))
				);
			c.Emit(OpCodes.Ldloc, 12);
			c.EmitFeatureDelegate(doubleJump, StunTimer, true);
			c.Emit(OpCodes.Br, jump.MarkLabel());

			// And the other hard limit again
			c.GotoNext(
				MoveType.After,
				x => x.MatchLdsfld(typeof(MoreSlugcats.MoreSlugcats).GetField(nameof(MoreSlugcats.MoreSlugcats.cfgArtificerExplosionCapacity))),
				x => x.MatchCallOrCallvirt(out _)
				);
			c.EmitFeatureDelegate(doubleJump, JumpLimit);

			c.GotoNext(
				x => x.MatchLdarg(0),
				x => x.MatchCallOrCallvirt(typeof(Player).GetMethod(nameof(Player.PyroDeath)))
				);
			jump = c.CloneAndGoToNext(
				MoveType.After,
				x => x.MatchCallOrCallvirt(typeof(Player).GetMethod(nameof(Player.PyroDeath)))
				);
			c.EmitFeatureDelegate(doubleJump, DeathScenerio, true);
			c.Emit(OpCodes.Br, jump.MarkLabel());
		}
	}
}
