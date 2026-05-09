using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features.PlayerRelated;
public class AerobicExhaustion() : PlayerFeature<int[]>("exhaustion", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static int[] Factory(JsonAny json) => JsonUtils.ToInts(ExtJsonUtils.AssertLength(json, 1, 2));

	// TODO: Implement
}
