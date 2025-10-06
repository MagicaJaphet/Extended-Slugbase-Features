using ExtendedSlugbaseFeatures.Helpers;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static ExtendedSlugbaseFeatures.Helpers.RoomSpecificScriptHelpers;
using static ExtendedSlugbaseFeatures.Helpers.RoomSpecificScriptHelpers.CustomCutscene;

namespace ExtendedSlugbaseFeatures.Resources;
internal class JsonResources
{
	/// <summary>
	/// Extension returning a new <see cref="GameFeatures"/> instance which allows for multiple <see cref="bool"/>.
	/// </summary>
	public static GameFeature<bool[]> GameBools(string id, int minLength = 0, int maxLength = int.MaxValue)
	{
		return new GameFeature<bool[]>(id, (json) => { return ToBools(FeatureTypes.AssertLength(json, minLength, maxLength)); });
	}

	/// <summary>
	/// Convertes a <see cref="JsonAny"/> object into a <see cref="bool"/> <see cref="Array"/>.
	/// </summary>
	public static bool[] ToBools(JsonAny json)
	{
		if (json.TryBool().HasValue)
		{
			return [json.AsBool()];
		}

		return [.. json.AsList().Select(JsonUtils.ToBool)];
	}
}

/// <summary>
/// A constant setting of a <see cref="SlugBaseCharacter"/>'s world state.
/// </summary>
/// <typeparam name="T">The type that stores this setting's information.</typeparam>
public class TimelineFeature<T> : Feature<T>
{
	/// <summary>
	/// Creates a new <see cref="TimelineFeature{T}"/> with the given <paramref name="id"/>.
	/// </summary>
	/// <param name="id">The JSON key.</param>
	/// <param name="factory">A delegate that parses <see cref="JsonAny"/> into <typeparamref name="T"/>. An exception should be thrown on failure.</param>
	public TimelineFeature(string id, Func<JsonAny, T> factory) : base(id, factory) { }

	/// <summary>
	/// Gets the <typeparamref name="T"/> instance assocated with <paramref name="game"/>.
	/// </summary>
	/// <param name="game">A <see cref="RainWorldGame"/> instance that may belong to a <see cref="SlugBaseCharacter"/>'s timeline with this <see cref="Feature"/>.</param>
	/// <param name="value">The stored setting, or <typeparamref name="T"/>'s default value if the feature wasn't found.</param>
	/// <returns><c>true</c> if the <paramref name="game"/>'s <see cref="SlugBaseCharacter"/> timeline point had this feature, <c>false</c> otherwise.</returns>
	public bool TryGet(RainWorldGame game, out T value)
	{
		if (SlugBaseCharacter.TryGet(new(game.TimelinePoint.value, false), out var slugCat))
			return TryGet(slugCat, out value);
		value = default;
		return false;
	}
}
