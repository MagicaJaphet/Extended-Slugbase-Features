using ExtendedSlugbaseFeatures.Resources;
using RWCustom;
using SlugBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ExtendedSlugbaseFeatures.Helpers;
internal class AbstractPhysicalObjectHelpers
{
	/// <summary>
	/// Used to store <see cref="AbstractPhysicalObject"/> that should spawn upon a certain condition.
	/// </summary>
	public class AbstractData
	{
		/// <summary>
		/// Stores the <see cref="Type"/> that inherits <see cref="AbstractPhysicalObject"/>.
		/// </summary>
		public string ObjectType { get; }

		/// <summary>
		/// Stores any parameter information that should be passed to the <see cref="AbstractPhysicalObject"/> type.
		/// </summary>
		public Dictionary<string, object> ObjectParameters { get; } = [];

		/// <summary>
		/// Controls how the object will spawn in the room in relation to the Player.
		/// </summary>
		public enum SpawnType
		{
			Room,
			Stomach,
			Grasp
		}

		public SpawnType Spawn { get; }

		public IntVector2 SpawnPos { get; } = default;

		public bool spawned;

		public AbstractData(JsonObject json)
		{
			foreach (var obj in json)
			{
				ObjectType = obj.Key;

				if (obj.Value.TryObject() is JsonObject objInfo)
				{
					foreach (var parameter in objInfo)
					{
						// Parse information
						switch (parameter.Key)
						{
							case "spawn_pos":
								if (parameter.Value.TryList() is JsonList numbers && JsonResources.AssertLength(numbers, 2).TryList() is JsonList outNumbers && outNumbers[0].TryInt() is int x && outNumbers[1].TryInt() is int y)
								{
									SpawnPos = new(x, y);
								}
								break;

							case "spawn_type":
								if (parameter.Value.TryString() is string enumName && Enum.GetNames(typeof(SpawnType)).Contains(enumName) && Enum.TryParse(enumName, out SpawnType type))
								{
									Spawn = type;
								}
								break;

							default:
								if (parameter.Key != null)
								{
									ObjectParameters.Add(parameter.Key, parameter.Value);
								}
								break;
						}
					}
				}
			}
		}

		internal bool TrySpawn(AbstractRoom room, out AbstractPhysicalObject spawnObj)
		{
			spawnObj = GetAbstractPhysicalObjectsFromDict(room, ObjectType, ObjectParameters, new(room.index, SpawnPos.x, SpawnPos.y, -1));
			spawned = spawnObj != null;
			return spawned;
		}
	}

	/// <summary>
	/// Taken from DevConsole, our valid types based on their string values.
	/// </summary>
	private static readonly Dictionary<string, Type> typeMap = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Taken from DevConsole, our valid Constructors we can call
	/// </summary>
	private static readonly Dictionary<Type, ConstructorInfo[]> typeCtors = [];

	/// <summary>
	/// Taken from DevConsole, the Regex matching we'll use for entityIDs
	/// </summary>
	private static readonly Regex entityID = new(@"^ID\.-?\d+\.-?\d+(\.-?\d+)?$");

