using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using SlugBase;
using SlugBase.DataTypes;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Diagnostics;

namespace ExtendedSlugbaseFeatures
{
	public class Resources
	{
		public class ExtFeatures
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
			/// Copy of Slugbase's <see cref="SlugBase.Utils.MatchCaseInsensitiveEnum{T}(string)"/> as the Utils class is internal.
			/// </summary>
			public static string MatchCaseInsensitiveEnum<T>(string name)
				where T : ExtEnum<T>
			{
				return ExtEnum<T>.values.entries.FirstOrDefault(value => value.Equals(name, System.StringComparison.InvariantCultureIgnoreCase)) ?? name;
			}

			/// <summary>
			/// Extension returning a new <see cref="GameFeatures"/> instance which allows for multiple <see cref="bool"/>.
			/// </summary>
			public static GameFeature<bool[]> GameBools(string id, int minLength = 0, int maxLength = int.MaxValue)
			{
				return new GameFeature<bool[]>(id, (JsonAny json) => { return ToBools(AssertLength(json, minLength, maxLength)); });
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

			#region UNIMPLEMENTED
			/// <summary>
			/// Forces <see cref="SlugBaseCharacter"/> to use Hunter's illness mechanic, meaning the character dies past <see cref="int"/> number of cycles. An extra <see cref="int"/> can be set for the maximum number of <see cref="MMF.cfgHunterBonusCycles"/> one recieves from Five Pebbles.
			/// </summary>
			public static readonly GameFeature<int[]> cycleLimit = FeatureTypes.GameInts("max_cycle_limit", 1, 2);

			/// <summary>
			/// Allows <see cref="SlugBaseCharacter"/> to use Saint's tongue mechanic.
			/// </summary>
			public static readonly PlayerFeature<bool> saintTongue = FeatureTypes.PlayerBool("has_tongue");
			#endregion

			/// <summary>
			/// If a valid <see cref="AbstractPhysicalObject.AbstractObjectType"/> exists, overrides the <see cref="Player.Grabability(PhysicalObject)"/> for that type.
			/// </summary>
			public static readonly PlayerFeature<Dictionary<AbstractPhysicalObject.AbstractObjectType, Player.ObjectGrabability>> objectGrabability = new("grab_overrides", (json) =>
			{
				var obj = json.AsObject();
				var grabs = new Dictionary<AbstractPhysicalObject.AbstractObjectType, Player.ObjectGrabability>();
				List<Player.ObjectGrabability> validTypes = [
						Player.ObjectGrabability.BigOneHand,
						Player.ObjectGrabability.CantGrab,
						Player.ObjectGrabability.Drag,
						Player.ObjectGrabability.OneHand,
						Player.ObjectGrabability.TwoHands
					];
				foreach (var pair in obj)
				{
					var type = new AbstractPhysicalObject.AbstractObjectType(MatchCaseInsensitiveEnum<AbstractPhysicalObject.AbstractObjectType>(pair.Key));
					if (pair.Value.AsString() is string grab && validTypes.Select(x => x.ToString().ToLowerInvariant()).Contains(grab.ToLowerInvariant()))
						grabs[type] = validTypes.Where(x => x.ToString().ToLowerInvariant() == grab.ToLowerInvariant()).First();
					else
						throw new JsonException("Grabability is not a valid type!", pair.Value);
				}
				return grabs;
			});

			/// <summary>
			/// Changes the default <see cref="Player.DeathByBiteMultiplier"/> value, with the second value altering the difficulty.
			/// </summary>
			public static readonly PlayerFeature<float[]> deathByBiteMultiplier = FeatureTypes.PlayerFloats("bite_lethality_mutliplier", 1, 2);

			/// <summary>
			/// Allows <see cref="SlugBaseCharacter"/> to automatically pop <see cref="WaterNut"/> when held.
			/// </summary>
			public static readonly PlayerFeature<bool> popBubbleFruit = FeatureTypes.PlayerBool("pop_held_bubblefruit");

			/// <summary>
			/// Forces <see cref="SlugBaseCharacter"/> to be unable to use <see cref="Spear"/>s, instead tossing them.
			/// </summary>
			public static readonly PlayerFeature<bool> tossSpears = FeatureTypes.PlayerBool("only_tosses_spears");

			/// <summary>
			/// Forces <see cref="SlugBaseCharacter"/> to be able to grab <see cref="Spear"/>s in their embedded state.
			/// </summary>
			public static readonly PlayerFeature<bool> pullSpearsFromWalls = FeatureTypes.PlayerBool("take_spears_from_wall");

			/// <summary>
			/// Allows <see cref="SlugBaseCharacter"/> to use Artificer's explosive jump ability, setting the soft and hard limits in <see cref="int"/>s.
			/// </summary>
			public static readonly PlayerFeature<int[]> explosiveJumpLimits = FeatureTypes.PlayerInts("explosive_jump", 1, 2);

			// Player Features section
			/// <summary>
			/// Allows <see cref="SlugBaseCharacter"/> to visually have Rivulet's gills. The default <see cref="int"/> of <see cref="PlayerGraphics.gills"/> rows is 3.
			/// </summary>
			public static readonly PlayerFeature<int> numOfRivGills = FeatureTypes.PlayerInt("gill_rows");

			/// <summary>
			/// Allows <see cref="SlugBaseCharacter"/> to spawn <see cref="Spear"/>s from <see cref="PlayerGraphics.tailSpecks"/>. The default <see cref="int"/> of <see cref="PlayerGraphics.tailSpecks"/> is 3 and 5.
			/// </summary>
			public static readonly PlayerFeature<int[]> rowsAndColumnsSpearSpecks = FeatureTypes.PlayerInts("spear_specks", 2);

			/// <summary>
			/// Replaces the default face sprite with Artificer's variant. <see cref="usesSaintFaceCondition"/> takes priority over this due to how <see cref="PlayerGraphics.SaintFaceCondition"/> runs.
			/// </summary>
			public static readonly PlayerFeature<bool> hasArtiFace = FeatureTypes.PlayerBool("arti_eyes");

			/// <summary>
			/// When the <see cref="Color.black"/> is used in a <see cref="SlugBase.DataTypes.ColorSlot"/>, it will automatically attempt to replace it with <see cref="RoomPalette.blackColor"/>. This tells <see cref="SlugBase"/> how much that new color should blend with Watcher's blueish hue.
			/// </summary>
			public static readonly PlayerFeature<float> watcherBlackLerpAmount = FeatureTypes.PlayerFloat("watcher_blue");

			/// <summary>
			/// Replaces the <see cref="PlayerGraphics"/> head sprite with Saint's fluffier variant.
			/// </summary>
			public static readonly PlayerFeature<bool> hasSaintHead = FeatureTypes.PlayerBool("saint_fluff");

			/// <summary>
			/// Allows <see cref="SlugBaseCharacter"/> to use <see cref="Player.FoodInStomach"/> to craft explosives, and the <see cref="int"/> cost in quarter intervals.
			/// </summary>
			public static readonly PlayerFeature<int> explosiveCraftCost = FeatureTypes.PlayerInt("craft_explosives_cost");

			/// <summary>
			/// Allows <see cref="SlugBaseCharacter"/> to gain <see cref="Player.Karma"/> from holding a <see cref="Scavenger"/> corpse.
			/// </summary>
			public static readonly PlayerFeature<bool> getKarmaFromScavs = FeatureTypes.PlayerBool("get_karma_from_scavs");

			/// <summary>
			/// Allows <see cref="SlugBaseCharacter"/> to hold two <see cref="Spear"/>s.
			/// </summary>
			public static readonly PlayerFeature<bool> canDualWield = FeatureTypes.PlayerBool("can_dualwield");

			/// <summary>
			/// Applies <see cref="PlayerGraphics.SaintFaceCondition"/> if true, forcing the face sprite to flip when it uses open and closed eyes.
			/// </summary>
			public static readonly PlayerFeature<bool> usesSaintFaceCondition = FeatureTypes.PlayerBool("saint_eyes");

			/// <summary>
			/// Allows <see cref="SlugBaseCharacter"/> to inflict damage when hitting a <see cref="Creature"/> with momentum.
			/// </summary>
			public static readonly PlayerFeature<bool> canSlam = FeatureTypes.PlayerBool("can_slam");

			/// <summary>
			/// Disallows <see cref="SlugBaseCharacter"/> from swallowing or regurgitating objects if true.
			/// </summary>
			public static readonly PlayerFeature<bool> cantSwallowObjects = FeatureTypes.PlayerBool("cant_swallow_objects");

			/// <summary>
			/// Forces <see cref="SlugBaseCharacter"/> to use self-made <see cref="Spear"/>s to feed.
			/// </summary>
			public static readonly PlayerFeature<bool> forceFeedingFromSpears = FeatureTypes.PlayerBool("feeds_from_spears");

			// Game Features section
			/// <summary>
			/// Allows <see cref="SlugBaseCharacter"/> to pass OE's gate, with an extra condition to check if Gourmand has been beaten if desired.
			/// </summary>
			public static readonly GameFeature<bool[]> openOEGate = GameBools("can_pass_OE_gate", 1, 2);

			/// <summary>
			/// Returns the <see cref="int"/> of slugpups requested into <see cref="StoryGameSession.slugPupMaxCount"/>.
			/// </summary>
			public static readonly GameFeature<int> maxSlugpupSpawns = FeatureTypes.GameInt("max_slugpup_spawns");

			/// <summary>
			/// Allows <see cref="SlugBaseCharacter"/> to choose whether they voluntarily spawn <see cref="KarmaFlower"/>.
			/// </summary>
			public static readonly GameFeature<bool> shouldSpawnKarmaFlowers = FeatureTypes.GameBool("spawn_karma_flowers");

			/// <summary>
			/// Allows <see cref="SlugBaseCharacter"/> to speak to <see cref="Oracle"/> and <see cref="Ghost"/> without the mark, as well as being able to see <see cref="VoidSpawn"/> without the mark.
			/// </summary>
			public static readonly GameFeature<bool> enlightenedState = FeatureTypes.GameBool("enlightened");

			/// <summary>
			/// Allows <see cref="SlugBaseCharacter"/> to use <see cref="Player.InitChatLog"/> if the <see cref="CollectToken.whiteToken"/> object exists in their <see cref="RegionState"/>.
			/// </summary>
			public static readonly GameFeature<bool> canProcessWhiteTokens = FeatureTypes.GameBool("can_access_whitetokens");

			/// <summary>
			/// If the <see cref="SlugBaseCharacter"/> has the mark, forces the mark sprite to Lerp it's alpha from 0 to 1 based on <see cref="SaveState.cycleNumber"/>.
			/// </summary>
			public static readonly GameFeature<int> revealMarkOverTotalCycles = FeatureTypes.GameInt("reveal_mark_overtime");

			/// <summary>
			/// Returns the starting position of the <see cref="Player"/> in room tiles based on the room name, if it exists.
			/// </summary>
			public static readonly GameFeature<Dictionary<string, IntVector2>> possibleSpawnPositons = new("start_position", json => 
			{
				Dictionary<string, IntVector2> startingPositions = [];

				foreach (var room in json.AsObject())
				{
					if (!startingPositions.ContainsKey(room.Key))
					{
						string roomName = room.Key;
						JsonList roomPosition = json.AsObject()[roomName].AsList();
						IntVector2 tilePositon = new();
						for (int i = 0; i < roomPosition.Count; i++)
						{
							if (roomPosition[i] is JsonAny any && any.TryInt() is int position)
							{
								if (i == 0)
									tilePositon.x = position;
								else
									tilePositon.y = position;
							}
						}

						startingPositions.Add(roomName, tilePositon);
					}
					else
					{
						throw new JsonException("Room name key already exists!", room.Value);
					}
				}

				return startingPositions;
			});

			/// <summary>
			/// Returns a dictionary of a <see cref="AbstractPhysicalObject"/> the <see cref="SlugBaseCharacter"/> starts their campaign with. Only allows one object to spawn, as per the typical stomach limit.
			/// </summary>
			public static readonly GameFeature<Dictionary<string, Dictionary<string, object>>> spawnStomachObject = new("start_stomach_item", json =>
			{
				Dictionary<string, Dictionary<string, object>> stomachObject = [];

				JsonObject objectJSON = json.AsObject();
				foreach (var item in objectJSON)
				{
					// EXPECTED EXAMPLE: "AbstractDataPearl": { "dataPearlType": "CC" }
					if (Miscellaneous.JSONtoAbstractObjectParameters(item.Value.AsObject(), item.Key, out var dict))
					{
						if (item.Key != null && dict != null && !stomachObject.ContainsKey(item.Key))
						{
							Plugin.Logger.LogInfo($"Added object to {nameof(Player.objectInStomach)}! {item.Key} : Items {dict.Count}");
							stomachObject.Add(item.Key, dict);
							break;
						}
					}
					else
					{
						throw new JsonException("Unable to parse item list into valid object!", item.Value);
					}
				}

				return stomachObject;
			});

			/// <summary>
			/// Parses <see cref="AbstractPhysicalObject"/> the <see cref="Player"/> starts with in their hands, along with any inputs that should be passed to <see cref="Player.InputPackage"/>.
			/// </summary>
			public static readonly GameFeature<Dictionary<string, Dictionary<Type, object>>> introCutsceneDict = new("intro_cutscene", json =>
			{
				Dictionary<string, Dictionary<Type, object>> roomObjects = [];

				JsonObject rooms = json.AsObject();
				foreach (var room in rooms)
				{
					// Get if any dictionary is a starting room
					if (!roomObjects.ContainsKey(room.Key))
					{
						Dictionary<Type, object> roomContents = [];
						JsonObject components = rooms[room.Key].AsObject();
						// Search key for more object values
						foreach (var component in components)
						{
							if (component.Key == "inputs")
							{
								JsonList inputDict = component.Value.AsList();
								List<Player.InputPackage> introCutsceneInputs = [];
								List<int> introCutsceneTimedInputs = [];

								foreach (var inputGroup in inputDict)
								{
									Player.InputPackage input = new(false, Options.ControlSetup.Preset.None, 0, 0, false, false, false, false, false);
									JsonObject inputCanidates = inputGroup.AsObject();
									int? repeat = null;
									int time = 1;
									foreach (var canidate in inputCanidates)
									{
										switch (canidate.Key)
										{
											case "repeat":
												repeat = canidate.Value.TryInt() ?? 0;
												break;

											case "time":
												time = canidate.Value.TryInt() ?? 1;
												break;

											case nameof(Player.InputPackage.x):
												input.x = canidate.Value.TryInt() ?? 0;
												break;

											case nameof(Player.InputPackage.y):
												input.y = canidate.Value.TryInt() ?? 0;
												break;

											case "jump" or nameof(Player.InputPackage.jmp):
												input.jmp = canidate.Value.TryBool() ?? false;
												break;

											case "throw" or nameof(Player.InputPackage.thrw):
												input.thrw = canidate.Value.TryBool() ?? false;
												break;

											case "grab" or nameof(Player.InputPackage.pckp):
												input.pckp = canidate.Value.TryBool() ?? false;
												break;

											case "map" or nameof(Player.InputPackage.mp):
												input.mp = canidate.Value.TryBool() ?? false;
												break;

											case "crouch" or nameof(Player.InputPackage.crouchToggle):
												input.crouchToggle = canidate.Value.TryBool() ?? false;
												break;
										}
									}

									// Allows slugcat to roll if holding down and a direction
									if (input.x != 0 && input.y == -1)
									{
										input.downDiagonal = input.x;
									}

									UnityEngine.Debug.Log($"{input.x} {input.y}");

									introCutsceneInputs.Add(input);
									introCutsceneTimedInputs.Add(time);
									if (repeat != null)
									{
										for (int i = 1; i < repeat.Value; i++)
										{
											introCutsceneInputs.Add(input);
											introCutsceneTimedInputs.Add(time);
										}
									}
								}

								if (introCutsceneInputs.Count > 0)
								{
									UnityEngine.Debug.Log("Found inputs!");
									roomContents.Add(typeof(Player.InputPackage), introCutsceneInputs);
								}
								if (introCutsceneTimedInputs.Count > 0)
								{
									roomContents.Add(typeof(int), introCutsceneTimedInputs);
								}
							}
							// "player_grasps": [ ]
							else if (component.Key == "player_grasps")
							{
								JsonObject objectDict = component.Value.AsObject();
								List<Dictionary<string, Dictionary<string, object>>> introCutsceneObjects = [];

								Miscellaneous.ParseObjectsIntoList(objectDict, out introCutsceneObjects);

								if (introCutsceneObjects.Count > 0)
								{
									UnityEngine.Debug.Log("Found objects!");
									roomContents.Add(typeof(AbstractPhysicalObject), introCutsceneObjects);
								}
							}
							else if (component.Key == "food")
							{
								if (component.Value.TryFloat() is float food)
								{
									roomContents.Add(typeof(float), food);
								}
								else
								{
									throw new JsonException("Food value is not a float!", component.Value);
								}
							}
							else
							{
								throw new JsonException("Unable to parse object, as it is not a supported cutscene object.", component.Value);
							}
						}

						roomObjects.Add(room.Key, roomContents);
					}
				}

				return roomObjects;
			});
		}

