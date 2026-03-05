using RWCustom;
using MagicaHookingLibrary.Helpers;
using SlugBase;
using static ExtendedSlugbase.Objects.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Diagnostics;
using static MagicaHookingLibrary.Helpers.ReflectionHelpers;
using static ExtendedSlugbase.Helpers.JsonHelpers;
using System.Collections;

namespace ExtendedSlugbase.Helpers;
public static class AbstractHelpers
{
	/// <summary>
	/// Taken from DevConsole, our valid types based on their string values.
	/// </summary>
	internal static readonly Dictionary<string, Type> typeMap = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Taken from DevConsole, our valid Constructors we can call
	/// </summary>
	internal static readonly Dictionary<Type, ConstructorInfo[]> typeCtors = [];

	/// <summary>
	/// Taken from DevConsole, the Regex matching we'll use for entityIDs
	/// </summary>
	internal static readonly Regex entityID = new(@"^ID\.-?\d+\.-?\d+(\.-?\d+)?$");

	/// <summary>
	/// Taken from DevConsole, returns our valid types that we need to spawn <see cref="AbstractPhysicalObject"/> types.
	/// </summary>
	internal static void ScanAbstractTypes()
	{
		scanned = true;
		foreach (var t in ScanTypes().Where(t =>
					typeof(AbstractPhysicalObject).IsAssignableFrom(t)
					&& !t.ContainsGenericParameters
					&& !t.IsAbstract))
		{
			typeMap[t.FullName] = t;
			typeMap[t.Name] = t;
			typeMap[t.Name.Replace("Abstract", "")] = t;
		}
	}

	/// <summary>
	/// Taken from DevConsole, returns valid Constructors for object spawning.
	/// </summary>
	internal static ConstructorInfo[] GetConstructors(Type type)
	{
		if (!typeCtors.TryGetValue(type, out ConstructorInfo[] ctors))
		{
			ctors = type.GetConstructors(anyFlag);
			Array.Sort(ctors, (a, b) =>
			{
				if (a.IsPublic != b.IsPublic) return a.IsPublic ? -1 : 1;

				return a.GetParameters().Length - b.GetParameters().Length;
			});

			typeCtors[type] = ctors;
		}

		return ctors;
	}

	/// <summary>
	/// Taken from DevConsole, returns a parsed <see cref="EntityID"/>.
	/// </summary>
	internal static EntityID ParseExtendedID(string id)
	{
		EntityID outID = EntityID.FromString(id);
		string[] split = id.Split('.');
		if (split.Length > 3 && int.TryParse(split[3], out int altSeed))
		{
			outID.setAltSeed(altSeed);
		}
		return outID;
	}

	internal static readonly Type[] typesOkToBeNull = [
		typeof(Room),
		typeof(AbstractRoom),
		typeof(World),
		typeof(WorldCoordinate),
		typeof(EntityID),
		typeof(PlacedObject.ConsumableObjectData)
	];

	internal static readonly Type[] typesOkToBeNullAssignableFrom = [
		typeof(PhysicalObject),
	];

    internal static bool scanned;

	public class AbstractObject
	{
		public ConstructorInfo Contructor { get; internal set; }
		public object[] ConstructorArguments { get; internal set; }
		public EntityID? AltID { get; internal set; }
		public Dictionary<FieldInfo, object> FieldArguments { get; internal set; }
		public Dictionary<MethodInfo, object> PropertyArguments { get; internal set; }


		public bool TryGetObject(AbstractRoom room, WorldCoordinate pos, out AbstractPhysicalObject obj)
		{
			obj = null;
			try
			{
				if (this.CreatePhysicalObject(room, pos) is AbstractPhysicalObject result)
				{
					obj = result;
					return result != null;
				}
			}
			catch (Exception ex)
			{
				Plugin.Logger?.LogError(ex);
			}
			return false;
		}

		public AbstractObject(JsonAny json)
		{
			var enumerator = json.AsObject().GetKeyPairEnumerator().Select(x => (x.key, x.value.AsObject()));
			
			foreach((string objType, JsonObject args) in enumerator)
			{
				if (!scanned) 
					ScanAbstractTypes();

				// Find our Abstract type
				if (!typeMap.TryGetValue(objType, out Type type))
				{
					try
					{
						type = Type.GetType(objType, true, true);
					}
					catch (Exception e)
					{
						Plugin.Logger?.LogError(e);
					}

					typeMap[objType] = type;
				}

				// Find constructors
				ConstructorInfo[] ctors = GetConstructors(type);

				if (ctors == null || ctors.Length == 0)
				{
					Plugin.Logger?.LogError($"No constructors were found for {type}");
					continue;
				}


				// Parse our arguments to the correct type to pass to the ctor
				foreach (ConstructorInfo ctor in ctors)
				{
					try
					{
						if (!CallConstructor(this, ctor, args, out var invalid))
						{
							Plugin.Logger?.LogInfo($"{ctor.Name} was not valid! Missing parameters: {string.Join(", ", invalid.Select(x => x.Name))}");
						}
						else
						{
							break;
						}
					}
					catch (Exception e)
					{
						Plugin.Logger?.LogError(e);
					}
				}
				break;
			}
		}
	}

