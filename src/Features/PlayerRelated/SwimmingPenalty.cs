using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features.PlayerRelated;
public class SwimmingPenalty() : PlayerFeature<SwimmingPenalty.Hydrophobia>("swimming_penalty", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static Hydrophobia Factory(JsonAny json) => new(json);

	public class Hydrophobia
	{
		public Hydrophobia(JsonAny json)
		{
			//FEATURE: Slugcat fucking explodes when trying to swim
		}
	}
}
