using ExtendedSlugbase.Features.GameRelated;
using ExtendedSlugbase.Features.PlayerRelated;
using ExtendedSlugbase.Features.TimelineRelated;
using SlugBase.Features;
using SlugBase;
using System.ComponentModel;
using static ExtendedSlugbase.Features.ExtFeatureTypes;

namespace ExtendedSlugbase.Features;

public class ExtGameFeatures
{
	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to use <see cref="PlayerRelated.InitChatLog"/> if the <see cref="CollectToken.whiteToken"/> object exists in their <see cref="RegionState"/>.
	/// </summary>
	public static readonly CanProcessBroadcasts CanProcessBroadcasts = new();

	/// <summary>
	/// When present, overrides any static relationship values specifically for the Slugcat.
	/// </summary>
	public static readonly CreaturePlayerRelationOverrides CreaturePlayerRelationOverrides = new();
	
	/// <summary>
	/// Forces <see cref="SlugBaseCharacter"/> to use <see cref="SlugcatStats.Name.Red"/>'s illness mechanic, meaning the character dies past <see cref="int"/> number of cycles. An extra <see cref="int"/> can be set for the maximum number of <see cref="MMF.cfgHunterBonusCycles"/> one recieves from Five Pebbles.
	/// </summary>
	public static readonly CycleLimit CycleLimit = new();

	/// <summary>
	/// Disallows <see cref="SlugBaseCharacter"/> from using passages or making progress on specified passages.
	/// </summary>
	public static readonly DisablePassages DisablePassages = new();

	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to speak to <see cref="Oracle"/> and <see cref="Ghost"/> without the mark, as well as being able to see <see cref="VoidSpawn"/> without the mark.
	/// </summary>
	public static readonly EnlightenedState EnlightenedState = new();

	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to gain <see cref="PlayerRelated.Karma"/> from holding a <see cref="Scavenger"/> corpse.
	/// </summary>
	public static readonly GetKarmaFromScavs GetKarmaFromScavs = new();

	/// <summary>
	/// Overrides the default <see cref="Menu.MenuScene.SceneID"/> used when selecting <see cref="SlugBaseCharacter"/> in Expedition.
	/// </summary>
	public static readonly ExpeditionMenuSceneID ExpeditionMenuSceneID = new();

	/// <summary>
	/// Extended guide overseer properties.
	/// </summary>
	public static readonly ExtGuideOverseer ExtGuideOverseer = new();
	
	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to sense when an Echo is in the region.
	/// </summary>
	public static readonly HasGhostPing HasGhostPing = new();

	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to spawn with an ID drone, like Artificer.
	/// </summary>
	public static readonly HasIDDrone HasIDDrone = new();

	/// <summary>
	/// Forces <see cref="SlugBaseCharacter"/> to normally be unselectable in the select menu.
	/// </summary>
	public static readonly HiddenOrUnplayable HiddenOrUnplayable = new();

	// LATER: Add
	//public static readonly GameFeature<CustomCutscene.CutsceneID> introCutscene = FeatureTypes.GameExtEnum<CustomCutscene.CutsceneID>("intro_cutscene");
	
	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Returns the <see cref="int"/> of slugpups requested into <see cref="StoryGameSession.slugPupMaxCount"/>.
	/// </summary>
	public static readonly MaxSlugpupSpawns MaxSlugpupSpawns = new();

	/// <summary>
	/// Forces <see cref="SlugBaseCharacter"/> to use the statistics button after ascending like Artificer or Saint.
	/// </summary>
	public static readonly NoContinueAfterAscension NoContinueAfterAscension = new();

	/// <summary>
	/// Disallows <see cref="SlugBaseCharacter"/> from being able to starve, even with food pips.
	/// </summary>
	public static readonly NoStarvation NoStarvation = new();

	/// <summary>
	/// When present, replaces an existing <see cref="Overseer"/>'s colors with a custom color during a story campaign.
	/// </summary>
	public static readonly OverseerColorOverrides OverseerColorOverrides = new();

	/// <summary>
	/// Forces certain slugcats to be beaten before unlocking <see cref="SlugBaseCharacter"/>.
	/// </summary>
	public static readonly ProgressionLocked ProgressionLocked = new();

	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to choose whether they voluntarily spawn <see cref="KarmaFlower"/>.
	/// </summary>
	public static readonly SpawnKarmaFlowers SpawnKarmaFlowers = new();

