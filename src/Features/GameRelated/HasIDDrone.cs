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
public class HasIDDrone() : GameFeature<bool>("has_id_drone", JsonUtils.ToBool)
{
	internal static class Implementation
	{
		internal static void PlayerProgression_GetOrInitateSaveState(SlugcatStats.Name saveStateNumber, ref SaveState saveState)
		{
			if (saveStateNumber.TryGetFeature(ExtGameFeatures.HasIDDrone, out bool hasID))
			{
				saveState.hasRobo = hasID;
			}
		}
	}
}