		public class Miscellaneous
		{
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
			private static readonly HashSet<string> dllBlacklist = new()
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
				if (!typeCtors.TryGetValue(type, out ConstructorInfo[] ctors))
				{
					ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
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
			private static object ParseJsonAny(object jsonAny, Type toType)
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
			internal static List<AbstractPhysicalObject> GetAbstractPhysicalObjectsFromDict(AbstractRoom room, Dictionary<string, Dictionary<string, object>> objectDict, WorldCoordinate pos = default, int maxLength = int.MaxValue)
			{
				List<AbstractPhysicalObject> objectCanidates = [];
				if (pos == default)
					pos = new(room.index, 0, 0, -1);

				if (objectDict.Keys.Count > 0)
				{
					foreach (var key in objectDict.Keys)
					{
						if (objectDict[key] != null)
						{
							var potentialObj = CreateAbstractObject(key, objectDict[key], room, pos);
							UnityEngine.Debug.Log($"potential obj is {(potentialObj == null ? "NULL" : potentialObj.GetType())}");
							if (potentialObj != null)
							{
								objectCanidates.Add(potentialObj);

								if (objectCanidates.Count == maxLength)
									break;
							}
						}
					}
				}
				return objectCanidates;
			}
		}

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

		public class Scripts
		{
			public class MovementScript : UpdatableAndDeletable
			{
				private int timer;
				private Player player;
				private int inputIndex;
				private List<Player.InputPackage> cutsceneInputs;
				private List<AbstractPhysicalObject> playerGrasps;
				private List<int> cutsceneTimings;
				private bool shouldParseTimings;
				private bool shouldParseInputs;
				private bool shouldParseObjects;
				private float startFood;

