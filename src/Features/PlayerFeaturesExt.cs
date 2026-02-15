using System.Collections.Generic;
using ExtendedSlugbase.Helpers;
using MagicaHookingLibrary.Helpers;
using SlugBase;
using SlugBase.Features;
using static ExtendedSlugbase.Helpers.FeatureHelpers;
using static ExtendedSlugbase.Objects.PlayerObjects;

namespace ExtendedSlugbase.Features;
public class PlayerFeaturesExt
{
	//FEATURE: Food that stuns or kills if it gives negative pips
	//FEATURE: Slugpup
	//FEATURE: Saint ascension (toggleable flight)
	//FEATURE: Modify movement bonuses/penalties (land and water)
	//FEATURE: Watcher invisibility
	//FEATURE: expedition perk slow time
	//FEATURE: expedition burdens in campaign
	//FEATURE: Configure player grasps (and add sprites)
	//FEATURE: Expedition menu scene art
	//FEATURE: Toggle passage progression and passage button 		
	// Menu.SleepAndDeathScreen.AddPassageButton(bool) : void @06005C19
	// Menu.SleepAndDeathScreen.GetDataFromGame(KarmaLadderScreen.SleepDeathScreenDataPackage) : void @06005C1C

	//FEATURE: Default hidden or unplayable toggle
	//FEATURE: DMS support for assets with sprites
	//FEATURE: Spear immunity chances
	//FEATURE: Embedded pearl
	//FEATURE: Jolly icon / pup variant
	//FEATURE: Cycle limit death main menu art / results screen
	//FEATURE: Lock ascension (no continue)
	//FEATURE: Guide overseer region priority (with room priorities)
	//FEATURE: Guide overseer icon
	//FEATURE: Progression locked slugcat (ex: have to beat hunter to play)
	//FEATURE: Lock abilities behind a check, with the ability to add tutorial text when first encountering the ability to use them
	//FEATURE: Slugcat fucking explodes when trying to swim
	//FEATURE: Mauling side effects
	//FEATURE: Weaver cosmetic toggles
	//FEATURE: Inability to starve
	//FEATURE: Spear damage random save chance (like gourmand)
	//	Spear.HitSomething

