using System;
using MagicaHookingLibrary.Interfaces;
using SlugBase;
using SlugBase.Features;
using SlugBase.DataTypes;
using ExtendedSlugbase.Helpers;
using ExtendedSlugbase.Features;
using BepInEx.Logging;
using UnityEngine;
using MoreSlugcats;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RWCustom;
using MagicaHookingLibrary.Helpers;
using ExtendedSlugbase.Objects;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class PlayerGraphicsHooks : IOwnHooks
    {
        public void PreApply()
        {
            On.PlayerGraphics.MSCUpdate += PlayerGraphics_MSCUpdate;
            On.PlayerGraphics.ctor += PlayerGraphics_Ctor;
            On.PlayerGraphics.AxolotlGills.ctor += PlayerGraphics_AxolotlGills_ctor2;
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

            if (self.player.TryGetFeature(PlayerFeaturesExt.canCreateSpears, out var specks) && self.tailSpecks.spearProg > 0.1f && !specks.ReactsToSpears)
            {
                self.blink = 0;
            }
        }

        private static void PlayerGraphics_Ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);

            // Cycles to fade in
            var game = self.player.abstractCreature.world.game;
            if (game.IsStorySession 
            && game.TryGetFeature(GameFeatures.TheMark, out bool hasMark) && hasMark 
            && game.TryGetFeature(GameFeaturesExt.revealMarkOverTotalCycles, out int cycles))
            {
                self.markBaseAlpha = Mathf.Pow(Mathf.InverseLerp(4f, cycles, self.player.abstractCreature.world.game.GetStorySession.saveState.cycleNumber), 3.5f);
            }

            int startSprite = 12;
            
            if (self.player.TryGetFeature(PlayerFeaturesExt.rivGills, out _))
            {
                self.gills = new(self, startSprite);
                startSprite += self.gills.numberOfSprites;
            }

            if (self.player.TryGetFeature(PlayerFeaturesExt.canCreateSpears, out _))
            {
                self.tailSpecks = new(self, startSprite);
                startSprite += self.tailSpecks.numberOfSprites;
            }

            if (self.player.TryGetFeature(PlayerFeaturesExt.saintTongue, out var tongue))
            {
                self.ropeSegments = new PlayerGraphics.RopeSegment[tongue.Segments];
                for (int i = 0; i < tongue.Segments; i++)
                {
                    self.ropeSegments[i] = new(i, self);
                }
            }
        }

        private void PlayerGraphics_AxolotlGills_ctor2(On.PlayerGraphics.AxolotlGills.orig_ctor orig, PlayerGraphics.AxolotlGills self, PlayerGraphics pGraphics, int startSprite)
        {
            orig(self, pGraphics, startSprite);

            if (pGraphics.player.TryGetFeature(PlayerFeaturesExt.rivGills, out var gills))
            {
                for (int i = 0; i < self.scalesPositions.Length / 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        var index = i * 2 + j;
                        self.scalesPositions[index] = new Vector2((j == 0) ? (-gills.Spread) : gills.Spread, 1f - gills.Spread);
                        self.scaleObjects[index].length = gills.Length ?? self.scaleObjects[index].length;
				        self.scaleObjects[index].width = gills.Width ?? self.scaleObjects[index].width;
                    }
                }
            }
        }

        private void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);

            var midGround = rCam.ReturnFContainer("Midground");

            if (self.player.TryGetFeature(PlayerFeaturesExt.rivGills, out _))
            {
                self.gills.AddToContainer(sLeaser, rCam, midGround);
            }

            if (self.player.TryGetFeature(PlayerFeaturesExt.canCreateSpears, out _))
            {
                self.tailSpecks.AddToContainer(sLeaser, rCam, midGround);
            }

            if (self.player.TryGetFeature(PlayerFeaturesExt.saintTongue, out _) && CWTs.PlayerCWT.TryGetData(self.player, out var cwt))
            {
                midGround.AddChild(sLeaser.sprites[cwt.saintTongueSprite]);
            }
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
            if (self.player.TryGetFeature(PlayerFeaturesExt.usesSaintFaceCondition, out bool saintFace))
            {
                return saintFace;
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

                if (self.player.TryGetFeature(PlayerFeaturesExt.rivGills, out _)
                && self.TryGetCustomColor(slots, "Gills", out var gillCol))
                {
                    self.gills?.SetGillColors(bodyColor, gillCol);
                    self.gills?.ApplyPalette(sLeaser, rCam, palette);
                }

                // Hell on earth
                if (self.player.TryGetFeature(PlayerFeaturesExt.saintTongue, out _)
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
