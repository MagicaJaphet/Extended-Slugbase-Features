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
using static ExtendedSlugbase.DataTypes.AbstractSpawners;

namespace ExtendedSlugbase.Features.GameRelated;
public class StartingStomachObject() : GameFeature<AbstractObject>("start_stomach_item", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static AbstractObject Factory(JsonAny json)
	{
		return new(json);
	}

	internal static class Implementation
	{
		internal static void Player_Ctor(Player self)
		{
			if (!Custom.rainWorld.ExpeditionMode && self.room != null && self.room.game.IsStorySession && self.room.game.GetStorySession?.saveState?.cycleNumber == 0
				&& self.room.game.TryGetFeature(ExtGameFeatures.StartingStomachObject, out var abstractObject)
				&& abstractObject.TryGetObject(self.room.abstractRoom, new(), out var startObject))
			{
				self.objectInStomach = startObject;
			}
		}
	}
}