				/// <summary>
				/// A script to parse movements for in-game cutscenes.
				/// </summary>
				/// <param name="roomDict">A <see cref="Dictionary{TKey, TValue}"/> tied to various objects used within the room script. Supports <see cref="Player.InputPackage"/>, <see cref="int"/> for timing purposes, and <see cref="AbstractPhysicalObject"/> for cutscene objects.</param>
				public MovementScript(Room room, Dictionary<Type, object> roomDict)
				{
					Plugin.Logger.LogMessage("MovementScript has been initalized!");
					this.room = room;
					inputIndex = 0;
					timer = 0;

					foreach (Type type in roomDict.Keys)
					{
						if (type == typeof(Player.InputPackage))
						{
							shouldParseInputs = true;
							object inputCanidates = roomDict[type];
							if (inputCanidates != null && inputCanidates is List<Player.InputPackage> inputs)
							{
								cutsceneInputs = inputs;
							}
						}
						else if (type == typeof(int))
						{
							shouldParseTimings = true;
							object timingCanidates = roomDict[type];
							if (timingCanidates != null && timingCanidates is List<int> timings)
							{
								cutsceneTimings = timings;
							}
						}
						else if (type == typeof(AbstractPhysicalObject))
						{
							shouldParseObjects = true;
							object objectCanidates = roomDict[type];
							if (objectCanidates != null && objectCanidates is List<Dictionary<string, Dictionary<string, object>>> objectDict && objectDict.Count > 0)
							{
								playerGrasps = [];
								foreach (var obj in objectDict)
								{
									playerGrasps.AddRange(Miscellaneous.GetAbstractPhysicalObjectsFromDict(room.abstractRoom, obj));
								}
							}
						}
						else if (type == typeof(float))
						{
							if (roomDict[type] is float food)
							{
								startFood = food;
							}
						}
					}
				}