	/// <summary>
	/// Heavily referenced from DevConsole, the spawning process that handles the actual Constructor.
	/// </summary>
	internal static bool CallConstructor(AbstractObject abstractObj, ConstructorInfo ctor, JsonObject argDict, out List<ParameterInfo> invalidParams)
	{
		invalidParams = [];
		var parameters = ctor.GetParameters();
		object[] finalArgs = new object[parameters.Length];

		List<string> argKeys = [.. argDict.GetKeys()];

		// Find Entity ID, if it's valid
		if (argKeys.Count > 0 && argKeys.Any(entityID.IsMatch))
		{
			int entityArg = argKeys.IndexOf(argKeys.First(x => entityID.IsMatch(x)));
			abstractObj.AltID = ParseExtendedID(argKeys[entityArg]);
			argKeys.RemoveAt(entityArg);
		}
		
		for (int outArgInd = 0; outArgInd < finalArgs.Length; outArgInd++)
		{
			var param = parameters[outArgInd];

			if (argKeys.Contains(param.Name))
			{
				var key = argKeys.First(x => x == param.Name);
				finalArgs[outArgInd] = ParseAny(argDict[key], param.ParameterType);
				argKeys.Remove(key);
			}

			if (finalArgs[outArgInd] == null && !typesOkToBeNull.Any(x => x == param.ParameterType) && !typesOkToBeNullAssignableFrom.Any(x => x.IsAssignableFrom(param.ParameterType)))
			{
				invalidParams.Add(param);
			}
		}
		if (invalidParams.Count == 0)
		{
			abstractObj.Contructor = ctor;
			abstractObj.ConstructorArguments = finalArgs;

			// if all is good, set any other fields
			if (argKeys.Count > 0)
			{
				abstractObj.FieldArguments = [];
				abstractObj.PropertyArguments = [];
				foreach (var key in argKeys)
				{
					bool skip = false;
					foreach (var field in ctor.ReflectedType.GetFields(anyFlag))
					{
						if (field?.Name == key)
						{
							abstractObj.FieldArguments.Add(field, ParseAny(argDict[key], field.FieldType));
							skip = true;
							break;
						}
					}
					if (!skip)
					{
						foreach (var property in ctor.ReflectedType.GetProperties(anyFlag))
						{
							if (property?.Name == key && property.GetSetMethod() is MethodInfo setMethod)
							{
								abstractObj.PropertyArguments.Add(setMethod, ParseAny(argDict[key], property.PropertyType));
								break;
							}
						}
					}
				}
			}
		}

		return invalidParams.Count == 0;
	}

	internal static AbstractPhysicalObject CreatePhysicalObject(this AbstractObject abstractObject, AbstractRoom room, WorldCoordinate pos)
	{
		var parameters = abstractObject.Contructor.GetParameters();
		var finalArgs = abstractObject.ConstructorArguments;
		var id = room.world.game.GetNewID();
		for (int outArgInd = 0; outArgInd < finalArgs.Length; outArgInd++)
		{
			var param = parameters[outArgInd];
			if (finalArgs[outArgInd] != null && !typesOkToBeNull.Contains(param.ParameterType)) continue;

			if (!TryFillAutoParam(param, room, pos, abstractObject.AltID ?? id, out finalArgs[outArgInd]) && !typesOkToBeNullAssignableFrom.Any(x => x.IsAssignableFrom(param.ParameterType)))
			{
				Plugin.Logger.LogInfo($"{param.ParameterType.Name} was not able to be auto filled!");
			}

			if (finalArgs[outArgInd] == null && !typeof(PhysicalObject).IsAssignableFrom(param.ParameterType))
			{
				return null;
			}
		}

		// All parameters were successfully converted
		// Try creating the object
		AbstractPhysicalObject result = null;
		try
		{
			result = (AbstractPhysicalObject)abstractObject.Contructor.Invoke(finalArgs);
		}
		catch (Exception ex)
		{
			Plugin.Logger?.LogError(ex);
		}

		if (result != null && abstractObject.FieldArguments != null)
		{
			foreach ((FieldInfo key, object value) in abstractObject.FieldArguments.Where(x => x.Key != null && x.Value != null).Select(x => (x.Key, x.Value)))
			{
				//LATER: dictonary values should replace values instead of overriding the entire dictionary
				//var obj = key.GetValue(result);
				//if (obj?.GetType() is Type type && typeof(IDictionary).IsAssignableFrom(type)) 
				//{
				//	// If value is not null, add to the dictionary

				//	var dictArgs = type.GetGenericArguments();
				//	var dictType = typeof(Dictionary<,>).MakeGenericType(dictArgs);
				//	var addMethod = dictType.GetMethod("Add", [dictArgs[0], dictArgs[1]]);

				//	var keys = dictType.GetField("Keys").GetValue(value);

				//	Dictionary<string, bool> a = [];
				//}
				//else
				//{

				//}

				try
				{
					key?.SetValue(result, value);
				}
				catch (Exception ex)
				{
					Plugin.Logger.LogError(ex);
				}
			}
		}
		if (abstractObject.PropertyArguments != null)
		{
			foreach ((MethodInfo key, object value) in abstractObject.PropertyArguments.Where(x => x.Key != null && x.Value != null).Select(x => (x.Key, x.Value)))
			{
				try
				{
					key?.Invoke(result, [value]);
				}
				catch (Exception ex)
				{
					Plugin.Logger.LogError(ex);
				}
			}
		}

		return result;
	}

	/// <summary>
	/// Taken from DevConsole, attempts to autofill any otherwise default values before passing it to our Constructor.
	/// </summary>
	internal static bool TryFillAutoParam(ParameterInfo info, AbstractRoom room, WorldCoordinate pos, EntityID id, out object value)
	{
		value = null;

		Type type = info.ParameterType;
		if (type == typeof(Room))
		{
			value = room?.realizedRoom;
		}
		else if (type == typeof(AbstractRoom))
		{
			value = room;
		}
		else if (type == typeof(World))
		{
			value = room?.world;
		}
		else if (type == typeof(WorldCoordinate))
		{
			value = pos;
		}
		else if (type == typeof(EntityID))
		{
			value = id;
		}
		else if (type == typeof(int))
		{
			value = -1;
		}

		return value != null;
	}
}