	/// <summary>
	/// Taken from DevConsole, a blacklist of Assemblies we should avoid referencing.
	/// </summary>
	internal static readonly HashSet<string> dllBlacklist = new()
			{
				"0Harmony",
				"0Harmony20",
				"Accessibility",
				"Assembly-CSharp-firstpass",
				"BepInEx.Harmony",
				"BepInEx.MonoMod.Loader",
				"BepInEx.MultiFolderLoader",
				"BepInEx.Preloader",
				"BepInEx",
				"Dragons.PublicDragon",
				"GalaxyCSharp",
				"GoKit",
				"HOOKS-Assembly-CSharp",
				"HarmonyXInterop",
				"Microsoft.Win32.Registry",
				"Mono.Cecil.Mdb",
				"Mono.Cecil.Pdb",
				"Mono.Cecil.Rocks",
				"Mono.Cecil",
				"Mono.Data.Sqlite",
				"Mono.Posix",
				"Mono.Security",
				"Mono.WebBrowser",
				"MonoMod.Common",
				"MonoMod.RuntimeDetour",
				"MonoMod.Utils",
				"MonoMod",
				"Newtonsoft.Json",
				"Novell.Directory.Ldap",
				"Purchasing.Common",
				"Rewired.Runtime",
				"Rewired_Core",
				"Rewired_Windows",
				"SonyNP",
				"SonyPS4CommonDialog",
				"SonyPS4SaveData",
				"SonyPS4SavedGames",
				"StovePCSDK.NET",
				"System.ComponentModel.Composition",
				"System.ComponentModel.DataAnnotations",
				"System.Configuration",
				"System.Core",
				"System.Data",
				"System.Design",
				"System.Diagnostics.StackTrace",
				"System.DirectoryServices",
				"System.Drawing.Design",
				"System.Drawing",
				"System.EnterpriseServices",
				"System.Globalization.Extensions",
				"System.IO.Compression.FileSystem",
				"System.IO.Compression",
				"System.Net.Http",
				"System.Numerics",
				"System.Runtime.Serialization.Formatters.Soap",
				"System.Runtime.Serialization.Xml",
				"System.Runtime.Serialization",
				"System.Runtime",
				"System.Security.AccessControl",
				"System.Security.Principal.Windows",
				"System.Security",
				"System.ServiceModel.Internals",
				"System.Transactions",
				"System.Web.ApplicationServices",
				"System.Web.Services",
				"System.Web",
				"System.Windows.Forms",
				"System.Xml.Linq",
				"System.Xml.XPath.XDocument",
				"System.Xml",
				"System",
				"Unity.Addressables",
				"Unity.Analytics.DataPrivacy",
				"Unity.Burst.Unsafe",
				"Unity.Burst",
				"Unity.Mathematics",
				"Unity.MemoryProfiler",
				"Unity.ResourceManager",
				"Unity.ScriptableBuildPipeline",
				"Unity.Services.Analytics",
				"Unity.Services.Core.Analytics",
				"Unity.Services.Core.Configuration",
				"Unity.Services.Core.Device",
				"Unity.Services.Core.Environments.Internal",
				"Unity.Services.Core.Environments",
				"Unity.Services.Core.Internal",
				"Unity.Services.Core.Networking",
				"Unity.Services.Core.Registration",
				"Unity.Services.Core.Scheduler",
				"Unity.Services.Core.Telemetry",
				"Unity.Services.Core.Threading",
				"Unity.Services.Core",
				"Unity.TextMeshPro",
				"Unity.Timeline",
				"UnityEngine.AIModule",
				"UnityEngine.ARModule",
				"UnityEngine.AccessibilityModule",
				"UnityEngine.Advertisements",
				"UnityEngine.AndroidJNIModule",
				"UnityEngine.AnimationModule",
				"UnityEngine.AssetBundleModule",
				"UnityEngine.AudioModule",
				"UnityEngine.ClothModule",
				"UnityEngine.ClusterInputModule",
				"UnityEngine.ClusterRendererModule",
				"UnityEngine.CoreModule",
				"UnityEngine.CrashReportingModule",
				"UnityEngine.DSPGraphModule",
				"UnityEngine.DirectorModule",
				"UnityEngine.GIModule",
				"UnityEngine.GameCenterModule",
				"UnityEngine.GridModule",
				"UnityEngine.HotReloadModule",
				"UnityEngine.IMGUIModule",
				"UnityEngine.ImageConversionModule",
				"UnityEngine.InputLegacyModule",
				"UnityEngine.InputModule",
				"UnityEngine.JSONSerializeModule",
				"UnityEngine.LocalizationModule",
				"UnityEngine.Monetization",
				"UnityEngine.ParticleSystemModule",
				"UnityEngine.PerformanceReportingModule",
				"UnityEngine.Physics2DModule",
				"UnityEngine.PhysicsModule",
				"UnityEngine.ProfilerModule",
				"UnityEngine.Purchasing.AppleCore",
				"UnityEngine.Purchasing.AppleMacosStub",
				"UnityEngine.Purchasing.AppleStub",
				"UnityEngine.Purchasing.Codeless",
				"UnityEngine.Purchasing.SecurityCore",
				"UnityEngine.Purchasing.SecurityStub",
				"UnityEngine.Purchasing.Stores",
				"UnityEngine.Purchasing.WinRTCore",
				"UnityEngine.Purchasing.WinRTStub",
				"UnityEngine.Purchasing",
				"UnityEngine.RuntimeInitializeOnLoadManagerInitializerModule",
				"UnityEngine.ScreenCaptureModule",
				"UnityEngine.SharedInternalsModule",
				"UnityEngine.SpatialTracking",
				"UnityEngine.SpriteMaskModule",
				"UnityEngine.SpriteShapeModule",
				"UnityEngine.StreamingModule",
				"UnityEngine.SubstanceModule",
				"UnityEngine.SubsystemsModule",
				"UnityEngine.TLSModule",
				"UnityEngine.TerrainModule",
				"UnityEngine.TerrainPhysicsModule",
				"UnityEngine.TextCoreModule",
				"UnityEngine.TextRenderingModule",
				"UnityEngine.TilemapModule",
				"UnityEngine.UI",
				"UnityEngine.UIElementsModule",
				"UnityEngine.UIElementsNativeModule",
				"UnityEngine.UIModule",
				"UnityEngine.UNETModule",
				"UnityEngine.UmbraModule",
				"UnityEngine.UnityAnalyticsCommonModule",
				"UnityEngine.UnityAnalyticsModule",
				"UnityEngine.UnityConnectModule",
				"UnityEngine.UnityCurlModule",
				"UnityEngine.UnityTestProtocolModule",
				"UnityEngine.UnityWebRequestAssetBundleModule",
				"UnityEngine.UnityWebRequestAudioModule",
				"UnityEngine.UnityWebRequestModule",
				"UnityEngine.UnityWebRequestTextureModule",
				"UnityEngine.UnityWebRequestWWWModule",
				"UnityEngine.VFXModule",
				"UnityEngine.VRModule",
				"UnityEngine.VehiclesModule",
				"UnityEngine.VideoModule",
				"UnityEngine.VirtualTexturingModule",
				"UnityEngine.WindModule",
				"UnityEngine.XR.LegacyInputHelpers",
				"UnityEngine.XRModule",
				"UnityEngine",
				"UnityPlayer",
				"com.rlabrecque.steamworks.net",
				"mscorlib",
				"netstandard",
			};
	private static bool scanned = false;