				public override void Update(bool eu)
				{
					base.Update(eu);

					if ((!shouldParseTimings || cutsceneTimings != null) && (!shouldParseObjects || playerGrasps != null) && (!shouldParseInputs || cutsceneInputs != null))
					{
						if (player == null)
						{
							player = room.PlayersInRoom.FirstOrDefault();
							if (player != null)
							{
								if (startFood != 0)
								{
									int quarters = Mathf.RoundToInt(startFood * 4f);
									int foodPips = 0;
									for (; quarters > 4; quarters -= 4)
										foodPips++;

									player.playerState.foodInStomach = foodPips;
									player.playerState.quarterFoodPoints = quarters;
								}

								if (cutsceneInputs != null)
								{
									player.controller = new MoveController(this);
									UnityEngine.Debug.Log("Found player and set controller to movement inputs!");
								}

								if (playerGrasps != null)
								{
									foreach (var obj in playerGrasps)
									{
										if (obj != null && obj.type != null)
										{
											room.abstractRoom.AddEntity(obj);
											obj.RealizeInRoom();
											if (player.FreeHand() != -1 && obj.realizedObject != null)
											{
												obj.realizedObject.firstChunk.pos = player.firstChunk.pos;
												player.SlugcatGrab(obj.realizedObject, player.FreeHand());
											}
										}
									}
									UnityEngine.Debug.Log("Found player and set spawn objects!");
								}
							}
						}

						if (player != null && player.controller is MoveController && cutsceneInputs != null)
						{
							if (inputIndex < cutsceneInputs.Count)
							{
								timer++;
								if (timer >= cutsceneTimings[inputIndex])
								{
									timer = 0;
									inputIndex++;
								}
							}
							else
							{
								Destroy();
							}
						}
					}
				}

