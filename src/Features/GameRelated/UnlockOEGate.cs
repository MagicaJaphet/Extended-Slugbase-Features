using ExtendedSlugbase.Features;
using static ExtendedSlugbase.Features.ExtFeatureTypes;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ExtendedSlugbase.Extensions.SlugBaseExtensions;
using ExtendedSlugbase.Extensions;

namespace ExtendedSlugbase.Features.GameRelated;
public class UnlockOEGate() : GameFeature<bool[]>("can_pass_OE_gate", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static bool[] Factory(JsonAny json)
	{
		return ToBools(ExtJsonUtils.AssertLength(json, 1, 2));
	}

	internal static class Implementation
	{
		/// <summary>
		/// Unlock logic, including the additional bool which determines if <see cref="MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Gourmand"/>'s campaign needs to be beaten first.
		/// </summary>
		internal static bool RegionGate_customOEGateRequirements(On.RegionGate.orig_customOEGateRequirements orig, RegionGate self)
		{
			bool gourmandUnlockedOE = self.room.game.IsStorySession && (self.room.game.rainWorld.progression.miscProgressionData.beaten_Gourmand || self.room.game.rainWorld.progression.miscProgressionData.beaten_Gourmand_Full || MoreSlugcats.MoreSlugcats.chtUnlockOuterExpanse.Value);
			return orig(self) || self.room.game.TryGetFeature(ExtGameFeatures.UnlockOEGate, out bool[] flags) && flags[0] && (!(flags.Length == 2 && flags[1]) || gourmandUnlockedOE);
		}
	}
}
