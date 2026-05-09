using ExtendedSlugbase.Extensions;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features.GameRelated;
public class HiddenOrUnplayable() : GameFeature<bool>("hidden_or_unplayable", JsonUtils.ToBool)
{
	// TODO: Implement
	internal static class Implementation
	{
		internal static bool SlugcatStats_HiddenOrUnplayableSlugcat(SlugcatStats.Name i)
		{
			return i.TryGetFeature(ExtGameFeatures.HiddenOrUnplayable, out bool hidden) && hidden;
		}
	}
}
