using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using MagicaHookingLibrary.Helpers;
using MonoMod.Cil;
using MoreSlugcats;
using SlugBase;
using SlugBase.Features;

namespace ExtendedSlugbase.Features.GameRelated;
public class GetKarmaFromScavs() : GameFeature<bool>("get_karma_from_scavs", JsonUtils.ToBool)
{
	internal static class Implementation
	{
		/// <summary>
		/// Allows the slugcat to hold scavenger corpses and get karma from them.
		/// </summary>
		/// <param name="c"></param>
		internal static void RainWorldGame_Update(ILCursor c)
		{
			// Get Karma From Scavengers: Main implementation
			static bool GetsKarmaFromScavs(bool isArtificer, RainWorldGame self)
			{
				return isArtificer || self.TryGetFeature(ExtGameFeatures.GetKarmaFromScavs, out bool getKarma) && getKarma;
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer).GetSlugcatFieldInfo()
				); // AFTER: if (ModManager.MSC && this.Players.Count > 0 && this.IsStorySession && this.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
			c.EmitLdarg0Delegate(GetsKarmaFromScavs);
		}
	}
}