	/// <summary>
	/// Taken from DevConsole, scans all of the avaliable Assembllies and excludes our blacklisted ones.
	/// </summary>
	/// <returns></returns>
	private static IEnumerable<Assembly> GetScanAssemblies()
	{
		return AppDomain.CurrentDomain.GetAssemblies().Where(asm => !dllBlacklist.Contains(asm.GetName().Name));
	}

	/// <summary>
	/// Taken from DevConsole, returns our valid types that we need to spawn <see cref="AbstractPhysicalObject"/> types.
	/// </summary>
	private static void ScanTypes()
	{
		scanned = true;

		foreach (var asm in GetScanAssemblies())
		{
			Type[] types = null;
			try
			{
				types = asm.GetTypes();
			}
			catch (ReflectionTypeLoadException e)
			{
				types = e.Types;
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogException(e);
			}

			if (types != null)
			{
				foreach (var t in types.Where(t =>
					typeof(AbstractPhysicalObject).IsAssignableFrom(t)
					&& !t.ContainsGenericParameters
					&& !t.IsAbstract))
				{
					typeMap[t.FullName] = t;
					typeMap[t.Name] = t;
					typeMap[t.Name.Replace("Abstract", "")] = t;
				}
			}
		}
	}

	/// <summary>
	/// Taken from DevConsole, returns valid Constructors for object spawning.
	/// </summary>
	private static ConstructorInfo[] GetConstructors(Type type)
	{
		if (type != null && !typeCtors.TryGetValue(type, out ConstructorInfo[] ctors))
		{
			ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
			Array.Sort(ctors, (a, b) =>
			{
				if (a.IsPublic != b.IsPublic) return a.IsPublic ? -1 : 1;

				return a.GetParameters().Length - b.GetParameters().Length;
			});
			typeCtors[type] = ctors;

			return ctors;
		}

		return null;
	}