	/// <summary>
	/// Forces a <see cref="SlugBaseCharacter"/>'s mark to reveal overtime in-game and in the select screen.
	/// </summary>
	public static readonly RevealMarkOverCycles RevealMarkOverCycles = new();

	/// <summary>
	/// Returns the starting position of the <see cref="PlayerRelated"/> in room tiles based on the room name, if it exists.
	/// </summary>
	public static readonly StartingSpawnPositions StartingSpawnPositions = new();

	/// <summary>
	/// Returns a dictionary of a <see cref="AbstractPhysicalObject"/> the <see cref="SlugBaseCharacter"/> starts their campaign with. Only allows one object to spawn, as per the typical stomach limit.
	/// </summary>
	public static readonly StartingStomachObject StartingStomachObject = new();

	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to pass OE's gate, with an extra condition to check if Gourmand has been beaten if desired.
	/// </summary>
	public static readonly UnlockOEGate UnlockOEGate = new();
}

public class ExtPlayerFeatures
{
	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Forces <see cref="SlugBaseCharacter"/> to rest if they exhibit too much physical activity, with the first <see cref="int"/> declaring the exhaustion time, and an optional 2nd <see cref="int"/> for how much activity causes exhaustion.
	/// </summary>
	public static readonly AerobicExhaustion AerobicExhaustion = new();

	[RequiresDLC(MSC: true)]
	/// <summary>
	/// A vertsitile crafting table that allows <see cref="SlugBaseCharacter"/> to turn anything tangible into something else.
	/// </summary>
	public static readonly CanCraftObjects CanCraftObjects = new();

	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to spawn <see cref="Spear"/>s from <see cref="PlayerGraphics.tailSpecks"/>. The default <see cref="int"/> of <see cref="PlayerGraphics.tailSpecks"/> is 3 and 5.
	/// </summary>
	public static readonly CanCreateSpears CanCreateSpears = new();

	/// <summary>
	/// Disallows <see cref="SlugBaseCharacter"/> from swallowing or regurgitating objects if true.
	/// </summary>
	public static readonly CantSwallowObjects CantSwallowObjects = new();

	/// <summary>
	/// Changes the default <see cref="Player.DeathByBiteMultiplier"/> value, with the second value altering the difficulty.
	/// </summary>
	public static readonly DeathByBiteMultiplier DeathByBiteMultiplier = new();

	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to use Artificer's explosive jump ability, setting the soft and hard limits in <see cref="int"/>s.
	/// </summary>
	public static readonly DoubleJump DoubleJump = new();

	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Forces <see cref="SlugBaseCharacter"/> to be smaller and use slugpup stats.
	/// </summary>
	public static readonly IsSlugpup IsSlugpup = new();

	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to inflict damage when hitting a <see cref="Creature"/> with momentum.
	/// </summary>
	public static readonly GourmandSlam GourmandSlam = new();

	/// <summary>
	/// Forces <see cref="SlugBaseCharacter"/> to hold onto items when stunned for any reason.
	/// </summary>
	public static readonly NoStunGraspPenalties NoStunGraspPenalties = new();

	/// <summary>
	/// If a valid <see cref="AbstractPhysicalObject.AbstractObjectType"/> exists, overrides the <see cref="Player.Grabability(PhysicalObject)"/> for that type.
	/// </summary>
	public static readonly ObjectGrabOverrides ObjectGrabOverrides = new();

	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to interact with various physical objects that have unique properties.
	/// </summary>
	public static readonly ObjectInteractions ObjectInteractions = new();

	/// <summary>
	/// Forces <see cref="SlugBaseCharacter"/> to be able to grab <see cref="Spear"/>s in their embedded state.
	/// </summary>
	public static readonly PullSpearsFromWalls PullSpearsFromWalls = new();

	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to visually have Rivulet's gills. The default <see cref="int"/> of <see cref="PlayerGraphics.gills"/> rows is 3.
	/// </summary>
	public static readonly RivuletGills RivuletGills = new();

	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to use Saint's tongue mechanic.
	/// </summary>
	public static readonly SaintTongue SaintTongue = new();

	/// <summary>
	/// If <see cref="SlugBaseCharacter"/> succeeds a random chance from 0-1, does not die from being hit by a spear.
	/// </summary>
	public static readonly SpearSaveChance SpearSaveChance = new();

