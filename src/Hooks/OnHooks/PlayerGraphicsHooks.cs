using System;
using MagicaHookingLibrary.Interfaces;
using SlugBase;
using SlugBase.Features;
using SlugBase.DataTypes;
using ExtendedSlugbase.Features;
using BepInEx.Logging;
using UnityEngine;
using MoreSlugcats;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RWCustom;
using MagicaHookingLibrary.Helpers;
using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features.GameRelated;
using ExtendedSlugbase.Features.PlayerRelated;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class PlayerGraphicsHooks : IOwnHooks
    {
        public void PreApply()
        {
            On.PlayerGraphics.MSCUpdate += PlayerGraphics_MSCUpdate;
            On.PlayerGraphics.ctor += PlayerGraphics_Ctor;
            On.PlayerGraphics.AxolotlGills.ctor += RivuletGills.Implementation.PlayerGraphics_AxolotlGills_ctor2;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
            On.PlayerGraphics.Update += PlayerGraphics_Update;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.SaintFaceCondition += Player_SaintFaceCondition;
            On.PlayerGraphics.ApplyPalette += ApplyColors;
            On.PlayerGraphics.TailSpeckles.DrawSprites += PlayerGraphics_TailSpeckles_DrawSprites;
        }

        private void PlayerGraphics_MSCUpdate(On.PlayerGraphics.orig_MSCUpdate orig, PlayerGraphics self)
        {
            orig(self);

			CanCreateSpears.Implementation.PlayerGraphics_MSCUpdate(self);
        }

        private static void PlayerGraphics_Ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);

			RevealMarkOverCycles.Implementation.PlayerGraphics_Ctor(self);

            int startSprite = 12;
            
            RivuletGills.Implementation.PlayerGraphics_Ctor(self, ref startSprite);

            CanCreateSpears.Implementation.PlayerGraphics_Ctor(self, ref startSprite);

            SaintTongue.Implementation.PlayerGraphics_Ctor(self);
        }

        private void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);

            var midGround = rCam.ReturnFContainer("Midground");

			RivuletGills.Implementation.PlayerGraphics_AddToContainer(self, sLeaser, rCam, midGround);

			CanCreateSpears.Implementation.PlayerGraphics_AddToContainer(self, sLeaser, rCam, midGround);

			SaintTongue.Implementation.PlayerGraphics_AddToContainer(self, sLeaser, midGround);
        }

        private void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);

            self.gills?.Update();
        }

        private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);

            self.gills?.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            self.tailSpecks?.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        private bool Player_SaintFaceCondition(On.PlayerGraphics.orig_SaintFaceCondition orig, PlayerGraphics self)
        {
            if (UseSaintFace.Implementation.PlayerGraphics_SaintFaceCondition(self))
            {
                return true;
            }
            return orig(self);
        }

        // Riv Gills: Apply color
        private static void ApplyColors(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);

            if (self.player.TryGetColorSlots(out var slots))
            {
                var bodyColor = sLeaser.sprites[0].color; // Default body color to be used when needed

				RivuletGills.Implementation.PlayerGraphics_ApplyPalette(self, sLeaser, rCam, palette, bodyColor, slots);

                SaintTongue.Implementation.PlayerGraphics_ApplyPalette(self, sLeaser, palette, slots);
            }
        }

        private void PlayerGraphics_TailSpeckles_DrawSprites(On.PlayerGraphics.TailSpeckles.orig_DrawSprites orig, PlayerGraphics.TailSpeckles self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);

            if (self.pGraphics.player.TryGetColorSlots(out var slots) && self.pGraphics.TryGetCustomColor(slots, "Spears", out Color color))
            {
                for (int row = 0; row < self.rows; row++)
                {
                    for (int line = 0; line < self.lines; line++)
                    {
                        sLeaser.sprites[self.startSprite + row * self.lines + line].color = color;

                        if (row == self.spearRow && line == self.spearLine)
                        {
                            sLeaser.sprites[self.startSprite + self.lines * self.rows].color = color;
                        }
                    }
                }
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
