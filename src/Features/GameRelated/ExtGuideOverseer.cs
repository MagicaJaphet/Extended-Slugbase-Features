using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features.GameRelated;
public class ExtGuideOverseer() : GameFeature<ExtGuideOverseer.GuidingProperties>("guide_overseer_properties", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static GuidingProperties Factory(JsonAny json) => new(json);

	public class GuidingProperties
	{
		public GuidingProperties(JsonAny json)
		{
			if (json.TryParse(out JsonObject obj))
			{
				//FEATURE: Guide overseer region priority (with room priorities)
				//FEATURE: Guide overseer icon
			}
		}
	}
}
