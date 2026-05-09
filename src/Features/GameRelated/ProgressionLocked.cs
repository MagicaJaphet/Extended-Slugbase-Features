using MagicaHookingLibrary.Helpers;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features.GameRelated;
public class ProgressionLocked() : GameFeature<List<SlugcatStats.Name>>("progression_locked", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static List<SlugcatStats.Name> Factory(JsonAny json) => json.TryParse(out SlugcatStats.Name[] names, 1) ? [.. names] : null;

	internal static class Implementation
	{
		// TODO: Implement
	}
}
