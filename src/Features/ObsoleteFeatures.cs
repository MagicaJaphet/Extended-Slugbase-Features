using SlugBase.Features;
using static ExtendedSlugbase.Helpers.PlayerHelpers;
using System;
using ExtendedSlugbase.Helpers;
using System.ComponentModel;
using static ExtendedSlugbase.Helpers.FeatureHelpers;
namespace ExtendedSlugbase.Features
{
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
        public static readonly PlayerFeature<int> explosiveCraftCost = ObsoletePlayerFeature<int>("craft_explosives_cost", $"is now replaced with {PlayerFeaturesExt.canCraftObjects.ID}.");

        [EditorBrowsable(EditorBrowsableState.Never)]
        /// <summary>
        /// Forces <see cref="SlugBaseCharacter"/> to use self-made <see cref="Spear"/>s to feed.
        /// </summary>
        public static readonly PlayerFeature<bool> forceFeedingFromSpears = ObsoletePlayerFeature<bool>("feeds_from_spears", $"is now merged into {PlayerFeaturesExt.canCreateSpears.ID}.");

        [EditorBrowsable(EditorBrowsableState.Never)]
        /// <summary>
        /// Allows <see cref="SlugBaseCharacter"/> to automatically pop <see cref="WaterNut"/> when held.
        /// </summary>
        public static readonly PlayerFeature<bool> popBubbleFruit = ObsoletePlayerFeature<bool>("pop_held_bubblefruit", $"is now implemented in {PlayerFeaturesExt.objectInteractions.ID}.");

        
        [EditorBrowsable(EditorBrowsableState.Never)]
        /// <summary>
        /// When above 0, uses the current <see cref="RoomPalette.blackColor"/> alongside the <see cref="SlugBaseCharacter"/>'s custom colors if present.
        /// </summary>
        public static readonly PlayerFeature<int> blackColorFade = ObsoletePlayerFeature<int>("use_blackcolor", $"has been merged into {PlayerFeatures.CustomColors.ID}.");


        [EditorBrowsable(EditorBrowsableState.Never)]
        /// <summary>
        /// Allows <see cref="SlugBaseCharacter"/> to hold two <see cref="Spear"/>s.
        /// </summary>
        public static readonly PlayerFeature<bool> canDualWield = ObsoletePlayerFeature<bool>("can_dualwield", $"use {PlayerFeaturesExt.objectGrabability.ID} to overwrite the Spear to one handed instead.");

    }
}
