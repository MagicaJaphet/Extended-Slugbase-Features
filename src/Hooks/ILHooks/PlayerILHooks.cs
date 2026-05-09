using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using ExtendedSlugbase.Features.GameRelated;
using ExtendedSlugbase.Features.PlayerRelated;
using MagicaHookingLibrary.Helpers;
using MagicaHookingLibrary.Interfaces;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using SlugBase;
using SlugBase.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ExtendedSlugbase.Features.PlayerRelated.CanCraftObjects;
using static ExtendedSlugbase.Features.PlayerRelated.CanCreateSpears;
using static MagicaHookingLibrary.Helpers.HookHelpers;

namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class PlayerILHooks : IOwnHooks
    {
        public void PreApply()
        {
            IL.Player.SlugSlamConditions += GourmandSlam.Implementation.Player_SlugSlamConditions;
            IL.Player.Collide += GourmandSlam.Implementation.Player_Collide;
            IL.Player.ClassMechanicsSpearmaster += CanCreateSpears.Implementation.Player_ClassMechanicsSpearmaster;
            IL.Player.ThrowObject += Player_ThrowObject;
            IL.Player.CanIPickThisUp += PullSpearsFromWalls.Implementation.Player_CanIPickThisUp;
			IL.Player.SaintTongueCheck += SaintTongue.Implementation.Player_SaintTongueCheck;
			IL.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;
            IL.Player.GrabUpdate += Player_GrabUpdate;
            IL.Player.Stun += NoStunGraspPenalties.Implementation.Player_Stun;
            IL.Player.TongueUpdate += SaintTongue.Implementation.Player_TongueUpdate;
            IL.Player.Tongue.Update += SaintTongue.Implementation.Player_Tongue_Update;
            IL.Player.ClassMechanicsArtificer += DoubleJump.Implementation.Player_ClassMechanicsArtificer;
            IL.Player.Regurgitate += CanCraftObjects.Implementation.Player_Regurgitate;
        }

        private void Player_ThrowObject(ILContext il)
		{
			ILCursor c = new(il);

			TossSpears.Implementation.Player_ThrowObject(c);
        }

        private void Player_ClassMechanicsSaint(ILContext il)
		{
			ILCursor c = new(il);

			static bool HasTongueOrGhostPing(bool isSaint, Player self)
            {
                return isSaint || self.TryGetFeature(ExtPlayerFeatures.SaintTongue, out _) || (self.room != null && self.room.game.TryGetFeature(ExtGameFeatures.HasGhostPing, out bool ghostPing) && ghostPing);
            }

            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint).GetSlugcatFieldInfo()
                );
            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint).GetSlugcatFieldInfo()
                );
            c.EmitLdarg0Delegate(HasTongueOrGhostPing);

			HasGhostPing.Implementation.Player_ClassMechanicsSaint(c);
        }

        private void Player_GrabUpdate(ILContext il)
		{
			ILCursor c = new(il);

			var specksFeature = ExtPlayerFeatures.CanCreateSpears.ImplementFeatureVariable<SpearCreating, Player>(il, c);
			var craftFeature = ExtPlayerFeatures.CanCraftObjects.ImplementFeatureVariable<Craftability, Player>(il, c);
			var cantSwallowFeature = ExtPlayerFeatures.CantSwallowObjects.ImplementFeatureVariable<bool, Player>(il, c);

			CanCreateSpears.Implementation.Player_GrabUpdate_1(c, specksFeature);

			CanCraftObjects.Implementation.Player_GrabUpdate_1(c, craftFeature);

			CantSwallowObjects.Implementation.Player_GrabUpdate_1(c, cantSwallowFeature);

			CanCreateSpears.Implementation.Player_GrabUpdate_2(c, specksFeature);

			CantSwallowObjects.Implementation.Player_GrabUpdate_2(c, cantSwallowFeature);

			CanCreateSpears.Implementation.Player_GrabUpdate_3(c, specksFeature);

			CanCraftObjects.Implementation.Player_GrabUpdate_2(c, craftFeature);
		}

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
