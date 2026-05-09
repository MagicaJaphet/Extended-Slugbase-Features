using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using MagicaHookingLibrary.Helpers;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features.PlayerRelated;
public class ObjectGrabOverrides() : PlayerFeature<ObjectGrabOverrides.Grabability>("grab_overrides", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static Grabability Factory(JsonAny json) => new(json);

	/// <summary>
	/// JSON Object which holds information about which objects a slugcat can hold.
	/// </summary>
	public class Grabability
	{
		public Dictionary<AbstractPhysicalObject.AbstractObjectType, Player.ObjectGrabability> ObjectOverrides { get; } = [];
		public Dictionary<CreatureTemplate.Type, Player.ObjectGrabability> CreatureOverrides { get; } = [];

		internal Grabability(JsonAny json)
		{
			foreach ((string key, JsonAny value) in json.AsObject().GetKeyPairEnumerator())
			{
				var grabby = value.AsEnum<Player.ObjectGrabability>();
				bool foundValue = false;
				if (ExtEnumHelpers.TryGetExtEnum<AbstractPhysicalObject.AbstractObjectType>(key, out var objType))
				{
					ObjectOverrides[objType] = grabby;
					foundValue = true;
				}
				if (ExtEnumHelpers.TryGetExtEnum<CreatureTemplate.Type>(key, out var type))
				{
					CreatureOverrides[type] = grabby;
					foundValue = true;
				}

				if (!foundValue)
				{
					throw new JsonException($"{key} is not a valid AbstractObjectType or CreatureTemplate.Type!", value);
				}
			}
		}
	}

	internal static class Implementation
	{
		internal static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
		{
			if (self.TryGetFeature(ExtPlayerFeatures.ObjectGrabOverrides, out var grabability))
			{
				if (obj is Creature creature && grabability.CreatureOverrides.TryGetValue(creature.Template.type, out var creatureGrab))
					return creatureGrab;
				if (grabability.ObjectOverrides.TryGetValue(obj.abstractPhysicalObject.type, out var abstractGrab))
					return abstractGrab;
			}
			return orig(self, obj);
		}
	}
}
