using ExtendedSlugbase.Extensions;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features.PlayerRelated;
public class IsSlugpup() : PlayerFeature<bool>("is_slugpup", JsonUtils.ToBool)
{
	internal static class Implementation
	{
		internal static void Player_ctor(Player self)
		{
			if (self.TryGetFeature(ExtPlayerFeatures.IsSlugpup, out bool slugpup))
			{
				self.setPupStatus(slugpup);
			}
		}
	}
}
