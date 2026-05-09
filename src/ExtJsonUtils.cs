using SlugBase;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using System.Reflection;
using RWCustom;
using UnityEngine;
using System.Collections;

namespace ExtendedSlugbase;
public static class ExtJsonUtils
{
    /// <summary>
    /// Copied version of Slugbase's Feature.FeatureTypes.AssertLength method.
    /// </summary>
    public static JsonAny AssertLength(JsonAny json, int minLength, int maxLength)
    {
		if (json.TryList() is JsonList list)
		{
			AssertLength(list, minLength, maxLength);
		}
        return json;
    }

	/// <summary>
	/// Direct version of Slugbase's AssertLength.
	/// </summary>
	public static JsonList AssertLength(JsonList list, int minLength, int maxLength)
	{
		if (list.Count < minLength)
			throw new JsonException($"List must contain at least {minLength} elements!", list);

		if (list.Count > maxLength)
			throw new JsonException($"List may not contain more than {maxLength} elements!", list);
            
		return list;
	}

	/// <summary>
	/// Returns a <see cref="JsonObject"/> as a key value enumerator.
	/// </summary>
	public static IEnumerable<(string key, JsonAny value)> GetKeyPairEnumerator(this JsonObject obj)
	{
		return from o in obj select (o.Key, o.Value);
	}

	/// <summary>
	/// Returns only the keys from <see cref="GetKeyPairEnumerator(JsonObject)"/>.
	/// </summary>
	public static IEnumerable<string> GetKeys(this JsonObject obj)
	{
		return obj.GetKeyPairEnumerator().Select(i => i.key);
	}

	/// <summary>
	/// Returns only the values from <see cref="GetKeyPairEnumerator(JsonObject)"/>.
	/// </summary>
	public static IEnumerable<JsonAny> GetValues(this JsonObject obj)
	{
		return obj.GetKeyPairEnumerator().Select(i => i.value);
	}

	public static Color AsColor(this JsonAny json)
	{
		return JsonUtils.ToColor(json);
    }

	public static Color GetColor(this JsonObject json, string key)
	{
		return json.Get(key).AsColor();
	}

	/// <summary>
	/// Tries parsing the Enum value directly in the most generic way.
	/// </summary>
	/// <exception cref="JsonException"></exception>
	public static T AsEnum<T>(this JsonAny json)
	{
		if (Enum.Parse(typeof(T), json.AsString(), true) is T result)
		{
			return result;
		}

		throw new JsonException("\"" + json.AsString() + "\" was not a value of \"" + typeof(T).Name + "\"!", json);
    }

	/// <summary>
	/// Non-generic form of <see cref="AsEnum{T}(JsonAny)"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="json"></param>
	/// <returns></returns>
	/// <exception cref="JsonException"></exception>
	public static object AsEnum(this JsonAny json, Type type)
	{
		var result = Enum.Parse(type, json.AsString(), true);
		if (result != null && type.IsAssignableFrom(result.GetType()))
		{
			return result;
		}

		throw new JsonException("\"" + json.AsString() + "\" was not a value of \"" + type.Name + "\"!", json);
    }

	public static T GetEnum<T>(this JsonObject json, string key)
	{
		return json.Get(key).AsEnum<T>();
	}

	/// <summary>
	/// Tries parsing the ExtEnum value directly in the most generic way.
	/// </summary>
	/// <exception cref="JsonException"></exception>
	public static T AsExtEnum<T>(this JsonAny json)
	{
		if (ExtEnumBase.Parse(typeof(T), json.AsString(), true) is T result)
		{
			return result;
		}
		
		throw new JsonException("\"" + json.AsString() + "\" was not a value of \"" + typeof(T).Name + "\"!", json);
    }

	/// <summary>
	/// Non-generic form of <see cref="AsExtEnum{T}(JsonAny)"/>
	/// </summary>
	/// <exception cref="JsonException"></exception>
	public static object AsExtEnum(this JsonAny json, Type type)
	{
		var result = ExtEnumBase.Parse(type, json.AsString(), true);
		if (result != null && type.IsAssignableFrom(result.GetType()))
		{
			return result;
		}
		
