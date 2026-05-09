using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using SlugBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features.TimelineRelated;

public class ShowRainTimer() : TimelineFeature<bool>("show_rain_timer", JsonUtils.ToBool)
{
	internal static class Implementation
	{
		internal static bool NoRainTimer(HUD.HUD hud)
		{
			if (hud?.owner.GetOwnerType() == HUD.HUD.OwnerType.Player && hud?.owner is Player player)
			{
				return ((ModManager.MSC && player.abstractCreature.world.game.TimelinePoint == SlugcatStats.Timeline.Saint)
				|| (player.abstractCreature.world.game.TryGetFeature(TimelineFeatures.ShowRainTimer, out bool showTimer) && !showTimer))
				&& hud?.map?.RegionName != "HR";
			}
			return false;
		}
	}
}
