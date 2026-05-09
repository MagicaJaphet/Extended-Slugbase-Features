using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features.GameRelated;
public class NoStarvation() : GameFeature<bool>("no_starvation", JsonUtils.ToBool)
{
	internal static class Implementation
	{
		//FEATURE: Inability to starve
	}
}