		throw new JsonException("\"" + json.AsString() + "\" was not a value of \"" + type.Name + "\"!", json);
    }

	public static T GetExtEnum<T>(this JsonObject json, string key)
	{
		return json.Get(key).AsExtEnum<T>();
	}

	/// <summary>
	/// <see cref="JsonUtils.ToVector2(JsonAny)"/> for <see cref="IntVector2"/>s.
	/// </summary>
	/// <exception cref="JsonException"></exception>
	public static IntVector2 AsIntVector2(this JsonAny json)
	{
		switch (json.Type)
        {
            case JsonAny.Element.List:
                {
                    JsonList jsonList = json.AsList();
                    if (jsonList.Count != 2)
                    {
                        throw new JsonException("2D vector must contain 2 values!", json);
                    }

                    return new IntVector2(jsonList.GetInt(0), jsonList.GetInt(1));
                }
            case JsonAny.Element.Object:
                {
                    JsonObject jsonObject = json.AsObject();
                    return new IntVector2(jsonObject.GetInt("x"), jsonObject.GetInt("y"));
                }
            default:
                throw new JsonException("Invalid 2D intvector!", json);
        }
    }

	public static IntVector2 GetIntVector2(this JsonObject json, string key)
	{
		return json.Get(key).AsIntVector2();
	}
	
	public static Vector2 AsVector2(this JsonAny json)
	{
		return JsonUtils.ToVector2(json);
    }

	public static Vector2 GetVector2(this JsonObject json, string key)
	{
		return json.Get(key).AsVector2();
	}

	public static T[] ParseListItems<T>(this JsonList list, int minLength = 0, int maxLength = int.MaxValue, bool throwIfParseError = true)
	{
		IEnumerable<T> values = [];
		foreach (var item in list)
		{
			if (item.TryParse(out T result, minLength, maxLength, throwIfParseError))
			{
				values = values.Append(result);
			}
		}
		return [.. values];
	}

	/// <summary>
	/// Since constraints are limited in flexibility, use this to throw an invalid generic type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal class InvalidGenericTypeException<T>() : ArgumentException($"{typeof(T).Name} is not a valid type for this generic method!") {}


	/// <summary>
	/// Attempts to return the value of a <see cref="JsonObject"/> without throwing if the requested key isn't present.
	/// </summary>
	public static bool TryGet<T>(this JsonObject json, string key, out T value, int minLength = 0, int maxLength = int.MaxValue, bool throwIfParseError = true)
	{
		value = default;
		if (json.TryGet(key)?.TryParse(out value, minLength, maxLength, throwIfParseError) ?? false)
		{
			return true;
		}
		return false;
	}

	public static bool TryParse<T>(this JsonAny json, out T value, int minLength = 0, int maxLength = int.MaxValue, bool throwIfParseError = true)
	{
		value = default;
		try
		{
			if (ParseAny<T>(json, minLength, maxLength) is T result)
			{
				value = result;
				return true;
			}
			Plugin.Logger?.LogError($"Could not parse {json.Type} into {typeof(T)?.Name ?? "null"}!");
		}
		catch (JsonException jsonEx)
		{
			if (throwIfParseError)
				throw jsonEx;
		}
		return false;
	}
	
	public static object ParseAny<T>(JsonAny json, int minLength = 0, int maxLength = int.MaxValue)
	{
		if (typeof(T).IsArray)
		{
			var arrayElementType = typeof(T).GetElementType();
			if (json.TryParse(out JsonList list, throwIfParseError: false))
			{
				var objectArray = (from item in AssertLength(list, minLength, maxLength) select Convert.ChangeType(ParseAny(item, arrayElementType), arrayElementType)).ToArray();

				Array castedArray = Array.CreateInstance(arrayElementType, objectArray.Length);
				Array.Copy(objectArray, castedArray, objectArray.Length);

				return castedArray;
			}
			else if (json.Type != JsonAny.Element.Object)
			{
				var singleItem = ParseAny(json, arrayElementType);
				if (singleItem != null)
				{
					object[] objectArray = [singleItem];
					Array singleItemArray = Array.CreateInstance(arrayElementType, 1);
					Array.Copy(objectArray, singleItemArray, objectArray.Length);

					return singleItemArray;
				}
			}
		}

