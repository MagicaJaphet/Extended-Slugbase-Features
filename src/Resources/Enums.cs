using System;
using PlacedObjectType = PlacedObject.Type;

namespace ExtendedSlugbaseFeatures.Resources.Resources
{
	public class Enums
	{
		public static void Register()
		{
			ScriptTriggerBox = new(nameof(ScriptTriggerBox), true);
		}

		public static PlacedObjectType ScriptTriggerBox;
	}
}