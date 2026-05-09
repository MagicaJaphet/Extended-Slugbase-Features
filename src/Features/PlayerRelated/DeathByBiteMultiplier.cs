using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using SlugBase;
using SlugBase.Features;

namespace ExtendedSlugbase.Features.PlayerRelated;
public class DeathByBiteMultiplier() : PlayerFeature<float[]>("bite_lethality_mutliplier", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static float[] Factory(JsonAny json) => JsonUtils.ToFloats(ExtJsonUtils.AssertLength(json, 1, 2));

	internal static class Implementation
	{
		internal static float Player_DeathByBiteMultiplier(On.Player.orig_DeathByBiteMultiplier orig, Player self)
		{
			if (self.TryGetFeature(ExtPlayerFeatures.DeathByBiteMultiplier, out float[] multipliers))
			{
				if (self.room != null && self.room.game.IsStorySession)
				{
					return multipliers[0] + self.room.game.GetStorySession.difficulty / (multipliers.Length == 1 ? 5f : multipliers[1]);
				}
				return multipliers[0] + 0.05f;
			}
			return orig(self);
		}
	}
}
