using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ExtendedSlugbase.Features.PlayerRelated;
public class Warmth() : PlayerFeature<float>("body_warmth", JsonUtils.ToFloat)
{
	internal static class Implementation
	{
		internal static void Player_Update(Player self)
		{
			if (self.room != null && self.room.blizzard && self.TryGetFeature(ExtPlayerFeatures.Warmth, out float warmth))
			{
				self.Hypothermia -= Mathf.Lerp(warmth, 0f, self.HypothermiaExposure);
			}
		}
	}
}
