using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using MagicaHookingLibrary.Helpers;
using SlugBase;
using SlugBase.Features;
using System.Collections.Generic;
using System.Linq;

namespace ExtendedSlugbase.Features.GameRelated;
public class CreaturePlayerRelationOverrides() : GameFeature<CreaturePlayerRelationOverrides.CreatureRelationship>("creature_relationships", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static CreatureRelationship Factory(JsonAny json)
	{
		return new(json);
	}

	/// <summary>
	/// JSON Object to hold relationships.
	/// </summary>
	public class CreatureRelationship
	{
		public Dictionary<CreatureTemplate.Type, CreatureTemplate.Relationship> relationshipOverrides = [];

		public bool TryGetRelationship(CreatureTemplate.Type type, out CreatureTemplate.Relationship relationship)
		{
			if (relationshipOverrides.TryGetValue(type, out relationship))
			{
				return true;
			}
			return false;
		}

		public CreatureRelationship(JsonAny json)
		{
			foreach ((string key, JsonAny any) in json.AsObject().GetKeyPairEnumerator())
			{
				if (ExtEnumHelpers.TryGetExtEnum(key, out CreatureTemplate.Type crtType))
				{
					(string type, float intensity) = any.AsObject().Select(x => (x.Key, x.Value.AsFloat())).FirstOrDefault();
					if (ExtEnumHelpers.TryGetExtEnum(type, out CreatureTemplate.Relationship.Type relation))
					{
						relationshipOverrides.Add(crtType, new(relation, intensity));
					}
					else
					{
						throw new JsonException($"{key} is not a value of CreatureTemplate.Relationship.Type!", any);
					}
				}
				else
				{
					throw new JsonException($"{key} is not a value of CreatureTemplate.Type!", any);
				}
			}
		}
	}

	internal static class Implementation
	{
		/// <summary>
		/// Overrides relationships.
		/// </summary>
		internal static CreatureTemplate.Relationship CreatureTemplate_CreatureRelationship_Creature(On.CreatureTemplate.orig_CreatureRelationship_Creature orig, CreatureTemplate self, Creature crit)
		{
			if (crit is Player player && player.AI == null && crit.room?.game != null && crit.room.game.TryGetFeature(ExtGameFeatures.CreaturePlayerRelationOverrides, out var overrides) && overrides.TryGetRelationship(self.type, out var relationship))
			{
				return relationship;
			}
			return orig(self, crit);
		}

		/// <summary>
		/// Overrides template relationships.
		/// </summary>
		internal static CreatureTemplate.Relationship CreatureTemplate_CreatureRelationship_CreatureTemplate(On.CreatureTemplate.orig_CreatureRelationship_CreatureTemplate orig, CreatureTemplate self, CreatureTemplate crit)
		{
			if (crit.type == CreatureTemplate.Type.Slugcat && MiscHelpers.TryGetCurrentGame(out var game) && game.TryGetFeature(ExtGameFeatures.CreaturePlayerRelationOverrides, out var overrides) && overrides.TryGetRelationship(self.type, out var relationship))
			{
				return relationship;
			}
			return orig(self, crit);
		}
	}
}
