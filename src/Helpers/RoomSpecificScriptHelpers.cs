using ExtendedSlugbaseFeatures.Hooks;
using ExtendedSlugbaseFeatures.Resources;
using RWCustom;
using SlugBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static ExtendedSlugbaseFeatures.Helpers.AbstractPhysicalObjectHelpers;

namespace ExtendedSlugbaseFeatures.Helpers;
internal class RoomSpecificScriptHelpers
{
	/// <summary>
	/// Complete hashset of registered <see cref="AbstractRoom.name"/> which are used for scripts. Used to prevent duplicate scripts in a room for one <see cref="SlugBaseCharacter"/>.
	/// </summary>
	public static Dictionary<SlugcatStats.Name, HashSet<string>> AllTriggerRooms { get; } = [];

	/// <summary>
	/// General list of rooms which contain a valid script.
	/// </summary>
	public static List<string> ScriptTriggerRooms { get; } = [];

	internal static void ScanFiles()
	{
		CustomCutscene.Registry.WatchForChanges = true;
		CustomCutscene.Registry.ScanDirectory("slugbase/cutscenes");
	}

	/// <summary>
	/// Used to store custom in-game cutscene data.
	/// </summary>
	public class CustomCutscene
	{
		internal bool triggered;

		static CustomCutscene()
		{
			Registry.LoadFailed += (registry, args) =>
			{
				Action retry = null;
				if (args.Path != null)
					retry = () => SlugBaseCharacter.Registry.TryAddFromFile(args.Path);

				Plugin.Logger.LogError(args.Exception);
			};
			Registry.EntryReloaded += (_, args) =>
			{
				Plugin.Logger.LogInfo($"Attempting to reload {args.Key}");
				if (Registry.TryGet(args.Key, out var entry))
				{
					entry = args.Value;
					if (ResourceHooks.inputDevUI != null && Custom.rainWorld?.processManager?.currentMainLoop is RainWorldGame game)
					{
						ResourceHooks.inputDevUI.inputHistory?.Destroy();
						ResourceHooks.inputDevUI.inputHistory = new(entry, game, game.FirstAlivePlayer.Room.name);
					}
				}
			};
		}

		public class CutsceneID : ExtEnum<CutsceneID>
		{
			public CutsceneID(string value, bool register = false) : base(value, register) { }
		}

		public enum CutsceneType
		{
			Intro,
			Trigger,
			Ending
		}

		public CutsceneType Type { get; } = CutsceneType.Trigger;

		public static JsonRegistry<CutsceneID, CustomCutscene> Registry { get; } = new((key, json) => new(key, json));

		public CutsceneID ID { get; }

		// We'll worry about Jolly Coop support later
		public Dictionary<string, Dictionary<int, List<Player.InputPackage>>> Inputs { get; } = [];
		public Dictionary<string, Dictionary<int, List<int>>> InputTimers { get; } = [];

		public List<string> TriggerRooms { get; } = [];

		public Dictionary<string, Dictionary<int, List<AbstractData>>> SpawnObjects { get; } = [];

		public Dictionary<int, Vector2> PlayerInitialTriggerPos { get; } = [];

		//public Dictionary<string, Dictionary<int, Dictionary<string, object>>> PlayerValues { get; } = [];

		public float ScriptDuration { get; }