				public override void Destroy()
				{
					base.Destroy();
					cutsceneInputs = null;
					cutsceneTimings = null;
					player.controller = null;
				}

				internal Player.InputPackage GetInput()
				{
					if (inputIndex < cutsceneInputs.Count)
						return cutsceneInputs[inputIndex];

					return default;
				}

				internal class MoveController : Player.PlayerController
				{
					private MovementScript owner;

					public MoveController(MovementScript owner)
					{
						this.owner = owner;
					}

					public override Player.InputPackage GetInput()
					{
						return owner.GetInput();
					}
				}
			}
		}
	}

	internal static class ExtensionMethods
	{
		// Player related extensions
		/// <summary>
		/// Adds <paramref name="food"/> into the <see cref="Player"/>'s food meter. Returns true if <paramref name="food"/> is more than 0.
		/// </summary>
		/// <returns></returns>
		internal static bool ProcessFood(this Player player, float food)
		{
			int quarterPips = Mathf.RoundToInt(food * 4f);

			for (; quarterPips >= 4; quarterPips -= 4)
				player.AddFood(1);

			for (; quarterPips >= 1; quarterPips--)
				player.AddQuarterFood();

			return food > 0f;
		}

		internal static bool UnprocessFood(this Player player, float food)
		{
			int quarterPips = Mathf.RoundToInt(food * 4f);

			for (; quarterPips >= 4; quarterPips -= 4)
				player.SubtractFood(1);

			for (; quarterPips >= 1; quarterPips--)
				player.SubtractQuarterFood();

			return food > 0f;
		}

