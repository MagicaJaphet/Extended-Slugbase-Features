using SlugBase.DataTypes;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ExtendedSlugbase
{
	// Taken from LudoCrypt
	/// <summary>
	/// Creates a new CWT class from a template.
	/// </summary>
	/// <typeparam name="T">The type to use as a weak reference.</typeparam>
	/// <typeparam name="C">The type to return from the <see cref="ConditionalWeakTable{TKey, TValue}"/> with <see cref="T"/> as a key.</typeparam>
	public abstract class ExtraDataClass<T, C> where T : class where C : class
	{
		private static readonly ConditionalWeakTable<T, C> weakData = new();

		public static C GetData(T obj)
		{
			return weakData.GetOrCreateValue(obj);
		}

		public static bool TryGetData(T obj, out C value)
		{
			return weakData.TryGetValue(obj, out value);
		}
	}

	public class CWTs
    {
        public class SpearCWT : ExtraDataClass<AbstractSpear, SpearCWT>
        {
            public ColorSlot generatedSpearColor;
            public ColorSlot generatedSpearFadeColor;

			public int? playerNumber;
		}

        public class PlayerCWT : ExtraDataClass<Player, PlayerCWT>
        {
            public int saintTongueSprite;
        }
    }
}
