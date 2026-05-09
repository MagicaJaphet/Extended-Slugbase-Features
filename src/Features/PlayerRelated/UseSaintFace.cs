using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features.PlayerRelated;
public class UseSaintFace() : PlayerFeature<bool>("saint_eyes", JsonUtils.ToBool)
{
	internal static class Implementation
	{
		internal static bool PlayerGraphics_SaintFaceCondition(PlayerGraphics self)
		{
			if (self.player.TryGetFeature(ExtPlayerFeatures.UseSaintFace, out bool saintFace))
			{
				return saintFace;
			}
			return false;
		}
	}
}