	/// <summary>
	/// Taken from DevConsole, returns a parsed <see cref="EntityID"/>.
	/// </summary>
	private static EntityID ParseExtendedID(string id)
	{
		EntityID outID = EntityID.FromString(id);
		string[] split = id.Split('.');
		if (split.Length > 3 && int.TryParse(split[3], out int altSeed))
		{
			outID.setAltSeed(altSeed);
		}
		return outID;
	}

	/// <summary>
	/// Taken from DevConsole, tells us our valid parsing type.
	/// </summary>
	private static readonly Type[] fromStringTypes = [typeof(string)];

	/// <summary>
	/// Taken from DevConsole, parses strings into a valid Type object.
	/// </summary>
	private static object FromString(string text, Type toType)
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

		// Try finding a method called FromString
		var fromString = toType.GetMethod("FromString", BindingFlags.Static, null, fromStringTypes, null);
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

	/// <summary>
	/// Attempts to parse a <see cref="JsonAny"/> into a valid object.
	/// </summary>
	public static object ParseJsonAny(object jsonAny, Type toType)
	{
		// Try hardcoded, safe conversions
		if (jsonAny is not JsonAny || jsonAny == null || jsonAny == default)
		{
			return null;
		}
		else if (jsonAny is JsonAny any)
		{
			if (any.TryBool() != null)
			{
				return any.AsBool();
			}
			else if (any.TryFloat() != null && toType == typeof(float))
			{
				return any.AsFloat();
			}
			else if (any.TryInt() != null && toType == typeof(int))
			{
				return any.AsInt();
			}
			else if (any.TryDouble() != null && toType == typeof(double))
			{
				return any.AsDouble();
			}
			else if (any.TryLong() != null && toType == typeof(long))
			{
				return any.AsLong();
			}
			else if (any.TryList() != null || any.TryObject() != null)
			{
				throw new JsonException("Object field cannot be a JSON list or object!", any);
			}
			// Finally pass to FromString
			else if (any.TryString() != null)
			{
				return FromString(any.AsString(), toType);
			}
		}

		return false;
	}

	/// <summary>
	/// Heavily referenced from DevConsole, allows dynamic spawning of various Abstract types.
	/// </summary>
	public static AbstractPhysicalObject CreateAbstractObject(string objType, Dictionary<string, object> args, AbstractRoom room, WorldCoordinate pos)
	{
		if (!scanned)
			ScanTypes();

		// Find our Abstract type
		if (!typeMap.TryGetValue(objType, out Type type))
		{
			try
			{
				type = Type.GetType(objType, true, true);
			}
			catch (Exception e)
			{
				Plugin.Logger.LogError(e);
			}

			typeMap[objType] = type;
		}

		// Find constructors
		ConstructorInfo[] ctors = GetConstructors(type);

		var argList = args.Keys.ToList();

		// Find Entity ID, if it's valid
		EntityID id = room.world.game.GetNewID();
		if (argList.Count > 0 && argList.Any(entityID.IsMatch))
		{
			int entityArg = argList.IndexOf(argList.Where(x => entityID.IsMatch(x)).First());
			id = ParseExtendedID(argList[entityArg]);
			argList.RemoveAt(entityArg);
		}

		if (ctors.Length == 0)
		{
			Plugin.Logger.LogError($"No constructors were found for {type}");
			return null;
		}

		// Parse our arguments to the correct type to pass to the ctor
		foreach (ConstructorInfo ctor in ctors)
		{
			try
			{
				return CallConstructor(ctor, args, room, pos, id);
			}
			catch (Exception e)
			{
				Plugin.Logger.LogError(e);
			}
		}
		return null;
	}

