using RWCustom;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtendedSlugbaseFeatures
{
	public class Resources
	{
		public static readonly Feature<bool> rivuletGills = FeatureTypes.PlayerBool("rivuletgills");
		public static readonly Feature<bool> spearSpecks = FeatureTypes.PlayerBool("can_spawnspears");

		public static readonly PlayerFeature<bool> artiEyes = FeatureTypes.PlayerBool("artieyes");
		public static readonly PlayerFeature<float> watcherBlackAmount = FeatureTypes.PlayerFloat("use_watchersblackamount");
		public static readonly PlayerFeature<bool> saintFluff = FeatureTypes.PlayerBool("saintfluff");
		public static readonly PlayerFeature<bool> explosiveJump = FeatureTypes.PlayerBool("can_explosivejump");
		public static readonly PlayerFeature<bool> explosionCraft = FeatureTypes.PlayerBool("can_craftexplosives");
		public static readonly PlayerFeature<bool> getKarmaFromScavs = FeatureTypes.PlayerBool("get_karmafromscavs");
		public static readonly PlayerFeature<bool> dualWield = FeatureTypes.PlayerBool("can_dualwield");
		public static readonly PlayerFeature<bool> saintEyes = FeatureTypes.PlayerBool("sainteyes");
		public static readonly PlayerFeature<bool> canSlam = FeatureTypes.PlayerBool("can_slam");
		public static readonly PlayerFeature<bool> cantSwallowObjects = FeatureTypes.PlayerBool("cant_swallowobjects");
		public static readonly PlayerFeature<bool> feedFromSpears = FeatureTypes.PlayerBool("feedsfromspears");

		public static readonly GameFeature<Dictionary<string, Dictionary<Type, object>>> introCutscene = new("intro_cutscene", json =>
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
		/// Processes potential object properties from the <see cref="SlugBaseCharacter"/> JSON.
		/// </summary>
		/// <exception cref="JsonException"></exception>
		private static void ProcessObjectFromJSON(JsonAny objectCanidate, out AbstractPhysicalObject.AbstractObjectType key, out Dictionary<string, object> properties)
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

		public static readonly GameFeature<int> maxSlugpups = FeatureTypes.GameInt("max_slugpupspawns");
		public static readonly GameFeature<bool> canSpawnKarmaFlowers = FeatureTypes.GameBool("spawn_karmaflowers");
		public static readonly GameFeature<bool> spirituallyEnlightened = FeatureTypes.GameBool("enlightened");
		public static readonly GameFeature<bool> spawnBroadcasts = FeatureTypes.GameBool("can_accesswhitetokens");
		public static readonly GameFeature<bool> revealMark = FeatureTypes.GameBool("revealmarkovertime");
		/// <summary>
		/// Returns a dictionary of a <see cref="AbstractPhysicalObject"/> the <see cref="SlugBaseCharacter"/> starts their campaign with.
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
		public static readonly GameFeature<int[]> spawnPosition = FeatureTypes.GameInts("start_position");

		internal static bool ILSpearSpecks(bool orig, Player player) => ILHasFeature(orig, player, spearSpecks);
		internal static bool ILCraftExplosives(bool orig, Player player) => ILHasFeature(orig, player, explosionCraft);
		internal static bool ILScavCorpseKarma(bool orig, Player player) => ILHasFeature(orig, player, getKarmaFromScavs);
		internal static bool ILExplosiveJump(bool orig, Player player) => ILHasFeature(orig, player, explosiveJump);
		internal static bool ILSlam(bool orig, Player player) => ILHasFeature(orig, player, canSlam);

		internal static bool ILHasFeature(bool orig, Player player, Feature<bool> feature)
		{
			return orig || player.HasFeature(feature);
		}
		internal static bool ILHasFeature(bool orig, Player player, GameFeature<bool> feature)
		{
			return orig || player.HasFeature(feature);
		}
		internal static bool ILHasFeature(bool orig, Player player, PlayerFeature<bool> feature)
		{
			return orig || player.HasFeature(feature);
		}

		internal static AbstractPhysicalObject ParseDictToObject(Room room, AbstractPhysicalObject.AbstractObjectType objType, Dictionary<string, object> properties)
		{
			AbstractPhysicalObject absObject = null;
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
					absObject = spear;
					break;

				case var _ when objType == AbstractPhysicalObject.AbstractObjectType.DataPearl:
					DataPearl.AbstractDataPearl pearl = new(room.game.world, objType, null, pos, room.game.GetNewID(), room.abstractRoom.index, 0, null, null);
					if (properties != null && properties.ContainsKey(nameof(DataPearl.AbstractDataPearl.dataPearlType)) && properties[nameof(DataPearl.AbstractDataPearl.dataPearlType)] is DataPearl.AbstractDataPearl.DataPearlType pearlType)
					{
						pearl.dataPearlType = pearlType;
					}
					absObject = pearl;
					break;

				case var _ when objType == AbstractPhysicalObject.AbstractObjectType.WaterNut:
					WaterNut.AbstractWaterNut waterNut = new(room.game.world, null, pos, room.game.GetNewID(), room.abstractRoom.index, 0, null, false);
					if (properties != null && properties.ContainsKey(nameof(WaterNut.AbstractWaterNut.swollen)) && properties[nameof(WaterNut.AbstractWaterNut.swollen)] is bool swollen)
					{
						waterNut.swollen = swollen;
					}
					absObject = waterNut;
					break;

				case var _ when ExtensionResources.IsUnrecognizedType(objType):
					AbstractPhysicalObject obj = ExtensionResources.ParseUnrecognizedTypes(objType, properties);
					if (obj != null)
					{
						absObject = obj;
					}
					break;

				default:
					AbstractPhysicalObject defaultObj = new(room.game.world, objType, null, pos, room.game.GetNewID());
					absObject = defaultObj;
					break;
			}

			if (absObject != null)
			{
				if (properties != null && properties.ContainsKey(nameof(AbstractPhysicalObject.ID.altSeed)) && properties[nameof(AbstractPhysicalObject.ID.altSeed)] is int altSeed)
				{
					absObject.ID.setAltSeed(altSeed);
				}
			}

			return absObject;
		}

		internal static bool HandleFood(Player player, float food)
		{
			int quarterPips = Mathf.RoundToInt(food * 4f);

			for (; quarterPips >= 4; quarterPips -= 4)
				player.AddFood(1);

			for (; quarterPips >= 1; quarterPips--)
				player.AddQuarterFood();

			return food > 0f;
		}
	}
}