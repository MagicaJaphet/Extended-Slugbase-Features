using ExtendedSlugbase.Extensions;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtendedSlugbase.Features;

public class ExtFeatureTypes
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
	public static TimelineFeature<int[]> TimelineInts(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => JsonUtils.ToInts(ExtJsonUtils.AssertLength(json, minLength, maxLength)));

	/// <summary>Create a timeline feature that takes an array of integers.</summary>
	public static TimelineFeature<long[]> TimelineLongs(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => JsonUtils.ToLongs(ExtJsonUtils.AssertLength(json, minLength, maxLength)));

	/// <summary>Create a timeline feature that takes an array of numbers.</summary>
	public static TimelineFeature<double[]> TimelineDoubles(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => JsonUtils.ToDoubles(ExtJsonUtils.AssertLength(json, minLength, maxLength)));

	/// <summary>Create a timeline feature that takes an array of numbers.</summary>
	public static TimelineFeature<float[]> TimelineFloats(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => JsonUtils.ToFloats(ExtJsonUtils.AssertLength(json, minLength, maxLength)));

	/// <summary>Create a timeline feature that takes an array of strings.</summary>
	public static TimelineFeature<string[]> TimelineStrings(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => JsonUtils.ToStrings(ExtJsonUtils.AssertLength(json, minLength, maxLength)));

	/// <summary>Create a timeline feature that takes an array of slugcat names.</summary>
	public static TimelineFeature<SlugcatStats.Name[]> TimelineSlugcatNames(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => JsonUtils.ToSlugcatNames(ExtJsonUtils.AssertLength(json, minLength, maxLength)));

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
		return new(id, json => JsonUtils.ToExtEnums<T>(ExtJsonUtils.AssertLength(json, minLength, maxLength)));
	}

	public static PlayerFeature<T> ObsoletePlayerFeature<T>(string id, string message = null)
	{
		return new(id, json => {
			ObsoleteFeature(id, json, message);
			return default;
		});
	}
	public static GameFeature<T> ObsoleteGameFeature<T>(string id, string message = null)
	{
		return new(id, json => {
			ObsoleteFeature(id, json, message);
			return default;
		});
	}
	public static TimelineFeature<T> ObsoleteTimelineFeature<T>(string id, string message = null)
	{
		return new(id, json => {
			ObsoleteFeature(id, json, message);
			return default;
		});
	}

	/// <summary>
	/// A method for throwing an inner Slugbase error referring to obsolete features.
	/// </summary>
	internal static void ObsoleteFeature(string id, JsonAny json, string message = null)
	{
		if (!string.IsNullOrEmpty(message))
		{
			throw new JsonException($"{id} {message}", json);

		}
		else
		{
			throw new JsonException($"{id} is no longer supported.", json);
		}
	}

	/// <summary>Create a player feature that takes an array of bools.</summary>
	public static PlayerFeature<bool[]> PlayerBools(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToBools(ExtJsonUtils.AssertLength(json, minLength, maxLength)));

	/// <summary>Create a game feature that takes an array of bools.</summary>
	public static GameFeature<bool[]> GameBools(string id, int minLength = 0, int maxLength = int.MaxValue) => new(id, json => ToBools(ExtJsonUtils.AssertLength(json, minLength, maxLength)));

	///<summary>Convert list to <see cref="bool"/>[].</summary>
	public static bool[] ToBools(JsonAny json) => json.TryBool() == null ? [.. from element in json.AsList() select JsonUtils.ToBool(element)] : [json.AsBool()];

	internal static Dictionary<SlugBaseCharacter.FeatureList, bool> throwDLCErrors = [];

	internal static readonly Feature<bool> ignoreDLCErrors = new("ignore_dlc_errors", json => { return json.AsBool(); });

	/// <summary>
	/// Declares if a feature requires a DLC to properly use, regardless of implementation.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class RequiresDLC : Attribute
	{
		internal bool needsMSC;
		internal bool needsWatcher;
		internal bool mutualExclusion;

		internal RequiresDLC(bool MSC = false, bool Watcher = false, bool mutuallyExclusive = true)
		{
			needsMSC = MSC;
			needsWatcher = Watcher;
			mutualExclusion = mutuallyExclusive;
		}

		public static bool DLCsEnabled(string featureID)
		{
			if (SlugbaseHelpers.RegisteredFeatures.TryGetValue(featureID, out var info) && info.dlc != null)
			{
				RequiresDLC dlc = info.dlc;
				bool needsMSC = dlc.needsMSC && !ModManager.MSC;
				bool needsWatcher = dlc.needsWatcher && !ModManager.Watcher;
				if (dlc.mutualExclusion && needsMSC && needsWatcher)
				{
					return false;
				}
				if (!dlc.mutualExclusion && (needsMSC || needsWatcher))
				{
					return false;
				}
			}
			return true;
		}
	}
}