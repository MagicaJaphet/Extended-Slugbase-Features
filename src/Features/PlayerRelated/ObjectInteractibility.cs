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

namespace ExtendedSlugbase.Features.PlayerRelated;
public class ObjectInteractions() : PlayerFeature<ObjectInteractions.ObjectInteractibility>("object_interactions", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static ObjectInteractibility Factory(JsonAny json) => new(json);

	/// <summary>
	/// JSON Object which holds information about several object-specific interactions for a slugcat.
	/// </summary>
	public class ObjectInteractibility
	{
		public static float? lastAteMushroomFPS;
		public bool PopBubbleFruit { get; } = false;
		public float BubbleWeedUsageMultiplier { get; } = 1f;
		public bool ExplosiveImmune { get; } = false;
		public int MushroomTimer { get; } = 320;
		public float MushroomFPS { get; } = 15f;
		public bool PoisonImmune { get; } = false;

		internal ObjectInteractibility(JsonAny json)
		{
			//LATER: God can't help me now x3
			/*	
				POPCORN: pop when standing near
				SPOREPUFF: stun time from exposure, affected by amounts with possibility of death
				BATNIP: Idk for this one honestly lol
				ACID: Immunities to touching / swimming in it
			*/

			if (json.TryParse(out JsonObject obj))
			{
				if (obj.TryGet("pop_bubble_fruit", out bool popBubbleFruit))
				{
					PopBubbleFruit = popBubbleFruit;
				}
				if (obj.TryGet("bubble_weed_usage_multiplier", out float usageMultiplier))
				{
					BubbleWeedUsageMultiplier = usageMultiplier;
				}
				if (obj.TryGet("poison_immune", out bool poisonImmune))
				{
					PoisonImmune = poisonImmune;
				}
				if (obj.TryGet("explosive_immune", out bool explosiveImmune))
				{
					ExplosiveImmune = explosiveImmune;
				}
				if (obj.TryGet("mushroom_interactions", out JsonObject mushroom))
				{
					if (mushroom.TryGet("timer", out int timer))
					{
						MushroomTimer = timer;
					}
					if (mushroom.TryGet("frames_per_second", out int framesPerSecond))
					{
						MushroomFPS = framesPerSecond;
					}
				}
			}
		}
	}

	internal static class Implementation
	{
		internal static void BubbleGrass_Update(ILContext il)
		{
			ILCursor c = new(il);

			static float BubbleWeedMultiplier(float orig, BubbleGrass self)
			{
				if (self.grabbedBy?.FirstOrDefault(x => x.grabber is Player)?.grabber is Player player && player.TryGetFeature(ExtPlayerFeatures.ObjectInteractions, out var objectInteractions))
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

		internal static void Creature_InjectPoison(On.Creature.orig_InjectPoison orig, Creature self, float amount, Color poisonColor)
		{
			if (self is Player player && player.TryGetFeature(ExtPlayerFeatures.ObjectInteractions, out var objectInteractions) && objectInteractions.PoisonImmune)
			{
				return;
			}
			orig(self, amount, poisonColor);
		}


		internal static void Explosion_Update(ILContext il)
		{
			ILCursor c = new(il);

			static bool ExplosionImmunity(bool isArti, Explosion self, int j, int k)
			{
				return isArti || self.room.physicalObjects[j][k] is Player player && player.TryGetFeature(ExtPlayerFeatures.ObjectInteractions, out var interactions) && interactions.ExplosiveImmune;
			}
			static bool NoExplosionImmunity(bool isNotArti, Explosion self, int j, int k)
			{
				return isNotArti && !(self.room.physicalObjects[j][k] is Player player && player.TryGetFeature(ExtPlayerFeatures.ObjectInteractions, out var interactions) && interactions.ExplosiveImmune);
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

		internal static void Mushroom_BitByPlayer(ILContext il)
		{
			ILCursor c = new(il);

			static int MushroomCounter(int timer, Creature.Grasp grasp)
			{
				if (grasp.grabber is Player player && player.TryGetFeature(ExtPlayerFeatures.ObjectInteractions, out var objInteractions))
				{
					ObjectInteractibility.lastAteMushroomFPS = objInteractions.MushroomFPS;
					return objInteractions.MushroomTimer;
				}
				ObjectInteractibility.lastAteMushroomFPS = null;
				return timer;
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchLdcI4(320)
				); // (grasp.grabber as Player).mushroomCounter += 320;
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate(MushroomCounter);
		}

		internal static void RainWorldGame_RawUpdate(ILCursor c)
		{
			static float FramesPerSecondMushroom(float orig, RainWorldGame self)
			{
				if (ObjectInteractibility.lastAteMushroomFPS is float fps)
				{
					return fps;
				}
				return orig;
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchConvR4(),
				x => x.MatchLdcR4(15)
				); // float num2 = flag ? Mathf.Lerp((float)this.framesPerSecond, 8f, num) : Mathf.Lerp((float)this.framesPerSecond, 15f, num);
			c.EmitLdarg0Delegate(FramesPerSecondMushroom);
		}

		internal static void Spear_HitSomethingWithoutStopping(Player player)
		{
			player.mushroomCounter += player.TryGetFeature(ExtPlayerFeatures.ObjectInteractions, out var objectInteractions) ? objectInteractions.MushroomTimer : 320;
		}

		internal static void WaterNut_Update(ILContext il)
		{
			ILCursor c = new(il);

			static bool PopWaterNut(bool isRivulet, WaterNut self, int grasp)
			{
				return isRivulet || (self.grabbedBy[grasp].grabber as Player).TryGetFeature(ExtPlayerFeatures.ObjectInteractions, out var objectInteractions) && objectInteractions.PopBubbleFruit;
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Rivulet).GetSlugcatFieldInfo()
				);
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloc, 1);
			c.EmitDelegate(PopWaterNut);
		}
	}
}
