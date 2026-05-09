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
public class TossSpears() : PlayerFeature<bool>("only_tosses_spears", JsonUtils.ToBool)
{
	internal static class Implementation
	{
		internal static void Player_ThrowObject(ILCursor c)
		{
			static bool TossSpears(bool isSaint, Player self)
			{
				return isSaint || self.TryGetFeature(ExtPlayerFeatures.TossSpears, out bool tossSpear) && tossSpear;
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint).GetSlugcatFieldInfo()
				);
			c.EmitLdarg0Delegate(TossSpears);
		}
	}
}