	/// <summary>
	/// Forces <see cref="SlugBaseCharacter"/> to avoid water to avoid dying like Artificer.
	/// </summary>
	public static readonly SwimmingPenalty SwimmingPenalty = new();

	/// <summary>
	/// Forces <see cref="SlugBaseCharacter"/> to be unable to use <see cref="Spear"/>s, instead tossing them.
	/// </summary>
	public static readonly TossSpears TossSpears = new();

	[RequiresDLC(MSC: true)]
	/// <summary>
	/// Applies <see cref="PlayerGraphics.SaintFaceCondition"/> if true, forcing the face sprite to flip when it uses open and closed eyes.
	/// </summary>
	public static readonly UseSaintFace UseSaintFace = new();

	[RequiresDLC(MSC: true, Watcher: true, mutuallyExclusive: true)]
	/// <summary>
	/// Affects the <see cref="Creature.Hypothermia"/> gain and/or loss the <see cref="SlugBaseCharacter"/> has with a base warmth value.
	/// </summary>
	public static readonly Warmth Warmth = new();
}

public class TimelineFeatures
{
	/// <summary>
	/// Controls whether the rain timer dots should show in this timeline.
	/// </summary>
	public static readonly ShowRainTimer ShowRainTimer = new();

	/// <summary>
	/// Controls whether the shelter logic should close at the end of the cycle as long as the <see cref="Player"/>s are standing still.
	/// </summary>
	public static readonly EndOfCycleForcesSheltering EndOfCycleForcesSheltering = new();
}

public class ObsoleteFeatures
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	/// <summary>
	/// Replaces the default face sprite with Artificer's variant. <see cref="usesSaintFaceCondition"/> takes priority over this due to how <see cref="PlayerGraphics.SaintFaceCondition"/> runs.
	/// </summary>
	public static readonly PlayerFeature<bool> hasArtiFace = ObsoletePlayerFeature<bool>("arti_eyes", "is obsolete because SlugSprites exists.");

	[EditorBrowsable(EditorBrowsableState.Never)]
	/// <summary>
	/// Replaces the <see cref="PlayerGraphics"/> head sprite with Saint's fluffier variant.
	/// </summary>
	public static readonly PlayerFeature<bool> hasSaintHead = ObsoletePlayerFeature<bool>("saint_fluff", "is obsolete because SlugSprites exists.");

	[EditorBrowsable(EditorBrowsableState.Never)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to use <see cref="Player.FoodInStomach"/> to craft explosives, and the <see cref="int"/> cost in quarter intervals.
	/// </summary>
	public static readonly PlayerFeature<int> explosiveCraftCost = ObsoletePlayerFeature<int>("craft_explosives_cost", $"is now replaced with {ExtPlayerFeatures.CanCraftObjects.ID}.");

	[EditorBrowsable(EditorBrowsableState.Never)]
	/// <summary>
	/// Forces <see cref="SlugBaseCharacter"/> to use self-made <see cref="Spear"/>s to feed.
	/// </summary>
	public static readonly PlayerFeature<bool> forceFeedingFromSpears = ObsoletePlayerFeature<bool>("feeds_from_spears", $"is now merged into {ExtPlayerFeatures.CanCreateSpears.ID}.");

	[EditorBrowsable(EditorBrowsableState.Never)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to automatically pop <see cref="WaterNut"/> when held.
	/// </summary>
	public static readonly PlayerFeature<bool> popBubbleFruit = ObsoletePlayerFeature<bool>("pop_held_bubblefruit", $"is now implemented in {ExtPlayerFeatures.ObjectInteractions.ID}.");

	[EditorBrowsable(EditorBrowsableState.Never)]
	/// <summary>
	/// When above 0, uses the current <see cref="RoomPalette.blackColor"/> alongside the <see cref="SlugBaseCharacter"/>'s custom colors if present.
	/// </summary>
	public static readonly PlayerFeature<int> blackColorFade = ObsoletePlayerFeature<int>("use_blackcolor", $"has been merged into {PlayerFeatures.CustomColors.ID}.");

	[EditorBrowsable(EditorBrowsableState.Never)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to hold two <see cref="Spear"/>s.
	/// </summary>
	public static readonly PlayerFeature<bool> canDualWield = ObsoletePlayerFeature<bool>("can_dualwield", $"use {ExtPlayerFeatures.ObjectGrabOverrides.ID} to overwrite the type \"Spear\" to one handed instead.");

}
