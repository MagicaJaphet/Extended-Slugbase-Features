using ExtendedSlugbase.Helpers;
using RWCustom;
using SlugBase;
using SlugBase.Features;
using static ExtendedSlugbase.Helpers.AbstractHelpers;
using static ExtendedSlugbase.Helpers.FeatureHelpers;
using static ExtendedSlugbase.Objects.PlayerObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ExtendedSlugbase.Objects;
using static ExtendedSlugbase.Objects.GameObjects;

namespace ExtendedSlugbase.Features;
public  class GameFeaturesExt
{
	/// <summary>
	/// Forces <see cref="SlugBaseCharacter"/> to use Hunter's illness mechanic, meaning the character dies past <see cref="int"/> number of cycles. An extra <see cref="int"/> can be set for the maximum number of <see cref="MMF.cfgHunterBonusCycles"/> one recieves from Five Pebbles.
	/// </summary>
	public static readonly GameFeature<HardMode> cycleLimit = new("limited_cycles", json => new HardMode(json));

	/// <summary>
	/// When present, overrides any static relationship values specifically for the Slugcat.
	/// </summary>
	public static readonly GameFeature<CreatureRelationship> creaturePlayerRelationOverrides = new("creature_relationships", json => new CreatureRelationship(json));

	/// <summary>
	/// When present, replaces an existing <see cref="Overseer"/>'s colors with a custom color during a story campaign.
	/// </summary>
	public static readonly GameFeature<Dictionary<int, Color>> overseerOverwrite = new("overseer_overwrite", json =>
	{
		Dictionary<int, Color> overseerColor = [];

		if (json.TryParse(out JsonObject[] objs))
		{
			foreach (var obj in objs)
			{
				overseerColor[obj.GetInt("owner")] = obj.GetColor("color");
			}
		}

		return overseerColor;
	});

	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to gain <see cref="Player.Karma"/> from holding a <see cref="Scavenger"/> corpse.
	/// </summary>
	public static readonly GameFeature<bool> getKarmaFromScavs = FeatureTypes.GameBool("get_karma_from_scavs");

	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to sense when an Echo is in the region.
	/// </summary>
	public static readonly GameFeature<bool> ghostPing = FeatureTypes.GameBool("ghost_pings");

	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to pass OE's gate, with an extra condition to check if Gourmand has been beaten if desired.
	/// </summary>
	public static readonly GameFeature<bool[]> openOEGate = GameBools("can_pass_OE_gate", 1, 2);

	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Returns the <see cref="int"/> of slugpups requested into <see cref="StoryGameSession.slugPupMaxCount"/>.
	/// </summary>
	public static readonly GameFeature<int> maxSlugpupSpawns = FeatureTypes.GameInt("max_slugpup_spawns");

	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to choose whether they voluntarily spawn <see cref="KarmaFlower"/>.
	/// </summary>
	public static readonly GameFeature<bool> spawnKarmaFlowers = FeatureTypes.GameBool("spawn_karma_flowers");

	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to speak to <see cref="Oracle"/> and <see cref="Ghost"/> without the mark, as well as being able to see <see cref="VoidSpawn"/> without the mark.
	/// </summary>
	public static readonly GameFeature<bool> enlightenedState = FeatureTypes.GameBool("enlightened");

	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to spawn with an ID drone, like Artificer.
	/// </summary>
	public static readonly GameFeature<bool> hasIDDrone = FeatureTypes.GameBool("has_id_drone");

	[RequiresDLC(MSC: true)]
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
	public static readonly GameFeature<IntVector2[]> possibleSpawnPositons = new("start_position", json =>
	{
		IntVector2[] startingPositions = [];

		if (json.TryParse(out JsonList list))
		{
			if (list.ParseListItems<JsonList>(throwIfParseError: false) is JsonList[] rooms)
			{
				startingPositions = [.. from room in rooms select new IntVector2(room.GetInt(0), room.GetInt(1))];
			}
			else
			{
				startingPositions = [new IntVector2(list.GetInt(0), list.GetInt(1))];
			}
		}

		return startingPositions;
	});

	/// <summary>
	/// Returns a dictionary of a <see cref="AbstractPhysicalObject"/> the <see cref="SlugBaseCharacter"/> starts their campaign with. Only allows one object to spawn, as per the typical stomach limit.
	/// </summary>
	public static readonly GameFeature<AbstractObject> spawnStomachObject = new("start_stomach_item", json => new AbstractObject(json));

	//FEATURE: Implement when this is ready lol
	/// <summary>
	/// Parses <see cref="AbstractPhysicalObject"/> the <see cref="Player"/> starts with in their hands, along with any inputs that should be passed to <see cref="Player.InputPackage"/>.
	/// </summary>
	//public static readonly GameFeature<CustomCutscene.CutsceneID> introCutscene = FeatureTypes.GameExtEnum<CustomCutscene.CutsceneID>("intro_cutscene");
}
