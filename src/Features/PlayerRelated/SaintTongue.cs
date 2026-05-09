using ExtendedSlugbase.Extensions;
using MagicaHookingLibrary.Helpers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using SlugBase;
using SlugBase.DataTypes;
using SlugBase.Features;
using System;
using UnityEngine;

namespace ExtendedSlugbase.Features.PlayerRelated;
public class SaintTongue() : PlayerFeature<SaintTongue.Tongue>("grapple_tongue", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static Tongue Factory(JsonAny json) => new(json);

	/// <summary>
	/// JSON Object to hold information about a player's tongue.
	/// </summary>
	public class Tongue
	{
		public float Length { get; } = 150f;
		public float Thickness { get; } = 1f;
		public bool Retractable { get; } = true;
		public float[] RetractLengths { get; } = [50f, 170f];
		public int Segments { get; } = 20;
		public float RetractSpeed { get; } = 1f;

		internal Tongue(JsonAny json)
		{
			if (json.TryParse(out JsonObject obj))
			{
				if (obj.TryGet("segments", out int segs))
				{
					Segments = segs;
				}
				if (obj.TryGet("length", out float length))
				{
					Length = length;
				}
				if (obj.TryGet("retract_lengths", out float[] lengths, 2, 2))
				{
					RetractLengths = lengths;
				}
				if (obj.TryGet("retract_speed", out float retractSpeed))
				{
					RetractSpeed = retractSpeed;
				}
				if (obj.TryGet("retractable", out bool retractable))
				{
					Retractable = retractable;
				}
			}
		}
	}

	internal static class Implementation
	{
		// For IL local variables
		internal static Tongue TongueFeature(Player self) => ExtPlayerFeatures.SaintTongue.Get(self);
		internal static Tongue TongueFeature_Tongue(Player.Tongue self) => ExtPlayerFeatures.SaintTongue.Get(self.player);

		internal static void Player_ctor(Player self)
		{
			if (self.TryGetFeature(ExtPlayerFeatures.SaintTongue, out var tongue))
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
		}

		internal static void PlayerGraphics_AddToContainer(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, FContainer midGround)
		{
			if (self.player.TryGetFeature(ExtPlayerFeatures.SaintTongue, out _) && CWTs.PlayerCWT.TryGetData(self.player, out var cwt))
			{
				midGround.AddChild(sLeaser.sprites[cwt.saintTongueSprite]);
			}
		}

		internal static void PlayerGraphics_ApplyPalette(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomPalette palette, ColorSlot[] slots)
		{
			if (self.player.TryGetFeature(ExtPlayerFeatures.SaintTongue, out _)
				&& self.TryGetCustomColor(slots, "Tongue", out var tongueCol)
				&& CWTs.PlayerCWT.TryGetData(self.player, out var cwt))
			{
				TriangleMesh mesh = sLeaser.sprites[cwt.saintTongueSprite] as TriangleMesh;
				for (int j = 0; j < mesh.verticeColors.Length; j++)
				{
					mesh.verticeColors[j] = Color.Lerp(palette.fogColor, tongueCol, 0.7f);
				}
			}
		}

		internal static void PlayerGraphics_Ctor(PlayerGraphics self)
		{
			if (self.player.TryGetFeature(ExtPlayerFeatures.SaintTongue, out var tongue))
			{
				self.ropeSegments = new PlayerGraphics.RopeSegment[tongue.Segments];
				for (int i = 0; i < tongue.Segments; i++)
				{
					self.ropeSegments[i] = new(i, self);
				}
			}
		}

		internal static void PlayerGraphics_InitiateSprites(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser)
		{
			if (self.player.TryGetFeature(ExtPlayerFeatures.SaintTongue, out _) && CWTs.PlayerCWT.TryGetData(self.player, out var cwt))
			{
				cwt.saintTongueSprite = sLeaser.sprites.Length;
				Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);

				sLeaser.sprites[cwt.saintTongueSprite] = TriangleMesh.MakeLongMesh(self.ropeSegments.Length - 1, false, true);
			}
		}

		internal static void PlayerGraphics_DrawSprites(ILCursor c, ILContext il)
		{
			// Set up a value we will use later, thanks Glebi
			var saintTongueSpriteIndex = il.ImplementLocalVariable<int>();

			static int SpriteIndex(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser)
			{
				if (self.player.TryGetFeature(ExtPlayerFeatures.SaintTongue, out _) && CWTs.PlayerCWT.TryGetData(self.player, out var cwt))
				{
					return cwt.saintTongueSprite;
				}
				return 12;
			}
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate(SpriteIndex);
			c.Emit(OpCodes.Stloc, saintTongueSpriteIndex); // Now our local variable should have our value

			// Draw sprites if we have a tongue
			static bool HasTongue(bool isSaint, PlayerGraphics self)
			{
				return isSaint || self.player.TryGetFeature(ExtPlayerFeatures.SaintTongue, out _);
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint).GetSlugcatFieldInfo()
				);
			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint).GetSlugcatFieldInfo()
				); // if (this.player.room != null && this.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint)
			c.EmitLdarg0Delegate(HasTongue);

			// Now we need to fix the indexing for the sprites because why are they hardcoded i stg
			for (int i = 0; i < 6; i++)
			{
				c.GotoNext(
					MoveType.After,
					x => x.MatchLdcI4(12)
					); // The index of the sprite
				c.Emit(OpCodes.Pop);
				c.Emit(OpCodes.Ldloc, saintTongueSpriteIndex);
			}
		}

		internal static void PlayerGraphics_MSCUpdate(ILCursor c)
		{
			static bool HasTongue(bool isSaint, PlayerGraphics self)
			{
				return isSaint || self.player.TryGetFeature(ExtPlayerFeatures.SaintTongue, out _);
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint).GetSlugcatFieldInfo()
				); // if (this.player.room != null && this.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint)
			c.EmitLdarg0Delegate(HasTongue);

			// Jump over the tentacle math because we don't need it
			static bool SlugbaseTongue(PlayerGraphics self)
			{
				return self.player.TryGetFeature(ExtPlayerFeatures.SaintTongue, out _);
			}

			c.GotoNext(MoveType.After, x => x.MatchLdarg(0)); // if (this.tentaclesVisible > 0 && this.darkenFactor == 0f)
			ILCursor jump = c.CloneAndGoToNext(
				x => x.MatchLdarg(0),
				x => x.MatchLdarg(0),
				x => x.MatchLdfld(out _)
				); // this.lastStretch = this.stretch;
			c.EmitDelegate(SlugbaseTongue);
			c.Emit(OpCodes.Brtrue, jump.MarkLabel());
			c.Emit(OpCodes.Ldarg_0); // We consume the ldarg_0 so put it back on the stack
		}


		internal static void Player_SaintTongueCheck(ILContext il)
		{
			ILCursor c = new(il);

			static bool HasTongue(bool isSaint, Player self)
			{
				return isSaint || self.TryGetFeature(ExtPlayerFeatures.SaintTongue, out _);
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint).GetSlugcatFieldInfo()
				);
			c.EmitLdarg0Delegate(HasTongue);
		}

		internal static float Player_Tongue_TotalRope(Func<Player.Tongue, float> orig, Player.Tongue self)
		{
			if (self.player.TryGetFeature(ExtPlayerFeatures.SaintTongue, out var tongue))
			{
				return Mathf.Max(tongue.Length + 50f, tongue.RetractLengths[1] + 30f);
			}
			return orig(self);
		}

		internal static void Player_TongueUpdate(ILContext il)
		{
			ILCursor c = new(il);

			var saintTongue = ExtPlayerFeatures.SaintTongue.ImplementFeatureVariable<Tongue, Player>(il, c);
			static float RopeLengthFactor(float orig, Tongue tongue)
			{
				return tongue?.RetractSpeed * orig ?? orig;
			}

			c.GotoNext(
				x => x.MatchCallOrCallvirt(typeof(Player.Tongue).GetMethod(nameof(Player.Tongue.decreaseRopeLength)))
				);
			c.EmitFeatureDelegate(saintTongue, RopeLengthFactor);

			c.GotoNext(
				x => x.MatchCallOrCallvirt(typeof(Player.Tongue).GetMethod(nameof(Player.Tongue.increaseRopeLength)))
				);
			c.EmitFeatureDelegate(saintTongue, RopeLengthFactor);
		}

		internal static void Player_Tongue_Update(ILContext il)
		{
			ILCursor c = new(il);

			var saintTongue = ExtPlayerFeatures.SaintTongue.ImplementFeatureVariable<Tongue, Player.Tongue>(il, c, TypeConverters.GetPlayer);
			static float RetractSpeed(float orig, Tongue tongue)
			{
				return tongue?.RetractSpeed ?? orig;
			}

			for (int i = 0; i < 2; i++)
			{
				c.GotoNext(
					x => x.MatchLdcR4(1),
					x => x.MatchLdarg(0),
					x => x.MatchLdfld(typeof(Player.Tongue).GetField(nameof(Player.Tongue.elastic)))
					);
				c.GotoNext(x => x.MatchLdarg(0));
				c.EmitFeatureDelegate(saintTongue, RetractSpeed);
			}
		}
	}
}