		return ParseAny(json, typeof(T));
	}

	public static object ParseAny(JsonAny json, Type type)
	{
		// Default
		if (type == typeof(JsonAny))
		{
			return json;
		}

		// Else, conversion time
		if (type == typeof(bool))
		{
			return json.AsBool();
		}
		else if (type == typeof(Color))
		{
			return json.AsColor();
		}
		else if (type == typeof(double))
		{
			return json.AsDouble();
		}
		else if (type.IsEnum)
		{
			return json.AsEnum(type);
		}
		else if (type.IsExtEnum())
		{
			return json.AsExtEnum(type);
		}
		else if (type == typeof(float))
		{
			return json.AsFloat();
		}
		else if (type == typeof(int))
		{
			return json.AsInt();
		}
		else if (type == typeof(IntVector2))
		{
			return json.AsIntVector2();
		}
		else if (type == typeof(JsonList))
		{
			return json.AsList();
		}
		else if (type == typeof(JsonObject))
		{
			return json.AsObject();
		}
		else if (type == typeof(long))
		{
			return json.AsLong();
		}
		else if (type == typeof(string))
		{
			return json.AsString();
		}
		else if (type == typeof(Vector2))
		{
			return json.AsVector2();
		}

		// Try parse dictionary values
		else if (typeof(IDictionary).IsAssignableFrom(type))
		{
			var obj = json.AsObject();

			var dictArgs = type.GetGenericArguments();
			var dictType = typeof(Dictionary<,>).MakeGenericType(dictArgs);
			var dict = Activator.CreateInstance(dictType);

			var addMethod = dictType.GetMethod("Add", [dictArgs[0], dictArgs[1]]);

			foreach ((string jsonKey, JsonAny jsonValue) in obj.GetKeyPairEnumerator())
			{
				var key = FromString(jsonKey, dictArgs[0]);
				var value = ParseAny(jsonValue, dictArgs[1]);
				if (key != null && value != null)
				{
					addMethod.Invoke(dict, [key, value]); // Add to our dictionary
				}
			}
			return dict;
		}

		// Finally pass to FromString
		else if (json.TryParse(out string str))
		{
			return FromString(str, type);
		}

		return null;
	}

	/// <summary>
    /// Taken from DevConsole, parses strings into a valid Type object.
    /// </summary>
    internal static object FromString(string text, Type toType)
	{
		// Try hardcoded, safe conversions
		if (text.Equals("null", StringComparison.OrdinalIgnoreCase) || text.Equals("default", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		else if (toType.IsEnum)
		{
			return Enum.Parse(toType, text, true);
		}
		else if (toType.IsExtEnum())
		{
			return ExtEnumBase.Parse(toType, text, true);
		}
		else if (toType == typeof(CreatureTemplate))
		{
			return StaticWorld.GetCreatureTemplate(WorldLoader.CreatureTypeFromString(text));
		}
		else if (toType == typeof(Color))
		{
			var namedColor = System.Drawing.Color.FromName(text);
			if (namedColor.IsKnownColor)
			{
				return new Color(namedColor.R, namedColor.G, namedColor.B, namedColor.A) / 255f;
			}

			var match = Regex.Match(text, "^#?([0-9a-fA-F]{6}(?:[0-9a-fA-F]{2})?)$");
			if (match.Success)
			{
				Color color = RXUtils.GetColorFromHex(match.Groups[1].Value);
				if (match.Groups[1].Value.Length == 6)
					color.a = 1f;
				return color;
			}

			throw new FormatException("Unknown color name or invalid hexadecimal color code!");
		}

		// Try finding a method called FromString
		var fromString = toType.GetMethod("FromString", BindingFlags.Static, null, [typeof(string)], null);
		if (fromString != null)
		{
			try
			{
				var res = fromString.Invoke(null, [text]);
				if (res != null && toType.IsAssignableFrom(res.GetType()))
					return res;
			}
			catch { }
		}

		// Default to conversion
		return Convert.ChangeType(text, toType);
	}
}
