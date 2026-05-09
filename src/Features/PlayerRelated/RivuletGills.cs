using ExtendedSlugbase.Extensions;
using MagicaHookingLibrary.Helpers;
using MonoMod.Cil;
using SlugBase;
using SlugBase.DataTypes;
using SlugBase.Features;
using System;
using UnityEngine;

namespace ExtendedSlugbase.Features.PlayerRelated;
public class RivuletGills(): PlayerFeature<RivuletGills.Gills>("gills", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static Gills Factory(JsonAny json) => new(json);

	/// <summary>
	/// JSON Object to hold information about player's gills.
	/// </summary>
	public class Gills
	{
		public int Rows { get; } = 3;
		public float Bounciness { get; } = 1f;
		public float Drag { get; } = 1f;
		public float Spread { get; } = 0.65f;

		public float? Length { get; }
		public float? Width { get; }

		// public string[] SpriteElements { get; }

		internal Gills(JsonAny json)
		{
			if (json.TryParse(out JsonObject obj))
			{
				if (obj.TryGet("rows", out int rows))
				{
					Rows = rows;
				}
				if (obj.TryGet("bounciness", out float bounciness))
				{
					Bounciness = bounciness;
				}
				if (obj.TryGet("drag", out float drag))
				{
					Drag = drag;
				}
				if (obj.TryGet("length", out float length))
				{
					Length = length;
				}
				if (obj.TryGet("width", out float width))
				{
					Width = width;
				}
				if (obj.TryGet("spread", out float spread))
				{
					Spread = spread;
				}
				if (obj.TryGet("element_names", out string[] names))
				{
					//FEATURE: JSON atlas/element loader handler
					// foreach (var name in names)
					// {
					//     if (Futile.atlasManager.GetElementWithName(name) == null)
					//     {
					//         throw new JsonException($"{name} is not a valid element! Make sure your sprite is loaded.", obj);
					//     }
					// }
					// SpriteElements = names;
				}
			}
		}
	}

	internal static class Implementation
	{
		internal static void PlayerGraphics_AddToContainer(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer midGround)
		{
			if (self.player.TryGetFeature(ExtPlayerFeatures.RivuletGills, out _))
			{
				self.gills.AddToContainer(sLeaser, rCam, midGround);
			}
		}

		internal static void PlayerGraphics_ApplyPalette(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette, Color bodyColor, ColorSlot[] slots)
		{
			if (self.player.TryGetFeature(ExtPlayerFeatures.RivuletGills, out _)
				&& self.TryGetCustomColor(slots, "Gills", out var gillCol))
			{
				self.gills?.SetGillColors(bodyColor, gillCol);
				self.gills?.ApplyPalette(sLeaser, rCam, palette);
			}
		}

		internal static void PlayerGraphics_AxolotlGills_ctor(ILContext il)
		{
			ILCursor c = new(il);

			static int NumberOfGills(int rows, PlayerGraphics.AxolotlGills self)
			{
				if (self.pGraphics.player.TryGetFeature(ExtPlayerFeatures.RivuletGills, out var gills))
				{
					self.rigor = 1f - gills.Bounciness;
					return gills.Rows;
				}
				return rows;
			}

			c.GotoNext(
				x => x.MatchStloc(1)
				); // int num2 = 3;
			c.EmitLdarg0Delegate(NumberOfGills);
		}
		internal static void PlayerGraphics_AxolotlGills_ctor2(On.PlayerGraphics.AxolotlGills.orig_ctor orig, PlayerGraphics.AxolotlGills self, PlayerGraphics pGraphics, int startSprite)
		{
			orig(self, pGraphics, startSprite);

			if (pGraphics.player.TryGetFeature(ExtPlayerFeatures.RivuletGills, out var gills))
			{
				for (int i = 0; i < self.scalesPositions.Length / 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						var index = i * 2 + j;
						self.scalesPositions[index] = new Vector2(j == 0 ? -gills.Spread : gills.Spread, 1f - gills.Spread);
						self.scaleObjects[index].length = gills.Length ?? self.scaleObjects[index].length;
						self.scaleObjects[index].width = gills.Width ?? self.scaleObjects[index].width;
					}
				}
			}
		}

		internal static void PlayerGraphics_AxolotlScale_Update(ILContext il)
		{
			ILCursor c = new(il);

			static float ModifyVelocity(float value, PlayerGraphics.AxolotlScale self)
			{
				if (self.owner is PlayerGraphics pGraphics && pGraphics.player.TryGetFeature(ExtPlayerFeatures.RivuletGills, out var gills))
				{
					return value * gills.Drag;
				}
				return value;
			}

			c.GotoNext(
				x => x.MatchCallOrCallvirt(out _),
				x => x.MatchStfld(typeof(PlayerGraphics.AxolotlScale).GetField(nameof(PlayerGraphics.AxolotlScale.vel)))
				); // this.vel *= 0.5f;
			c.EmitLdarg0Delegate(ModifyVelocity);

			c.GotoNext(
				x => x.MatchCallOrCallvirt(out _),
				x => x.MatchStfld(typeof(PlayerGraphics.AxolotlScale).GetField(nameof(PlayerGraphics.AxolotlScale.vel)))
				); // this.vel *= 0.9f;
			c.EmitLdarg0Delegate(ModifyVelocity);
		}

		internal static void PlayerGraphics_Ctor(PlayerGraphics self, ref int startSprite)
		{
			if (self.player.TryGetFeature(ExtPlayerFeatures.RivuletGills, out _))
			{
				self.gills = new(self, startSprite);
				startSprite += self.gills.numberOfSprites;
			}
		}

		internal static void PlayerGraphics_InitiateSprites(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			if (self.player.TryGetFeature(ExtPlayerFeatures.RivuletGills, out _))
			{
				self.gills.startSprite = sLeaser.sprites.Length;
				Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + self.gills.numberOfSprites);

				self.gills.InitiateSprites(sLeaser, rCam);
			}
		}
	}
}
