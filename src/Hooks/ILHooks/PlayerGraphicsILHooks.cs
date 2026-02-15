using MagicaHookingLibrary.Interfaces;
using MonoMod.Cil;
using SlugBase.Features;
using ExtendedSlugbase.Features;
using System;
using UnityEngine;
using Mono.Cecil.Cil;
using ExtendedSlugbase.Helpers;
using MagicaHookingLibrary.Helpers;
using static MagicaHookingLibrary.Helpers.HookHelpers;
using MoreSlugcats;
using ExtendedSlugbase.Objects;
namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class PlayerGraphicsILHooks : IOwnHooks
    {
        public void PreApply()
        {
            IL.PlayerGraphics.Update += ILAction(PlayerGraphics_Update);
            IL.PlayerGraphics.AxolotlGills.ctor += ILAction(PlayerGraphics_AxolotlGills_ctor);
            IL.PlayerGraphics.InitiateSprites += ILAction(PlayerGraphics_InitiateSprites);
            IL.PlayerGraphics.AxolotlScale.Update += ILAction(PlayerGraphics_AxolotlScale_Update);
            IL.PlayerGraphics.MSCUpdate += ILAction(PlayerGraphics_MSCUpdate);
            IL.PlayerGraphics.DrawSprites += ILAction(PlayerGraphics_DrawSprites2);
            IL.PlayerGraphics.TailSpeckles.ctor += ILAction(PlayerGraphics_TailSpeckles_ctor);
        }

        private void PlayerGraphics_Update(ILCursor c)
        {
            static bool CanRegurgitate(bool isGourm, PlayerGraphics self)
            {
                return isGourm || (self.player.TryGetFeature(PlayerFeaturesExt.canCraftObjects, out var craft) && craft.RegurgitateList.objects.Count > 0);
            }

            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Gourmand).GetSlugcatFieldInfo()
                );
            c.EmitLdarg0Delegate(CanRegurgitate);
        }


        private void PlayerGraphics_AxolotlGills_ctor(ILCursor c)
        {
            static int NumberOfGills(int rows, PlayerGraphics.AxolotlGills self)
            {
                if (self.pGraphics.player.TryGetFeature(PlayerFeaturesExt.rivGills, out var gills))
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

        private void PlayerGraphics_InitiateSprites(ILCursor c)
        {
            static void AddMoreSprites(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                if (self.player.TryGetFeature(PlayerFeaturesExt.rivGills, out _))
                {
                    self.gills.startSprite = sLeaser.sprites.Length;
                    Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + self.gills.numberOfSprites);

                    self.gills.InitiateSprites(sLeaser, rCam);
                }

                if (self.player.TryGetFeature(PlayerFeaturesExt.canCreateSpears, out _))
                {
                    self.tailSpecks.startSprite = sLeaser.sprites.Length;
                    Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + self.tailSpecks.numberOfSprites);

                    self.tailSpecks.InitiateSprites(sLeaser, rCam);
                }

                if (self.player.TryGetFeature(PlayerFeaturesExt.saintTongue, out _) && CWTs.PlayerCWT.TryGetData(self.player, out var cwt))
                {
                    cwt.saintTongueSprite = sLeaser.sprites.Length;
                    Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);

                    sLeaser.sprites[cwt.saintTongueSprite] =  TriangleMesh.MakeLongMesh(self.ropeSegments.Length - 1, false, true);
                }
            }

            c.GotoNext(
                x => x.MatchCallOrCallvirt(typeof(GraphicsModule).GetMethod(nameof(GraphicsModule.AddToContainer)))
                ); // this.AddToContainer(sLeaser, rCam, null);
            c.GotoPrev(
                MoveType.AfterLabel,
                x => x.MatchLdarg(0)
                ); // BEFORE: this.AddToContainer(sLeaser, rCam, null);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.Emit(OpCodes.Ldarg_2);
            c.EmitDelegate(AddMoreSprites);
        }
        private void PlayerGraphics_AxolotlScale_Update(ILCursor c)
        {
            static float ModifyVelocity(float value, PlayerGraphics.AxolotlScale self)
            {
                if (self.owner is PlayerGraphics pGraphics && pGraphics.player.TryGetFeature(PlayerFeaturesExt.rivGills, out var gills))
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

        private void PlayerGraphics_MSCUpdate(ILCursor c)
        {
            static bool HasTongue(bool isSaint, PlayerGraphics self)
            {
                return isSaint || self.player.TryGetFeature(PlayerFeaturesExt.saintTongue, out _);
            }

            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint).GetSlugcatFieldInfo()
                ); // if (this.player.room != null && this.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint)
            c.EmitLdarg0Delegate(HasTongue);

            // Jump over the tentacle math because we don't need it
            static bool SlugbaseTongue(PlayerGraphics self)
            {
                return self.player.TryGetFeature(PlayerFeaturesExt.saintTongue, out _);
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

        private void PlayerGraphics_DrawSprites2(ILCursor c, ILContext il)
        {
            // Set up a value we will use later, thanks Glebi
            var saintTongueSpriteIndex = il.ImplementLocalVariable<int>();

            static int SpriteIndex(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser)
            {
                if (self.player.TryGetFeature(PlayerFeaturesExt.saintTongue, out _) && CWTs.PlayerCWT.TryGetData(self.player, out var cwt))
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
                return isSaint || self.player.TryGetFeature(PlayerFeaturesExt.saintTongue, out _);
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

        private void PlayerGraphics_TailSpeckles_ctor(ILCursor c)
        {
            static void HandleRowsAndColumns(PlayerGraphics.TailSpeckles self)
            {
                if (self.pGraphics.player.TryGetFeature(PlayerFeaturesExt.canCreateSpears, out var specks))
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

        public void OnApply()
        {
        }


        public void PostApply()
        {
        }
    }
}
