using SlugBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbaseFeatures.Helpers;
internal class PlayerHelpers
{
	/// <summary>
	/// An object which holds information about which objects a <see cref="Player"/> can hold.
	/// </summary>
	public class Grabability
	{
		public Dictionary<AbstractPhysicalObject.AbstractObjectType, Player.ObjectGrabability> ObjectOverrides { get; } = [];
		public Dictionary<CreatureTemplate.Type, Player.ObjectGrabability> CreatureOverrides { get; } = [];

		internal Grabability(JsonAny json)
		{
			List<Player.ObjectGrabability> grabs = [];
			foreach (var grab in Enum.GetValues(typeof(Player.ObjectGrabability)))
			{
				grabs.Add((Player.ObjectGrabability)grab);
			}

			if (json.TryObject() is JsonObject obj)
			{
				foreach (var pair in obj)
				{
					if (pair.Value.TryString() is string str && Enum.TryParse(str, out Player.ObjectGrabability grabby))
					{
						ObjectOverrides[new AbstractPhysicalObject.AbstractObjectType(pair.Key)] = grabby;
						CreatureOverrides[new CreatureTemplate.Type(pair.Key)] = grabby;
					}
				}
			}
		}
	}
}
