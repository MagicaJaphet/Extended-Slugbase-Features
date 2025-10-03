using System.Runtime.CompilerServices;
using UnityEngine;

namespace ExtendedSlugbaseFeatures.Resources
{
	public class CWTs
	{
		public static ConditionalWeakTable<AbstractSpear, SpearValues> spearCWT = new();

		/// <summary>
		/// Contains Spearmaster specific values that need to be assigned to an individual spear.
		/// </summary>
		public class SpearValues
		{
			public Color? slugColor;
		}
	}
}