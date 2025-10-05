using ExtendedSlugbaseFeatures.Helpers;
using RWCustom;
using SlugBase;
using SlugBase.Features;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ExtendedSlugbaseFeatures.Helpers.PlayerHelpers;
using static ExtendedSlugbaseFeatures.Helpers.RoomSpecificScriptHelpers;

namespace ExtendedSlugbaseFeatures.Resources;
internal class ExtFeatures
{
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
	/// When above 0, uses the current <see cref="RoomPalette.blackColor"/> alongside the <see cref="SlugBaseCharacter"/>'s custom colors if present.
	/// </summary>
	public static readonly PlayerFeature<Dictionary<string, float[]>> blackColorFade = new("use_blackcolor", json =>
	{
		Dictionary<string, float[]> fades = [];

		if (json.TryObject() is JsonObject obj)
		{
			if (obj.TryGet("fades")?.TryList() is JsonList list && list.Count > 0)
			{
				float[] floats = new float[list.Count];
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].TryFloat() is float value)
					{
						floats[i] = value;
					}
					else
					{
						throw new JsonException("Value is not a float!", list[i]);
					}
				}
				fades.Add("fades", floats);
			}
			if (obj.TryGet("variance")?.TryList() is JsonList lerp && lerp.Count > 0)
			{
				float[] floats = new float[lerp.Count];
				for (int i = 0; i < lerp.Count; i++)
				{
					if (lerp[i].TryFloat() is float value)
					{
						floats[i] = value;
					}
					else
					{
						throw new JsonException("Value is not a float!", lerp[i]);
					}
				}
				fades.Add("variance", floats);
			}
		}
		else
		{
			throw new JsonException("No object found!", json);
		}

		return fades;
	});

	/// <summary>
	/// If a valid <see cref="AbstractPhysicalObject.AbstractObjectType"/> exists, overrides the <see cref="Player.Grabability(PhysicalObject)"/> for that type.
	/// </summary>
	public static readonly PlayerFeature<Grabability> objectGrabability = new("grab_overrides", (json) => new Grabability(json));

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
	public static readonly PlayerFeature<Dictionary<string, float[]>> rivGills = new("gills", json =>
	{
		Dictionary<string, float[]> gillsInfo = [];

		if (json.TryObject() is JsonObject obj)
		{
			string[] keys = ["rows"];
			foreach (var key in keys)
			{
				if (obj.TryGet(key)?.TryFloat() is float flt)
				{
					gillsInfo.Add(key, [flt]);
				}
				if (obj.TryGet(key)?.TryList() is JsonList floats)
				{
					float[] flts = new float[floats.Count];
					for (int i = 0; i < floats.Count; i++)
					{
						if (floats[i].TryFloat() is float flt2)
						{
							flts[i] = flt2;
						}
					}
					gillsInfo.Add(key, flts);
				}
			}
		}

		return gillsInfo;
	});
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to spawn <see cref="Spear"/>s from <see cref="PlayerGraphics.tailSpecks"/>. The default <see cref="int"/> of <see cref="PlayerGraphics.tailSpecks"/> is 3 and 5.
	/// </summary>
	public static readonly PlayerFeature<int[]> rowsAndColumnsSpearSpecks = FeatureTypes.PlayerInts("spear_specks", 2);

	/// <summary>
	/// Replaces the default face sprite with Artificer's variant. <see cref="usesSaintFaceCondition"/> takes priority over this due to how <see cref="PlayerGraphics.SaintFaceCondition"/> runs.
	/// </summary>
	public static readonly PlayerFeature<bool> hasArtiFace = FeatureTypes.PlayerBool("arti_eyes");

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
	/// When present, replaces an existing <see cref="Overseer"/>'s colors with a custom color during a story campaign.
	/// </summary>
	public static readonly GameFeature<Dictionary<int, Color>> overseerOverwrite = new("overseer_overwrite", json =>
	{
		Dictionary<int, Color> overseerColor = [];

		if (json.TryList() is JsonList list)
		{
			foreach (var item in list)
			{
				if (item.TryObject() is JsonObject obj)
				{
					if (obj.TryGet("owner")?.TryInt() is int owner && obj.TryGet("color") is JsonAny color)
					{
						overseerColor[owner] = JsonUtils.ToColor(color);
					}
					else
					{
						throw new JsonException("Owner and/or color property are missing!", item);
					}
				}
			}
		}

		return overseerColor;
	});

	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to pass OE's gate, with an extra condition to check if Gourmand has been beaten if desired.
	/// </summary>
	public static readonly GameFeature<bool[]> openOEGate = JsonResources.GameBools("can_pass_OE_gate", 1, 2);

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
			if (AbstractPhysicalObjectHelpers.JSONtoAbstractObjectParameters(item.Value.AsObject(), item.Key, out var dict))
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
	public static readonly GameFeature<CustomCutscene.CutsceneID> introCutscene = FeatureTypes.GameExtEnum<CustomCutscene.CutsceneID>("intro_cutscene");
}
