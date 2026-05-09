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
public class PullSpearsFromWalls() : PlayerFeature<bool>("take_spears_from_wall", JsonUtils.ToBool)
{
	internal static class Implementation
	{
		internal static void Player_CanIPickThisUp(ILContext il)
		{
			ILCursor c = new(il);

			static bool CanPullSpears(bool isNotArtiOrMSC, Player self)
			{
				return isNotArtiOrMSC && (!self.TryGetFeature(ExtPlayerFeatures.PullSpearsFromWalls, out bool canPullSpears) || !canPullSpears);
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer).GetSlugcatFieldInfo()
				); // if ((obj as Weapon).mode == Weapon.Mode.StuckInWall && (!ModManager.MMF || !MMF.cfgDislodgeSpears.Value) && (!ModManager.MSC || this.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Artificer)            
			c.EmitLdarg0Delegate(CanPullSpears);
		}
	}
}
