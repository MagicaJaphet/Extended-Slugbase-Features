using ExtendedSlugbase.Helpers;
using SlugBase;
using SlugBase.DataTypes;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ExtendedSlugbase.Features;

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

public class FeatureTypesExt
{
	/// <summary>Create a timeline feature that takes one integer.</summary>
	public static TimelineFeature<int> TimelineInt(string id) => new(id, JsonUtils.ToInt);

	/// <summary>Create a timeline feature that takes one integer.</summary>
	public static TimelineFeature<long> TimelineLong(string id) => new(id, JsonUtils.ToLong);

	/// <summary>Create a timeline feature that takes one number.</summary>
	public static TimelineFeature<double> TimelineDouble(string id) => new(id, JsonUtils.ToDouble);

	/// <summary>Create a timeline feature that takes one number.</summary>
	public static TimelineFeature<float> TimelineFloat(string id) => new(id, JsonUtils.ToFloat);

	/// <summary>Create a timeline feature that takes one string.</summary>
	public static TimelineFeature<string> TimelineString(string id) => new(id, JsonUtils.ToString);

	/// <summary>Create a timeline feature that takes a slugcat name.</summary>
	public static TimelineFeature<SlugcatStats.Name> TimelineSlugcatName(string id) => new(id, JsonUtils.ToSlugcatName);

	/// <summary>Create a timeline feature that takes an array of integers.</summary>
	public static TimelineFeature<int[]> TimelineInts(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => JsonUtils.ToInts(JsonHelpers.AssertLength(json, minLength, maxLength)));

	/// <summary>Create a timeline feature that takes an array of integers.</summary>
	public static TimelineFeature<long[]> TimelineLongs(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => JsonUtils.ToLongs(JsonHelpers.AssertLength(json, minLength, maxLength)));

	/// <summary>Create a timeline feature that takes an array of numbers.</summary>
	public static TimelineFeature<double[]> TimelineDoubles(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => JsonUtils.ToDoubles(JsonHelpers.AssertLength(json, minLength, maxLength)));

	/// <summary>Create a timeline feature that takes an array of numbers.</summary>
	public static TimelineFeature<float[]> TimelineFloats(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => JsonUtils.ToFloats(JsonHelpers.AssertLength(json, minLength, maxLength)));

	/// <summary>Create a timeline feature that takes an array of strings.</summary>
	public static TimelineFeature<string[]> TimelineStrings(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => JsonUtils.ToStrings(JsonHelpers.AssertLength(json, minLength, maxLength)));

	/// <summary>Create a timeline feature that takes an array of slugcat names.</summary>
	public static TimelineFeature<SlugcatStats.Name[]> TimelineSlugcatNames(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => JsonUtils.ToSlugcatNames(JsonHelpers.AssertLength(json, minLength, maxLength)));

	/// <summary>Create a timeline feature that takes a color.</summary>
	public static TimelineFeature<Color> TimelineColor(string id) => new(id, JsonUtils.ToColor);

	/// <summary>Create a timeline feature that takes one boolean.</summary>
	public static TimelineFeature<bool> TimelineBool(string id) => new(id, JsonUtils.ToBool);

	/// <summary>Create a timeline feature that takes one enum value.</summary>
	public static TimelineFeature<T> TimelineEnum<T>(string id) where T : struct => new(id, JsonUtils.ToEnum<T>);

	/// <summary>Create a timeline feature that takes one enum value.</summary>
	public static TimelineFeature<T> TimelineExtEnum<T>(string id) where T : ExtEnum<T> => new(id, JsonUtils.ToExtEnum<T>);

	/// <summary>Create a timeline feature that takes an array of enum values.</summary>
	public static TimelineFeature<T[]> TimelineExtEnums<T>(string id, int minLength = 0, int maxLength = int.MaxValue)
		where T : ExtEnum<T>
	{
		return new(id, json => JsonUtils.ToExtEnums<T>(JsonHelpers.AssertLength(json, minLength, maxLength)));
	}
}

public class TimelineFeatures
{
	/// <summary>
	/// Controls whether the rain timer dots should show in this timeline.
	/// </summary>
	public static TimelineFeature<bool> showRainTimer = FeatureTypesExt.TimelineBool("show_rain_timer");

	//LATER: Implement
	/// <summary>
	/// Controls whether the shelter logic should close at the end of the cycle as long as the <see cref="Player"/>s are standing still.
	/// </summary>
	public static TimelineFeature<bool> endOfCycleForcesSheltering = FeatureTypesExt.TimelineBool("end_of_cycle_forces_sheltering");
}