		public CustomCutscene(CutsceneID id, JsonObject json)
		{
			ID = id;

			if (json.TryGet("type")?.TryString() is string type && Enum.TryParse(type, out CutsceneType cutsceneType))
			{
				Plugin.Logger.LogInfo($"{id.value} type set to {cutsceneType}");
				Type = cutsceneType;
			}

			try
			{
				if (json.TryGet("script")?.TryObject() is JsonObject script)
				{
					Plugin.Logger.LogInfo($"{id.value} script found!");
					foreach (var room in script)
					{
						string roomName = room.Key;
						TriggerRooms.Add(roomName);
						ScriptTriggerRooms.Add(roomName);
						Plugin.Logger.LogInfo($"{id.value} {roomName} added to TriggerRooms!");

						if (room.Value.TryObject() is JsonObject roomObject)
						{
							/* IDEA LIST :
							 * Debug tool for inputs and testing creature IDs
							 * Ability to play sounds / create effects remotely
							 * Control room effects / settings
							 * HUD messages
							 * Room warp (may require two scripts)
							 * Control Shelter / Gate animations
							 */

							if (roomObject.TryGet("duration")?.TryFloat() is float duration)
							{
								ScriptDuration = duration;
							}

							if (roomObject.TryGet("inputs")?.TryList() is JsonList playerList)
							{
								Plugin.Logger.LogInfo($"{id.value} found inputs!");
								Dictionary<int, List<Player.InputPackage>> inputDict = [];
								Dictionary<int, List<int>> timerDict = [];
								//Dictionary<int, Dictionary<string, object>> playerValues = [];

								Dictionary<int, List<AbstractData>> objectDict = [];
								foreach (var player in playerList)
								{
									if (player.TryObject() is JsonObject playerObject)
									{
										if (playerObject.TryGet("player_number")?.TryInt() is int playerNumber)
										{
											//Dictionary<string, object> playerValue = [];
											//foreach (var key in playerObject)
											//{
											//	switch (key.Key)
											//	{
											//		case "player_number":
											//		case "inputs":
											//		case "objects":
											//			break;

											//		default:
											//			if (typeof(Player).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(x => x.Name == key.Key) && Miscellaneous.ParseJsonAny(key.Value, typeof(Player).GetField(key.Key).FieldType) is object obj)
											//			{
											//				Plugin.Logger.LogInfo($"Adding {key.Key} to player values");
											//				playerValue.Add(key.Key, obj);
											//			}
											//			else
											//			{
											//				throw new JsonException($"{key.Key} is not a valid Player field!", key.Value);
											//			}
											//			break;
											//	}
											//}

											//if (playerValue.Count > 0)
											//{
											//	playerValues.Add(playerNumber, playerValue);
											//}

											// Now we have a player number to tie the inputs to, interpret the inputs first
											List<Player.InputPackage> inputs = [];
											List<int> timers = [];

											if (playerObject.TryGet("inputs")?.TryList() is JsonList inputList)
											{
												int time = 1;
												foreach (var fullInput in inputList)
												{
													Player.InputPackage input = default;
													if (fullInput.TryObject() is JsonObject inputObject)
													{
														// Interpret inputs here

														object boxedInput = input;
														foreach (var field in typeof(Player.InputPackage).GetFields(BindingFlags.Public | BindingFlags.Instance))
														{
															if (field.FieldType != typeof(Vector2) && inputObject.TryGet(field.Name) is JsonAny any)
															{
																var parsedValue = ParseJsonAny(any, field.FieldType);
																if (parsedValue != null)
																{
																	field.SetValue(boxedInput, parsedValue);
																}
															}
															else if (field.FieldType == typeof(Vector2) && inputObject.TryGet(field.Name + ".x") is JsonAny x && inputObject.TryGet(field.Name + ".y") is JsonAny y)
															{
																var parsedx = ParseJsonAny(x, typeof(float));
																var parsedy = ParseJsonAny(y, typeof(float));
																if (parsedx is float fx && parsedy is float fy)
																{
																	field.SetValue(boxedInput, new Vector2(fx, fy));
																}
															}
														}
														input = (Player.InputPackage)boxedInput;

														if (inputObject.TryGet("timer")?.TryInt() is int timer)
														{
															time += timer;
														}
														timers.Add(time);
														inputs.Add(input);
													}
												}
											}

											if (inputs.Count > 0)
											{
												inputDict.Add(playerNumber, inputs);
												if (timers.Count > 0)
												{
													timerDict.Add(playerNumber, timers);
												}
											}

											if (playerObject.TryGet("objects")?.TryList() is JsonList objectList)
											{
												List<AbstractData> objects = [];
												foreach (var obj in objectList)
												{
													if (obj.TryObject() is JsonObject jsonObj)
														objects.Add(new(jsonObj));
												}

												if (objects.Count > 0)
												{
													objectDict.Add(playerNumber, objects);
												}
											}
										}
										else
										{
											throw new JsonException("player_number field is missing!", playerObject);
										}
									}
								}

								if (inputDict.Count > 0)
								{
									Inputs.Add(roomName, inputDict);

									if (timerDict.Count > 0)
									{
										InputTimers.Add(roomName, timerDict);
									}
								}

								if (objectDict.Count > 0)
								{
									SpawnObjects.Add(roomName, objectDict);
								}

								//if (playerValues.Count > 0)
								//{
								//	PlayerValues.Add(roomName, playerValues);
								//}
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				Plugin.Logger.LogError(e);
			}
		}

		internal static void TryGetTrigger(SlugcatStats.Name slugcat, string roomName, out CustomCutscene script)
		{
			script = null;
			foreach (var key in Registry.Keys)
			{
				if (Registry.TryGet(key, out var potentialScene) && (!AllTriggerRooms.TryGetValue(slugcat, out var hashSet) || !hashSet.Contains(roomName)))
				{
					script = potentialScene;
					return;
				}
			}
		}

		internal static void TrySave(CustomCutscene script, InputUI.InputHistory saveValues, ref bool saving)
		{
			var files = AssetManager.ListDirectory("slugbase/cutscenes", includeAll: true);

			foreach (var file in files.Where(file => file.EndsWith(".json")))
			{
				if (typeof(JsonRegistry<CutsceneID, CustomCutscene>).GetMethod("IsMostRecent", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Registry, [file]) is bool isMostRecent && isMostRecent)
				{
					try
					{
						file.Replace('/', Path.DirectorySeparatorChar);

						var jsonValue = JsonAny.Parse(File.ReadAllText(file)).AsObject();
						if (jsonValue.TryGet("id")?.TryString() is string id && new CutsceneID(id, false) is CutsceneID actualID && actualID == script.ID)
						{
							UnityEngine.Debug.Log("Found file to replace values with");

							var jsonDict = JsonConverter.ToDictionary(jsonValue);
							if (jsonDict.TryGetValue("script", out var r) && r is Dictionary<string, object> roomDict)
							{
								foreach (var key in roomDict.Keys)
								{
									if (key == saveValues.roomName)
									{
										UnityEngine.Debug.Log("Found existing room script, replacing inputs...");
										if (roomDict[key] is Dictionary<string, object> scriptDict)
										{
											if (scriptDict.TryGetValue("inputs", out var inputDict) && inputDict is List<object> inputObjs)
											{
												for (int i = 0; i < inputObjs.Count; i++)
												{
													if (inputObjs[i] is Dictionary<string, object> innerInputDict)
													{
														if (innerInputDict.ContainsKey("inputs"))
														{
															innerInputDict.Remove("inputs");
														}

														List<object> newInputList = [];
														float totalTime = 0;
														for (int j = saveValues.recordedInputs.Count - 1; j >= 0; j--)
														{
															var input = saveValues.recordedInputs[j];
															var time = saveValues.recordedTimings[j];
															Dictionary<string, object> keyPairs = [];
															keyPairs.Add("timer", time);
															totalTime += time;

															foreach (var field in typeof(Player.InputPackage).GetFields(BindingFlags.Public | BindingFlags.Instance))
															{
																// Find a way to check if object value is default for that type so it doesn't include unnecessary data
																var fieldValue = field.GetValue(input);
																if (field.FieldType != typeof(Vector2) && fieldValue != null && Convert.ChangeType(fieldValue, field.FieldType) != default)
																{
																	keyPairs.Add(field.Name, field.GetValue(input));
																}
																else if (fieldValue != null && fieldValue is Vector2 vec && vec != default)
																{
																	keyPairs.Add(field.Name + ".x", vec.x);
																	keyPairs.Add(field.Name + ".y", vec.y);
																}
															}

															newInputList.Add(keyPairs);
														}

														if (newInputList.Count > 0)
														{
															innerInputDict.Add("inputs", newInputList);
															if (scriptDict.ContainsKey("duration"))
															{
																scriptDict["duration"] = totalTime;
															}
															else
															{
																scriptDict.Add("duration", totalTime);
															}
														}
														File.WriteAllText(file, JsonHelper.FormatJson(Json.Serialize(jsonDict)));
													}
												}
											}
										}
									}
								}
							}
						}
						break;
					}
					catch (Exception ex)
					{
						UnityEngine.Debug.LogError(ex);
					}
				}
			}

			saving = false;
		}

		class JsonHelper
		{
			private const string INDENT_STRING = "    ";
			public static string FormatJson(string str)
			{
				var indent = 0;
				var quoted = false;
				var sb = new StringBuilder();
				for (var i = 0; i < str.Length; i++)
				{
					var ch = str[i];
					switch (ch)
					{
						case '{':
						case '[':
							sb.Append(ch);
							if (!quoted)
							{
								sb.AppendLine();
								Enumerable.Range(0, ++indent).ForEach(item => sb.Append(INDENT_STRING));
							}
							break;
						case '}':
						case ']':
							if (!quoted)
							{
								sb.AppendLine();
								Enumerable.Range(0, --indent).ForEach(item => sb.Append(INDENT_STRING));
							}
							sb.Append(ch);
							break;
						case '"':
							sb.Append(ch);
							bool escaped = false;
							var index = i;
							while (index > 0 && str[--index] == '\\')
								escaped = !escaped;
							if (!escaped)
								quoted = !quoted;
							break;
						case ',':
							sb.Append(ch);
							if (!quoted)
							{
								sb.AppendLine();
								Enumerable.Range(0, indent).ForEach(item => sb.Append(INDENT_STRING));
							}
							break;
						case ':':
							sb.Append(ch);
							if (!quoted)
								sb.Append(" ");
							break;
						default:
							sb.Append(ch);
							break;
					}
				}
				return sb.ToString();
			}
		}
	}
}