	/// <summary>
	/// Heavily referenced from DevConsole, the spawning process that handles the actual Constructor.
	/// </summary>
	private static AbstractPhysicalObject CallConstructor(ConstructorInfo ctor, Dictionary<string, object> argDict, AbstractRoom room, WorldCoordinate pos, EntityID id)
	{
		var parameters = ctor.GetParameters();
		object[] finalArgs = new object[parameters.Length];

		List<string> argKeys = [.. argDict.Keys];
		int inArgs = 0;
		for (int outArgInd = 0; outArgInd < finalArgs.Length; outArgInd++)
		{
			var param = parameters[outArgInd];

			if (!TryFillAutoParam(param, room, pos, id, out finalArgs[outArgInd]))
			{
				if (argKeys.Contains(param.Name))
				{
					finalArgs[outArgInd] = ParseJsonAny(argDict[argKeys.Where(x => x == param.Name).First()], param.ParameterType);
				}
			}
		}

		// All parameters were successfully converted
		// Try creating the object
		AbstractPhysicalObject result = (AbstractPhysicalObject)ctor.Invoke(finalArgs);
		foreach (var field in argDict.Keys)
		{
			try
			{
				TryParseProperty(result, field, argDict[field]);
			}
			catch (Exception e)
			{
				Plugin.Logger.LogError($"Could not identify field! {field} {e}");
			}
		}
		return result;
	}

	/// <summary>
	/// Attempts to parse fields not assigned during the Constructor call.
	/// </summary>
	private static void TryParseProperty(AbstractPhysicalObject result, string property, object value)
	{
		if (result != null && result.GetType().GetField(property) != null)
		{
			FieldInfo field = result.GetType().GetField(property);
			field.SetValue(result, ParseJsonAny(value, field.FieldType));
		}
	}

	/// <summary>
	/// Taken from DevConsole, attempts to autofill any otherwise default values before passing it to our Constructor.
	/// </summary>
	private static bool TryFillAutoParam(ParameterInfo info, AbstractRoom room, WorldCoordinate pos, EntityID id, out object value)
	{
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
		else if (typeof(PhysicalObject).IsAssignableFrom(type))
		{
			value = null;
		}
		else
		{
			value = null;
			return false;
		}

		return value != null;
	}

	/// <summary>
	/// Parses JSON into the actual object parameters.
	/// </summary>
	internal static void ParseObjectsIntoList(JsonObject objList, out List<Dictionary<string, Dictionary<string, object>>> objectList)
	{
		objectList = [];
		foreach (var item in objList)
		{
			// EXPECTED EXAMPLE: { "AbstractDataPearl": { "dataPearlType": "CC" }}
			if (item.Value.TryObject() != null && JSONtoAbstractObjectParameters(item.Value.AsObject(), item.Key, out var dict))
			{
				Dictionary<string, Dictionary<string, object>> obj = [];
				obj.Add(item.Key, dict);
				objectList.Add(obj);
			}
			else
			{
				throw new JsonException("Unable to parse item list into valid object!", item.Value);
			}
		}
	}

	/// <summary>
	/// Processes potential object properties from the <see cref="SlugBaseCharacter"/> JSON.
	/// </summary>
	public static bool JSONtoAbstractObjectParameters(JsonObject obj, string key, out Dictionary<string, object> arguments)
	{
		arguments = [];

		if (!scanned)
			ScanTypes();

		if (!string.IsNullOrEmpty(key))
		{
			// Find our Abstract type
			if (!typeMap.TryGetValue(key, out Type type))
			{
				try
				{
					type = Type.GetType(key, true, true);
				}
				catch
				{
					throw new JsonException($"Could not find Type {key}!", obj);
				}

				typeMap[key] = type;
			}

			// { "AbstractDataPearl": { "dataPearlType": "CC" }}
			Plugin.Logger.LogMessage($"{obj}");
			if (type != null)
			{
				foreach (var field in type.GetFields())
				{
					if (obj.Any(x => x.Key == field.Name))
					{
						arguments.Add(field.Name, obj[obj.Where(x => x.Key == field.Name).First().Key]);
					}
				}
				return true;
			}
		}
		else
		{
			throw new JsonException("Object type cannot be null!", obj);
		}
		return false;
	}

	/// <summary>
	/// Returns actual <see cref="AbstractPhysicalObject"/>s from the JSON dictionary.
	/// </summary>
	internal static AbstractPhysicalObject GetAbstractPhysicalObjectsFromDict(AbstractRoom room, string type, Dictionary<string, object> parameters, WorldCoordinate pos = default, int maxLength = int.MaxValue)
	{
		var potentialObj = CreateAbstractObject(type, parameters, room, pos);
		UnityEngine.Debug.Log($"potential obj is {(potentialObj == null ? "NULL" : potentialObj.GetType())}");
		return potentialObj;
	}
}
