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
public class SpawnKarmaFlowers() : GameFeature<bool>("spawn_karma_flowers", JsonUtils.ToBool)
{
	internal static class Implementation
	{
		internal static bool KarmaFlower_CanSpawnKarmaFlower(On.KarmaFlower.orig_CanSpawnKarmaFlower orig, Room room)
		{
			return orig(room) && (!room.game.TryGetFeature(ExtGameFeatures.SpawnKarmaFlowers, out bool shouldSpawn) || shouldSpawn);
		}
	}
}