		internal static void SubtractQuarterFood(this Player player)
		{
			if (player.redsIllness != null)
			{
				player.redsIllness.AddQuarterFood();
			}
			else if (player.FoodInStomach < player.MaxFoodInStomach)
			{
				player.playerState.quarterFoodPoints--;
				if (ModManager.CoopAvailable && player.abstractCreature.world.game.IsStorySession && player.abstractCreature.world.game.Players[0] != player.abstractCreature && !player.isNPC)
				{
					PlayerState obj = player.abstractCreature.world.game.Players[0].state as PlayerState;
					obj.quarterFoodPoints--;
				}

				if (player.playerState.quarterFoodPoints < 0)
				{
					player.SubtractFood(1);
					player.playerState.quarterFoodPoints = 3;
				}
			}
		}

		/// <summary>
		/// Attempts to move the <see cref="ILCursor"/> after the next instance of <see cref="SlugcatStats.Name"/>. Returns true if successful.
		/// </summary>
		internal static bool MoveToNextSlugcat(this ILCursor cursor, FieldInfo info, [CallerMemberName] string method = "")
		{
			try
			{
				Func<Instruction, bool> isSlugcat = info.IsStatic ?
					(x => x.MatchLdsfld(info)) :
					x => x.MatchLdfld(info);

				if (cursor.TryGotoNext(MoveType.After,
					isSlugcat,
					x => x.MatchCallOrCallvirt(out _)))
				{
					cursor.MoveAfterLabels();
					return true;
				}
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogException(ex);
			}
			Plugin.Logger.LogError($"IL HOOK MATCH FAILED AT: {method}");
			return false;
		}

