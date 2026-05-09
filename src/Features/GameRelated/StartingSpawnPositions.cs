using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using RWCustom;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features.GameRelated;
public class StartingSpawnPositions() : GameFeature<IntVector2[]>("start_position", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static IntVector2[] Factory(JsonAny json)
	{
		IntVector2[] startingPositions = [];

		if (json.TryParse(out JsonList list))
		{
			var rooms = list.ParseListItems<JsonAny>(throwIfParseError: false);
			if (rooms.Length > 0)
			{
				if (rooms[0].Type == JsonAny.Element.List)
				{
					startingPositions = [.. from room in rooms let roomList = room.AsList() select new IntVector2(roomList.GetInt(0), roomList.GetInt(1))];
				}
				else
				{
					startingPositions = [new IntVector2(list.GetInt(0), list.GetInt(1))];
				}
			}
		}

		return startingPositions;
	}

	internal static class Implementation
	{
		internal static bool RainWorldGame_TryGetPlayerStartPos(On.RainWorldGame.orig_TryGetPlayerStartPos orig, string room, out IntVector2 pos)
		{
			var result = orig(room, out pos);

			if (Custom.rainWorld.inGameSlugCat is SlugcatStats.Name name
			&& name.TryGetFeature(GameFeatures.StartRoom, out string[] rooms)
			&& name.TryGetFeature(ExtGameFeatures.StartingSpawnPositions, out IntVector2[] positions))
			{
				// Finds the room name in the rooms array by weakly comparing all of the names in the rooms array :|
				var roomIndex = rooms.IndexOf(rooms.FirstOrDefault(r => string.Equals(r, room, StringComparison.InvariantCultureIgnoreCase)));
				if (roomIndex > -1 && roomIndex < positions.Length)
				{
					pos = positions[roomIndex];
					return true;
				}
			}

			return result;
		}
	}
}
