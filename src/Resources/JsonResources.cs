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
	/// Reflected version of <see cref="JsonUtils.AssertLength"/>. Original method is private.
	/// </summary>
	public static JsonAny AssertLength(JsonAny json, int minLength, int maxLength = int.MaxValue)
	{
		// This method is private in the SlugBase.dll :(
		if (typeof(FeatureTypes).GetMethod("AssertLength", BindingFlags.NonPublic | BindingFlags.Static).Invoke(json, [json, minLength, maxLength]) is JsonAny any)
		{
			return any;
		}
		return json;
	}

	/// <summary>
	/// Copy of Slugbase's <see cref="Utils.MatchCaseInsensitiveEnum{T}(string)"/> as the Utils class is internal.
	/// </summary>
	public static string MatchCaseInsensitiveEnum<T>(string name)
		where T : ExtEnum<T>
	{
		return ExtEnum<T>.values.entries.FirstOrDefault(value => value.Equals(name, StringComparison.InvariantCultureIgnoreCase)) ?? name;
	}

	/// <summary>
	/// Extension returning a new <see cref="GameFeatures"/> instance which allows for multiple <see cref="bool"/>.
	/// </summary>
	public static GameFeature<bool[]> GameBools(string id, int minLength = 0, int maxLength = int.MaxValue)
	{
		return new GameFeature<bool[]>(id, (json) => { return ToBools(AssertLength(json, minLength, maxLength)); });
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

	/// <summary>
	/// Reflected version of Slugbase's method IsMostRecent for Slugbase JSONs.
	/// </summary>
	internal static bool IsMostRecent<TKey, TValue>(JsonRegistry<TKey, TValue> registry, object[] value) where TKey : ExtEnum<TKey>
	{
		return typeof(JsonRegistry<TKey, TValue>).GetMethod("IsMostRecent", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(registry, value) is bool isMostRecent && isMostRecent;
	}
}