		// Auto inserts our IL based on the type of delegate we use
		internal static void ImplementILCodeAssumingLdarg0(this ILCursor cursor, Delegate implementation)
		{
			cursor.Emit(OpCodes.Ldarg_0);
			cursor.EmitDelegate(implementation);
		}

		/// <summary>
		/// Explicit check for if a <see cref="Feature"/>'s return value is not default by our standards.
		/// </summary>
		/// <returns></returns>
		internal static bool HasFeature<T>(this Player player, PlayerFeature<T> feature, bool shouldReturnTrue = true)
		{
			if (player.HasFeature(feature, out var value))
			{
				if (value is bool boolValue)
				{
					return (shouldReturnTrue && boolValue) || (!shouldReturnTrue && !boolValue);
				}
				else if (value is bool[] boolValues)
				{
					return (shouldReturnTrue && boolValues.Any(x => x)) || (!shouldReturnTrue && !boolValues.Any(x => x));
				}
				else if (value is int intValue)
				{
					return (shouldReturnTrue && intValue > -1) || (!shouldReturnTrue && intValue < 0);
				}
				else if (value is int[] intValues)
				{
					return (shouldReturnTrue && intValues.Any(x => x > -1)) || (!shouldReturnTrue && !intValues.Any(x => x > -1));
				}
				else if (value is float floatValue)
				{
					return (shouldReturnTrue && floatValue > -1) || (shouldReturnTrue && floatValue < 0);
				}
				else
				{
					return (shouldReturnTrue && value != null) || (!shouldReturnTrue && value == null);
				}
			}
			return false;
		}
		/// <summary>
		/// Explicit check for if a <see cref="Feature"/>'s return value is not default by our standards.
		/// </summary>
		/// <returns></returns>
		internal static bool HasFeature<T>(this RainWorldGame game, GameFeature<T> feature, bool shouldReturnTrue = true)
		{
			if (game.HasFeature(feature, out var value))
			{
				if (value is bool boolValue)
				{
					return (shouldReturnTrue && boolValue) || (!shouldReturnTrue && !boolValue);
				}
				else if (value is bool[] boolValues)
				{
					return (shouldReturnTrue && boolValues.Any(x => x)) || (!shouldReturnTrue && !boolValues.Any(x => x));
				}
				else if (value is int intValue)
				{
					return (shouldReturnTrue && intValue > -1) || (!shouldReturnTrue && intValue < 0);
				}
				else if (value is int[] intValues)
				{
					return (shouldReturnTrue && intValues.Any(x => x > -1)) || (!shouldReturnTrue && !intValues.Any(x => x > -1));
				}
				else if (value is float floatValue)
				{
					return (shouldReturnTrue && floatValue > -1) || (shouldReturnTrue && floatValue < 0);
				}
				else
				{
					return (shouldReturnTrue && value != null) || (!shouldReturnTrue && value == null);
				}
			}
			return false;
		}

		/// <summary>
		/// Default shorthand for <see cref="PlayerFeature{T}.TryGet(Player, out T)"/>.
		/// </summary>
		/// <returns></returns>
		internal static bool HasFeature<T>(this Player player, PlayerFeature<T> feature, out T value)
		{
			return feature.TryGet(player, out value);
		}
		/// <summary>
		/// Default shorthand for <see cref="GameFeature{T}.TryGet(RainWorldGame, out T)"/>.
		/// </summary>
		/// <returns></returns>
		internal static bool HasFeature<T>(this RainWorldGame game, GameFeature<T> feature, out T value)
		{
			return feature.TryGet(game, out value);
		}
	}
}