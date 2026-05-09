using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using MagicaHookingLibrary.Helpers;
using MonoMod.Cil;
using MoreSlugcats;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features.PlayerRelated;
public class GourmandSlam() : PlayerFeature<bool>("can_slam", JsonUtils.ToBool)
{
	internal static class Implementation
	{
        internal static void Player_Collide(ILContext il)
		{
			ILCursor c = new(il);

			static bool CanSlam(bool isGourm, Player self)
            {
                return isGourm || self.TryGetFeature(ExtPlayerFeatures.GourmandSlam, out var canSlam) && canSlam;
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(Player).GetProperty(nameof(Player.isGourmand), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod())
                );
            c.MoveAfterLabels();
            c.EmitLdarg0Delegate(CanSlam);

            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Gourmand).GetSlugcatFieldInfo()
                ); // if (this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Gourmand && this.animation == Player.AnimationIndex.Roll && this.gourmandAttackNegateTime <= 0)
            c.EmitLdarg0Delegate(CanSlam);
        }

		internal static void Player_SlugSlamConditions(ILContext il)
		{
			ILCursor c = new(il);

			static bool CantSlam(bool isNotGourm, Player self)
			{
				return isNotGourm && !(self.TryGetFeature(ExtPlayerFeatures.GourmandSlam, out var canSlam) && canSlam);
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Gourmand).GetSlugcatFieldInfo()
				);
			c.EmitLdarg0Delegate(CantSlam);
		}
	}
}
