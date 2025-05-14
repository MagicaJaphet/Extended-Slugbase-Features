using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ExtendedSlugbaseFeatures
{
	public class Resources
	{
		public class ExtFeatures
		{
			/// <summary>
			/// Extension returning a new <see cref="GameFeatures"/> instance which allows for multiple <see cref="bool"/>.
			/// </summary>
			public static GameFeature<bool[]> GameBools(string id, int minLength = 0, int maxLength = int.MaxValue)
			{;
				return new GameFeature<bool[]>(id, (JsonAny json) => {
					// This method is private in the SlugBase.dll :(
					typeof(FeatureTypes).GetMethod("AssertLength", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(json, [json, minLength, maxLength]);
					if (json is JsonAny any)
					{
						return ToBools(any);
					}
					return null;
				});
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
			/// Allows <see cref="SlugBaseCharacter"/> to automatically pop <see cref="WaterNut"/> when held.
			/// </summary>
			public static readonly PlayerFeature<bool> popBubbleFruit = FeatureTypes.PlayerBool("pop_held_bubblefruit");

			/// <summary>
			/// Allows <see cref="SlugBaseCharacter"/> to pass OE's gate, with an extra condition to check if Gourmand has been beaten if desired.
			/// </summary>
			public static readonly GameFeature<bool[]> openOEGate = GameBools("can_pass_OE_gate", 1, 2);

			/// <summary>
			/// Forces <see cref="SlugBaseCharacter"/> to use Hunter's illness mechanic, meaning the character dies past <see cref="int"/> number of cycles. An extra <see cref="int"/> can be set for the maximum number of <see cref="MMF.cfgHunterBonusCycles"/> one recieves from Five Pebbles.
			/// </summary>
			public static readonly GameFeature<int[]> cycleLimit = FeatureTypes.GameInts("max_cycle_limit", 1, 2);

			/// <summary>
			/// Allows <see cref="SlugBaseCharacter"/> to use Saint's tongue mechanic.
			/// </summary>
			public static readonly PlayerFeature<bool> saintTongue = FeatureTypes.PlayerBool("has_tongue");

			/// <summary>
			/// Forces <see cref="SlugBaseCharacter"/> to be unable to use <see cref="Spear"/>s, instead tossing them.
			/// </summary>
			public static readonly PlayerFeature<bool> tossSpears = FeatureTypes.PlayerBool("only_tosses_spears");
			#endregion

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
			/// Allows <see cref="SlugBaseCharacter"/> to use Artificer's explosive jump ability, setting the soft and hard limits in <see cref="int"/>s.
			/// </summary>
			public static readonly PlayerFeature<int[]> explosiveJumpLimits = FeatureTypes.PlayerInts("explosive_jump", 1, 2);

			/// <summary>
			/// Allows <see cref="SlugBaseCharacter"/> to use <see cref="Player.FoodInStomach"/> to craft explosives, and the <see cref="int"/> cost in quarter intervals.
			/// </summary>
			public static readonly PlayerFeature<int[]> explosiveCraftCost = FeatureTypes.PlayerInts("craft_explosives_cost", 1, 3);

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
						Dictionary<Type, object> roomContents = new();
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

								if (!roomContents.ContainsKey(typeof(Player.InputPackage)) && introCutsceneInputs.Count > 0)
								{
									UnityEngine.Debug.Log("Found inputs!");
									roomContents.Add(typeof(Player.InputPackage), introCutsceneInputs);
								}
								if (!roomContents.ContainsKey(typeof(int)) && introCutsceneTimedInputs.Count > 0)
								{
									roomContents.Add(typeof(int), introCutsceneTimedInputs);
								}
							}
							// "player_grasps": [ ]
							else if (component.Key == "player_grasps")
							{
								JsonList objectDict = component.Value.AsList();
								Dictionary<AbstractPhysicalObject.AbstractObjectType, Dictionary<string, object>> introCutsceneObjects = [];

								// { "type":"Spear", "electric":3 }
								foreach (var objectCanidate in objectDict)
								{
									ProcessObjectFromJSON(objectCanidate, out var key, out var dict);

									if (key != null && dict != null && !introCutsceneObjects.ContainsKey(key))
									{
										introCutsceneObjects.Add(key, dict);
									}
								}

								if (!roomContents.ContainsKey(typeof(AbstractPhysicalObject)) && introCutsceneObjects.Count > 0)
								{
									UnityEngine.Debug.Log("Found objects!");
									roomContents.Add(typeof(AbstractPhysicalObject), introCutsceneObjects);
								}
							}
						}

						roomObjects.Add(room.Key, roomContents);
					}
				}

				return roomObjects;
			});

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
			/// Returns a dictionary of a <see cref="AbstractPhysicalObject"/> the <see cref="SlugBaseCharacter"/> starts their campaign with. Only allows one object to spawn, as per the typical stomach limit.
			/// </summary>
			public static readonly GameFeature<Dictionary<AbstractPhysicalObject.AbstractObjectType, Dictionary<string, object>>> spawnStomachObject = new("start_stomach_item", json =>
			{
				Dictionary<AbstractPhysicalObject.AbstractObjectType, Dictionary<string, object>> stomachObject = [];

				JsonObject objectJSON = json.AsObject();
				ProcessObjectFromJSON(objectJSON, out var key, out var dict);

				if (key != null && dict != null && !stomachObject.ContainsKey(key))
				{
					stomachObject.Add(key, dict);
				}

				return stomachObject;
			});

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
		}
		
		/// <summary>
		/// Processes potential object properties from the <see cref="SlugBaseCharacter"/> JSON.
		/// </summary>
		/// <exception cref="JsonException"></exception>
		internal static void ProcessObjectFromJSON(JsonAny objectCanidate, out AbstractPhysicalObject.AbstractObjectType key, out Dictionary<string, object> properties)
		{
			key = null;
			properties = null;

			JsonObject objectProperties = objectCanidate.AsObject();
			// "type":"Spear"
			if (objectProperties.TryGet("type") is JsonAny any && any.TryString() is string typeString && new AbstractPhysicalObject.AbstractObjectType(typeString, false) is AbstractPhysicalObject.AbstractObjectType objType)
			{
				key = objType;
			}
			else
			{
				throw new JsonException("Object is not a valid type!", objectProperties);
			}


			if (key != null)
			{
				properties = [];
				switch (key)
				{
					case var _ when key == AbstractPhysicalObject.AbstractObjectType.Spear:
						if (objectProperties.TryGet(nameof(AbstractSpear.explosive)) is JsonAny explosive)
						{
							properties.Add(nameof(AbstractSpear.explosive), explosive.TryBool() ?? false);
						}
						if (objectProperties.TryGet(nameof(AbstractSpear.electricCharge)) is JsonAny electric)
						{
							properties.Add(nameof(AbstractSpear.electricCharge), electric.TryInt() ?? 0);
						}
						if (objectProperties.TryGet(nameof(AbstractSpear.hue)) is JsonAny bugSpear)
						{
							properties.Add(nameof(AbstractSpear.hue), bugSpear.TryFloat() ?? 0f);
						}
						if (objectProperties.TryGet(nameof(AbstractSpear.poison)) is JsonAny poison)
						{
							properties.Add(nameof(AbstractSpear.poison), poison.TryFloat() ?? 0f);
						}
						if (objectProperties.TryGet(nameof(AbstractSpear.poisonHue)) is JsonAny poisonHue)
						{
							properties.Add(nameof(AbstractSpear.poisonHue), poison.TryFloat() ?? 0f);
						}
						if (objectProperties.TryGet(nameof(AbstractSpear.needle)) is JsonAny needle)
						{
							properties.Add(nameof(AbstractSpear.needle), poison.TryBool() ?? false);
						}
						ExtensionResources.HandleCustomSpearJSON(objectProperties, properties);

						if (objectProperties.TryGet(nameof(AbstractPhysicalObject.ID.altSeed)) is JsonAny spearSeed && spearSeed.TryInt() is int spearID)
						{
							properties.Add(nameof(AbstractPhysicalObject.ID.altSeed), spearID);
						}
						break;

					case var _ when key == AbstractPhysicalObject.AbstractObjectType.DataPearl:
						if (objectProperties.TryGet(nameof(DataPearl.AbstractDataPearl.dataPearlType)) is JsonAny pearlType && pearlType.TryString() is string name && new DataPearl.AbstractDataPearl.DataPearlType(name, false) is DataPearl.AbstractDataPearl.DataPearlType realType)
						{
							properties.Add(nameof(DataPearl.AbstractDataPearl.dataPearlType), realType);
						}
						else
						{
							properties.Add(nameof(DataPearl.AbstractDataPearl.dataPearlType), DataPearl.AbstractDataPearl.DataPearlType.Misc);
						}

						if (objectProperties.TryGet(nameof(AbstractPhysicalObject.ID.altSeed)) is JsonAny pearlSeed && pearlSeed.TryInt() is int pearlID)
						{
							properties.Add(nameof(AbstractPhysicalObject.ID.altSeed), pearlID);
						}
						break;

					case var _ when key == AbstractPhysicalObject.AbstractObjectType.WaterNut:
						if (objectProperties.TryGet(nameof(WaterNut.AbstractWaterNut.swollen)) is JsonAny waterType && waterType.TryBool() is bool swollen)
						{
							properties.Add(nameof(WaterNut.AbstractWaterNut.swollen), swollen);
						}
						break;

					case var _ when ExtensionResources.IsUnrecognizedType(key):
						ExtensionResources.HandleUnrecognizedTypes(objectProperties, out properties);
						break;
				}

				if (objectProperties.TryGet(nameof(AbstractPhysicalObject.ID.altSeed)) is JsonAny seed && seed.TryInt() is int entityID)
				{
					properties.Add(nameof(AbstractPhysicalObject.ID.altSeed), entityID);
				}
			}
		}

		/// <summary>
		/// Parses <paramref name="properties"/> into a <see cref="AbstractPhysicalObject"/>, setting the <paramref name="abstractObject"/> that uses this extension.
		/// </summary>
		/// <returns></returns>
		internal static bool ParseDictToObject(out AbstractPhysicalObject result, Room room, AbstractPhysicalObject.AbstractObjectType objType, Dictionary<string, object> properties)
		{
			result = null;
			WorldCoordinate pos = new(room.abstractRoom.index, -1, -1, 0);
			switch (objType)
			{
				case var _ when objType == AbstractPhysicalObject.AbstractObjectType.Spear:
					AbstractSpear spear = new(room.world, null, pos, room.game.GetNewID(), false);
					if (properties != null)
					{
						if (properties.ContainsKey(nameof(AbstractSpear.explosive)) && properties[nameof(AbstractSpear.explosive)] is bool isExplosive)
						{
							spear.explosive = isExplosive;
						}
						if (properties.ContainsKey(nameof(AbstractSpear.electricCharge)) && properties[nameof(AbstractSpear.electricCharge)] is int charges)
						{
							spear.electric = true;
							spear.electricCharge = charges;
						}
						if (properties.ContainsKey(nameof(AbstractSpear.hue)) && properties[nameof(AbstractSpear.hue)] is float hue)
						{
							spear.hue = hue;
						}
						if (properties.ContainsKey(nameof(AbstractSpear.poison)) && properties[nameof(AbstractSpear.poison)] is float poisonAmount)
						{
							spear.poison = poisonAmount;
						}
						if (properties.ContainsKey(nameof(AbstractSpear.poisonHue)) && properties[nameof(AbstractSpear.poisonHue)] is float poisonHue)
						{
							spear.poisonHue = poisonHue;
						}
						if (properties.ContainsKey(nameof(AbstractSpear.needle)) && properties[nameof(AbstractSpear.needle)] is bool needle)
						{
							spear.needle = needle;
						}
						ExtensionResources.HandleCustomAbstractSpearProperties(spear, properties);
					}
					result = spear;
					break;

				case var _ when objType == AbstractPhysicalObject.AbstractObjectType.DataPearl:
					DataPearl.AbstractDataPearl pearl = new(room.game.world, objType, null, pos, room.game.GetNewID(), room.abstractRoom.index, 0, null, null);
					if (properties != null && properties.ContainsKey(nameof(DataPearl.AbstractDataPearl.dataPearlType)) && properties[nameof(DataPearl.AbstractDataPearl.dataPearlType)] is DataPearl.AbstractDataPearl.DataPearlType pearlType)
					{
						pearl.dataPearlType = pearlType;
					}
					result = pearl;
					break;

				case var _ when objType == AbstractPhysicalObject.AbstractObjectType.WaterNut:
					WaterNut.AbstractWaterNut waterNut = new(room.game.world, null, pos, room.game.GetNewID(), room.abstractRoom.index, 0, null, false);
					if (properties != null && properties.ContainsKey(nameof(WaterNut.AbstractWaterNut.swollen)) && properties[nameof(WaterNut.AbstractWaterNut.swollen)] is bool swollen)
					{
						waterNut.swollen = swollen;
					}
					result = waterNut;
					break;

				case var _ when ExtensionResources.IsUnrecognizedType(objType):
					AbstractPhysicalObject obj = ExtensionResources.ParseUnrecognizedTypes(objType, properties);
					if (obj != null)
					{
						result = obj;
					}
					break;

				default:
					AbstractPhysicalObject defaultObj = new(room.game.world, objType, null, pos, room.game.GetNewID());
					result = defaultObj;
					break;
			}

			if (result != null)
			{
				if (properties != null && properties.ContainsKey(nameof(AbstractPhysicalObject.ID.altSeed)) && properties[nameof(AbstractPhysicalObject.ID.altSeed)] is int altSeed)
				{
					result.ID.setAltSeed(altSeed);
				}
				return true;
			}
			return false;
		}

		public static ConditionalWeakTable<AbstractSpear, SpearValues> spearCWT = new();
		public class SpearValues
		{
			public Color? slugColor;
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

		internal static bool TrueForSlugbase<T>(this Player player, PlayerFeature<T> feature, bool shouldBeTrue = true)
		{
			if (player.HasFeature(feature, out var value))
			{
				if (value is bool boolValue)
				{
					return (shouldBeTrue && boolValue) || (!shouldBeTrue && !boolValue);
				}
				else if (value is bool[] boolValues)
				{
					return (shouldBeTrue && boolValues.Any(x => x)) || (!shouldBeTrue && !boolValues.Any(x => x));
				}
				else if (value is int intValue)
				{
					return (shouldBeTrue && intValue > -1) || (!shouldBeTrue && intValue < 0);
				}
				else if (value is int[] intValues)
				{
					return (shouldBeTrue && intValues.Any(x => x > -1)) || (!shouldBeTrue && !intValues.Any(x => x > -1));
				}
				else if (value is float floatValue)
				{
					return (shouldBeTrue && floatValue > -1) || (shouldBeTrue && floatValue < 0);
				}
				else
				{
					return (shouldBeTrue && value != null) || (!shouldBeTrue && value == null);
				}
			}
			return false;
		}

		internal static bool HasFeature<T>(this Player player, PlayerFeature<T> feature, out T value)
		{
			return feature.TryGet(player, out value);
		}

		internal static bool HasFeature<T>(this RainWorldGame game, GameFeature<T> feature, out T value)
		{
			return feature.TryGet(game, out value);
		}
	}
}