	[RequiresDLC(MSC:true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to use Saint's tongue mechanic.
	/// </summary>
	public static readonly PlayerFeature<PlayerTongue> saintTongue = new("grapple_tongue", json => new PlayerTongue(json));

	/// <summary>
	/// If a valid <see cref="AbstractPhysicalObject.AbstractObjectType"/> exists, overrides the <see cref="Player.Grabability(PhysicalObject)"/> for that type.
	/// </summary>
	public static readonly PlayerFeature<Grabability> objectGrabability = new("grab_overrides", json => new Grabability(json));

	/// <summary>
	/// Changes the default <see cref="Player.DeathByBiteMultiplier"/> value, with the second value altering the difficulty.
	/// </summary>
	public static readonly PlayerFeature<float[]> deathByBiteMultiplier = FeatureTypes.PlayerFloats("bite_lethality_mutliplier", 1, 2);

	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to interact with various physical objects that have unique properties.
	/// </summary>
	public static readonly PlayerFeature<ObjectInteractions> objectInteractions = new("object_interactions", json => new ObjectInteractions(json));

	[RequiresDLC(MSC:true, Watcher:true, mutuallyExclusive: true)]
	/// <summary>
	/// Affects the <see cref="Creature.Hypothermia"/> gain and/or loss the <see cref="SlugBaseCharacter"/> has with a base warmth value.
	/// </summary>
	public static readonly PlayerFeature<float> warmth = FeatureTypes.PlayerFloat("body_warmth");

	/// <summary>
	/// Forces <see cref="SlugBaseCharacter"/> to be unable to use <see cref="Spear"/>s, instead tossing them.
	/// </summary>
	public static readonly PlayerFeature<bool> tossSpears = FeatureTypes.PlayerBool("only_tosses_spears");

	//TODO: Document
	/// <summary>
	/// Forces <see cref="SlugBaseCharacter"/> to hold onto items when stunned for any reason.
	/// </summary>
	public static readonly PlayerFeature<Dictionary<Creature.DamageType, bool>> noStunGraspPenalty = new("no_stun_grasp_penalty", json => {
		Dictionary<Creature.DamageType, bool> stunOverrides = [];
		foreach ((string key, JsonAny value) in json.AsObject().GetKeyPairEnumerator())
		{
			if (ExtEnumHelpers.TryGetExtEnum(key, out Creature.DamageType damage))
			{
				stunOverrides.Add(damage, value.AsBool());
			}
			else
			{
				throw new JsonException($"{key} is not a value of Creature.DamageType!", value);
			}
		}
		return stunOverrides;
	});

	/// <summary>
	/// Forces <see cref="SlugBaseCharacter"/> to be able to grab <see cref="Spear"/>s in their embedded state.
	/// </summary>
	public static readonly PlayerFeature<bool> pullSpearsFromWalls = FeatureTypes.PlayerBool("take_spears_from_wall");

	[RequiresDLC(MSC:true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to use Artificer's explosive jump ability, setting the soft and hard limits in <see cref="int"/>s.
	/// </summary>
	public static readonly PlayerFeature<DoubleJump> doubleJump = new("explosive_jump", json => new DoubleJump(json));

	[RequiresDLC(MSC:true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to visually have Rivulet's gills. The default <see cref="int"/> of <see cref="PlayerGraphics.gills"/> rows is 3.
	/// </summary>
	public static readonly PlayerFeature<PlayerGills> rivGills = new("gills", json => new PlayerGills(json));

	[RequiresDLC(MSC:true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to spawn <see cref="Spear"/>s from <see cref="PlayerGraphics.tailSpecks"/>. The default <see cref="int"/> of <see cref="PlayerGraphics.tailSpecks"/> is 3 and 5.
	/// </summary>
	public static readonly PlayerFeature<SpearCreatability> canCreateSpears = new("spear_specks", json => new SpearCreatability(json));

	[RequiresDLC(MSC:true)]
	/// <summary>
	/// A vertsitile crafting table that allows <see cref="SlugBaseCharacter"/> to turn anything tangible into something else.
	/// </summary>
	public static readonly PlayerFeature<Craftability> canCraftObjects = new("crafting", json => new Craftability(json));

	[RequiresDLC(MSC:true)]
	/// <summary>
	/// Applies <see cref="PlayerGraphics.SaintFaceCondition"/> if true, forcing the face sprite to flip when it uses open and closed eyes.
	/// </summary>
	public static readonly PlayerFeature<bool> usesSaintFaceCondition = FeatureTypes.PlayerBool("saint_eyes");

	[RequiresDLC(MSC:true)]
	/// <summary>
	/// Allows <see cref="SlugBaseCharacter"/> to inflict damage when hitting a <see cref="Creature"/> with momentum.
	/// </summary>
	public static readonly PlayerFeature<bool> canSlam = FeatureTypes.PlayerBool("can_slam");

	// LATER: Document
	[RequiresDLC(MSC:true)]
	// LATER: Implement
	/// <summary>
	/// Forces <see cref="SlugBaseCharacter"/> to rest if they exhibit too much physical activity, with the first <see cref="int"/> declaring the exhaustion time, and an optional 2nd <see cref="int"/> for how much activity causes exhaustion.
	/// </summary>
	public static readonly PlayerFeature<int[]> exhaustion = FeatureTypes.PlayerInts("exhaustion", 1, 2);

	/// <summary>
	/// Disallows <see cref="SlugBaseCharacter"/> from swallowing or regurgitating objects if true.
	/// </summary>
	public static readonly PlayerFeature<bool> cantSwallowObjects = FeatureTypes.PlayerBool("cant_swallow_objects");
}
