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

namespace ExtendedSlugbase.Features.GameRelated;
public class OverseerColorOverrides() : GameFeature<Dictionary<int, Color>>("overseer_overwrite", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static Dictionary<int, Color> Factory(JsonAny json)
	{
		Dictionary<int, Color> overseerColors = [];

		if (json.TryParse(out JsonObject[] objs))
		{
			foreach (var obj in objs)
			{
				overseerColors[obj.GetInt("owner")] = obj.GetColor("color");
			}
		}

		return overseerColors;
	}

	internal static class Implementation
	{
		/// <summary>
		/// Overrides existing overseer colors if they exist for the slugcat.
		/// </summary>
		internal static Color OverseerGraphics_MainColor(Func<OverseerGraphics, Color> orig, OverseerGraphics self)
		{
			if (!self.overseer.SafariOverseer && !self.overseer.SandboxOverseer
			&& self.overseer.abstractCreature.world.game.TryGetFeature(ExtGameFeatures.OverseerColorOverrides, out var overrides)
			&& overrides.TryGetValue((self.overseer.abstractCreature.abstractAI as OverseerAbstractAI).ownerIterator, out var overrideColor))
			{
				return overrideColor;
			}

			return orig(self);
		}
	}
}
