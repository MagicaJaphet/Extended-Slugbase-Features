using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features.GameRelated;
public class MaxSlugpupSpawns() : GameFeature<int>("max_slugpup_spawns", JsonUtils.ToInt)
{
	internal static class Implementation
	{
		internal static int StoryGameSession_slugPupMaxCount(Func<StoryGameSession, int> orig, StoryGameSession self)
		{
			if (self.game != null && self.game.TryGetFeature(ExtGameFeatures.MaxSlugpupSpawns, out int maxPups))
			{
				return maxPups;
			}

			return orig(self);
		}
	}
}
