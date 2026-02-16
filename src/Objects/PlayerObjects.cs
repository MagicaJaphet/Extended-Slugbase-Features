using System.Collections.Generic;
using SlugBase;
using System;
using UnityEngine;
using ExtendedSlugbase.Helpers;
using RWCustom;
using System.Linq;
using IL.ScavengerCosmetic;
using MagicaHookingLibrary.Helpers;
using IL.Menu.Remix.MixedUI;
using MoreSlugcats;
using Menu;
using System.CodeDom;
using static ExtendedSlugbase.Objects.PlayerObjects;
using System.Dynamic;
using ExtendedSlugbase.Features;
using IL.Watcher;
using static ExtendedSlugbase.Objects.GameObjects;
using static ExtendedSlugbase.Helpers.AbstractHelpers;
using SlugBase.DataTypes;
using static ExtendedSlugbase.Objects.PlayerObjects.Craftability;
namespace ExtendedSlugbase.Objects
{
    public static class PlayerObjects
    {
        public class ExtColorSlot
        {
            public static Dictionary<ColorSlot, ExtColorSlot> ExtendedColorSlots { get; } = [];
            
            public IntVector2? DefaultPaletteIndex { get; internal set; }
            public IntVector2?[] VariantPaletteIndexes { get; internal set; }
            public Color? DefaultFade { get; }
            public IntVector2? DefaultFadePaletteIndex { get; }
            public Color?[] VariantFades { get; }
            public IntVector2?[] VariantFadePaletteIndexes { get; }
			public Vector2 FadeVariance { get; } = new(0.08f, 0.04f);

            public bool TryGetPalKey(out IntVector2 key, int? variant = null)
            {
                key = default;
                if (variant is int v && VariantPaletteIndexes.Length > v
                    && VariantPaletteIndexes[v] is IntVector2 arenaCol)
                {
                    key = arenaCol;
                    return true;
                }
                else if (DefaultFadePaletteIndex is IntVector2 col)
                {
                    key = col;
                    return true;
                }
                return false;
            }
            
            public bool TryGetFadeColor(out Color color, int? variant = null)
            {
                color = default;
                if (variant is int v && VariantFades.Length > v 
                    && VariantFades[v] is Color arenaCol)
                {
                    color = arenaCol;
                    return true;
                }
                else if (DefaultFade is Color col)
                {
                    color = col;
                    return true;
                }
                return false;
            }

            public bool TryGetFadePalKey(out IntVector2 key, int? variant = null)
            {
                key = default;
                if (variant is int v && VariantFadePaletteIndexes.Length > v
                    && VariantFadePaletteIndexes[v] is IntVector2 arenaCol)
                {
                    key = arenaCol;
                    return true;
                }
                else if (DefaultFadePaletteIndex is IntVector2 col)
                {
                    key = col;
                    return true;
                }
                return false;
            }

            public float LerpThresholds(float lerp)
            {
                if (FadeVariance == null)
                {
                    return 1f;
                }
                return Mathf.Lerp(FadeVariance.x, FadeVariance.y, lerp);
            }


            public void ParseColor(JsonAny json, out Color? col, out IntVector2? pal)
            {
                col = null;
                pal = null;

                if (json.TryParse(out Color color, throwIfParseError: false))
                {
                    col = color;
                    return;
                }
                if (json.TryParse(out IntVector2 palette, throwIfParseError: false))
                {
                    pal = palette;
                    return;
                }
                throw new JsonException("Value is not a valid Color or IntVector2!", json);
            }

            public ExtColorSlot(JsonObject json)
            {
                if (json.TryGet("story_fade", out JsonAny any))
                {
                    ParseColor(any, out var fadeCol, out var fadePal);
                    DefaultFade = fadeCol;
                    DefaultFadePaletteIndex = fadePal;
                }
                if (json.TryGet("arena_fade", out JsonList list))
                {
                    Color?[] arenaColors = new Color?[list.Count];
                    IntVector2?[] arenaPalettes = new IntVector2?[list.Count];

                    for (int i = 0; i < list.Count; i++)
                    {
                        var item = list[i];
                        ParseColor(item, out var arenaFadeCol, out var arenaFadePal);
                        arenaColors[i] = arenaFadeCol ?? default;
                        arenaPalettes[i] = arenaFadePal;
                    }

                    VariantFades = arenaColors; 
                    VariantFadePaletteIndexes = arenaPalettes;
                }
                if (json.TryGet("darkness_variance", out Vector2 fades))
                {
                    FadeVariance = fades;
                }
            }
        }

        /// <summary>
        /// An object which holds information about which objects a <see cref="Player"/> can hold.
        /// </summary>
        public class Grabability
        {
            public Dictionary<AbstractPhysicalObject.AbstractObjectType, Player.ObjectGrabability> ObjectOverrides { get; } = [];
            public Dictionary<CreatureTemplate.Type, Player.ObjectGrabability> CreatureOverrides { get; } = [];

            internal Grabability(JsonAny json)
            {
                foreach ((string key, JsonAny value) in json.AsObject().GetKeyPairEnumerator())
                {
                    var grabby = value.AsEnum<Player.ObjectGrabability>();
                    bool foundValue = false;
                    if (ExtEnumHelpers.TryGetExtEnum<AbstractPhysicalObject.AbstractObjectType>(key, out var objType))
                    {
                        ObjectOverrides[objType] = grabby;
                        foundValue = true;
                    }
                    if (ExtEnumHelpers.TryGetExtEnum<CreatureTemplate.Type>(key, out var type))
                    {
                        CreatureOverrides[type] = grabby;
                        foundValue = true;
                    }

                    if (!foundValue)
                    {
                        throw new JsonException($"{key} is not a valid AbstractObjectType or CreatureTemplate.Type!", value);
                    }
                }
            }
        }

        public class CreatureRelationship
        {
            public Dictionary<CreatureTemplate.Type, CreatureTemplate.Relationship> relationshipOverrides = [];

            public bool TryGetRelationship(CreatureTemplate.Type type, out CreatureTemplate.Relationship relationship)
            {
                relationship = default;
                if (relationshipOverrides.TryGetValue(type, out relationship))
                {
                    return true;
                }
                return false;
            }

            public CreatureRelationship(JsonAny json)
            {
                foreach((string key, JsonAny any) in json.AsObject().GetKeyPairEnumerator())
                {
                    if (ExtEnumHelpers.TryGetExtEnum(key, out CreatureTemplate.Type crtType))
                    {
                        (string type, float intensity) = any.AsObject().Select(x => (x.Key, x.Value.AsFloat())).FirstOrDefault();
                        if (ExtEnumHelpers.TryGetExtEnum(type, out CreatureTemplate.Relationship.Type relation))
                        {
                            relationshipOverrides.Add(crtType, new(relation, intensity));
                        }
                        else
                        {
                            throw new JsonException($"{key} is not a value of CreatureTemplate.Relationship.Type!", any);
                        }
                    }
                    else
                    {
                        throw new JsonException($"{key} is not a value of CreatureTemplate.Type!", any);
                    }
                }
            }
        }

        public class PlayerGills
        {
            public int Rows { get; } = 3;
            public float Bounciness { get; } = 1f;
            public float Drag { get; } = 1f;
            public float Spread { get; } = 0.65f;

            public float? Length { get; }
            public float? Width { get; }

            // public string[] SpriteElements { get; }

            internal PlayerGills(JsonAny json)
            {
                if (json.TryParse(out JsonObject obj))
                {
                    if (obj.TryGet("rows", out int rows))
                    {
                        Rows = rows;
                    }
                    if (obj.TryGet("bounciness", out float bounciness))
                    {
                        Bounciness = bounciness;
                    }
                    if (obj.TryGet("drag", out float drag))
                    {
                        Drag = drag;
                    }
                    if (obj.TryGet("length", out float length))
                    {
                        Length = length;
                    }
                    if (obj.TryGet("width", out float width))
                    {
                        Width = width;
                    }
                    if (obj.TryGet("spread", out float spread))
                    {
                        Spread = spread;
                    }
                    if (obj.TryGet("element_names", out string[] names))
                    {
                        //FEATURE: JSON atlas/element loader handler
                        // foreach (var name in names)
                        // {
                        //     if (Futile.atlasManager.GetElementWithName(name) == null)
                        //     {
                        //         throw new JsonException($"{name} is not a valid element! Make sure your sprite is loaded.", obj);
                        //     }
                        // }
                        // SpriteElements = names;
                    }
                }
            }
        }

        public class PlayerTongue
        {
            public float Length { get; } = 150f;
            public float Thickness { get; } = 1f;
            public bool Retractable { get; } = true;
            public float[] RetractLengths { get; } = [ 50f, 170f ];
            public int Segments { get; } = 20;
            public float RetractSpeed { get; } = 1f;


            internal PlayerTongue(JsonAny json)
            {
                if (json.TryParse(out JsonObject obj))
                {
                    if (obj.TryGet("segments", out int segs))
                    {
                        Segments = segs;
                    }
                    if (obj.TryGet("length", out float length))
                    {
                        Length = length;
                    }
                    if (obj.TryGet("retract_lengths", out float[] lengths, 2, 2))
                    {
                        RetractLengths = lengths;
                    }
                    if (obj.TryGet("retract_speed", out float retractSpeed))
                    {
                        RetractSpeed = retractSpeed;
                    }
                    if (obj.TryGet("retractable", out bool retractable))
                    {
                        Retractable = retractable;
                    }
                }
            }
        }

        public class Craftability
        {
            public bool EatsMeals { get; } = false;
            public int MealBonus { get; } = 1;

            public Dictionary<Ingredient, Dictionary<Ingredient, AbstractObject>> TwoHandedRecipeTable { get; } = [];
            public Dictionary<Ingredient, (int cost, AbstractObject result)> OneHandedRecipeList { get; } = [];
            public Dictionary<Ingredient, (int cost, AbstractObject result)> SwallowRecipeList { get; } = [];
            public Regurgitatable RegurgitateList { get; } = new();
            public SoundID CraftSound { get; } = SoundID.Slugcat_Swallow_Item;


            public class Regurgitatable
            {
                public int cost = 1;
                public List<(AbstractObject obj, float rarity)> objects = [];
            }

            public class Ingredient
            {
                public AbstractPhysicalObject.AbstractObjectType objType;
                public CreatureTemplate.Type critType;

                public Ingredient(PhysicalObject obj)
                {
                    if (obj is Creature crit)
                    {
                        critType = crit.Template.type;
                    }
                    objType = obj?.abstractPhysicalObject.type;
                }

                public Ingredient(string str, JsonAny any)
                {
                    if (!ExtEnumHelpers.TryGetExtEnum(str, out objType) && !ExtEnumHelpers.TryGetExtEnum(str, out critType))
                    {
                        throw new JsonException($"{str} is not a valid AbstractObjectType or CreatureTemplate.Type!", any);
                    }
                }

                public override bool Equals(object obj)
                {
                    if (obj is Ingredient b)
                    {
                        return (critType != null && b.critType != null && critType == b.critType) || (objType != null && b.objType != null && objType == b.objType);
                    }
                    return base.Equals(obj);
                }

                public override int GetHashCode()
                {
                    return base.GetHashCode();
                }
            }

            public bool TryGetOneHandedRecipe(Player player, AbstractPhysicalObject testObj, Ingredient test, out AbstractPhysicalObject resultObj, bool swallowRecipe = false)
            {
                resultObj = null;
                if (test == null)
                {
                    return false;
                }

                foreach((Ingredient key, (int cost, AbstractObject result)) in (swallowRecipe ? SwallowRecipeList : OneHandedRecipeList).Select(x => (x.Key, x.Value)))
                {
                    if (key.Equals(test))
                    {
                        if (player.FoodInStomach >= cost)
                        {
                            //BUG: Abstract object will stop spawning after a while
                            if (result.TryGetObject(player.room.abstractRoom, player.room.GetWorldCoordinate(player.firstChunk.pos), out resultObj))
                            {
                                if ((resultObj is AbstractCreature crit && crit.creatureTemplate.type == key.critType) || (resultObj.type == key.objType && AbstractObjectsAreEqual(resultObj, testObj)))
                                {
                                    return false;
                                }
                                player.SubtractFood(cost);
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            private bool AbstractObjectsAreEqual(AbstractPhysicalObject a, AbstractPhysicalObject b)
            {
                if (a is AbstractSpear spearA && b is AbstractSpear spearB)
                {
                    return (spearA.explosive == spearB.explosive) 
                    && (spearA.electric == spearB.electric)
                    && (spearA.hue == spearB.hue)
                    && (spearA.poison == spearB.poison);
                }
                return true;
            }


            public bool TryGetRecipeResult(Player player, out bool isOneHanded, out AbstractPhysicalObject obj, out Creature.Grasp chosen)
            {
                chosen = null;
                isOneHanded =  true;
                obj = null;
                Creature.Grasp a = player.grasps[0];
                Creature.Grasp b = player.grasps[1];

                if (a == null && b == null)
                {
                    return false;
                }
                Ingredient ingredientA = a != null ? new(a.grabbed) : null;
                Ingredient ingredientB = b != null ? new(b.grabbed) : null;

                if (OneHandedRecipeList.Count > 0)
                {
                    if (a != null && TryGetOneHandedRecipe(player, a.grabbed.abstractPhysicalObject, ingredientA, out obj))
                    {
                        chosen = a;
                        return true;
                    }
                    if (b != null && TryGetOneHandedRecipe(player, b.grabbed.abstractPhysicalObject, ingredientB, out obj))
                    {
                        chosen = b;
                        return true;
                    }
                }

                // Two handed recipe table
                if (ingredientA == null || ingredientB == null)
                {
                    // Don't calculate two handed recipe if one is null
                    return false;
                }

                isOneHanded = false;

                foreach((Ingredient key, var dict) in TwoHandedRecipeTable.Select(x => (x.Key, x.Value)))
                {
                    if (key.Equals(ingredientA))
                    {
                        foreach((Ingredient secondKey, AbstractObject result) in TwoHandedRecipeTable[key].Select(x => (x.Key, x.Value)))
                        {
                            if (secondKey.Equals(ingredientB) && result.TryGetObject(player.room.abstractRoom, player.room.GetWorldCoordinate(player.firstChunk.pos), out obj))
                            {
                                return true;
                            }
                        }
                    }
                }

                if (EatsMeals && GourmandCombos.CraftingResults_ObjectData(a, b, true) == AbstractPhysicalObject.AbstractObjectType.DangleFruit
                && a?.grabbed is IPlayerEdible edible && SlugcatStats.NourishmentOfObjectEaten(player.SlugCatClass, edible) != -1 
                && b?.grabbed is IPlayerEdible edible2 && SlugcatStats.NourishmentOfObjectEaten(player.SlugCatClass, edible2) != -1)
                {
                    return true;
                }

                return false;
            }

            public bool TryGetRegurgitate(Player player, Regurgitatable r, out AbstractPhysicalObject obj)
            {
                obj = null;
                if (r.objects.Count > 0)
                {
                    var random = UnityEngine.Random.value;
                    var validItemsByRarity = r.objects.Where(x => x.rarity > random).ToList();
                    if (validItemsByRarity[UnityEngine.Random.Range(0, validItemsByRarity.Count - 1)].obj.TryGetObject(player.room.abstractRoom, player.room.GetWorldCoordinate(player.firstChunk.pos), out obj))
                    {
                        return true;
                    }
                }
                return false;
            }
            internal Craftability(JsonAny json)
            {
                if (json.TryParse(out JsonObject obj))
                {
                    if (obj.TryGet("can_eat_meals", out bool eatsMeals))
                    {
                        EatsMeals = eatsMeals;
                    }
                    if (obj.TryGet("meal_bonus", out int bonus))
                    {
                        MealBonus = bonus;
                    }
                    if (obj.TryGet("two_handed_recipes", out JsonObject twoHandedRecipeList))
                    {
                        foreach ((string mainItem, JsonObject ingredientList) in twoHandedRecipeList.GetKeyPairEnumerator().Select(x =>(x.key, x.value.AsObject())))
                        {
                            Ingredient mainIngredient = new(mainItem, twoHandedRecipeList);
                            TwoHandedRecipeTable[mainIngredient] = [];
                            
                            foreach ((string secondItem, JsonAny any) in ingredientList.GetKeyPairEnumerator())
                            {
                                Ingredient secondIngredient = new(secondItem, any);
                                AbstractObject result = new(any);
                                TwoHandedRecipeTable[mainIngredient][secondIngredient] = result;

                                // Also add the opposite
                                TwoHandedRecipeTable[secondIngredient] = [];
                                TwoHandedRecipeTable[secondIngredient][mainIngredient] = result;
                            }
                        }
                    }
                    if (obj.TryGet("craft_sound_id", out SoundID craftSound))
                    {
                        CraftSound = craftSound;
                    }
                    if (obj.TryGet("one_handed_recipes", out JsonObject oneHandedRecipeList))
                    {
                        foreach ((string mainItem, JsonObject ingredientList) in oneHandedRecipeList.GetKeyPairEnumerator().Select(x =>(x.key, x.value.AsObject())))
                        {
                            Ingredient mainIngredient = new(mainItem, oneHandedRecipeList);
                            OneHandedRecipeList[mainIngredient] = (ingredientList.Get("cost").AsInt(), new(ingredientList.Get("result")));
                        }
                    }
                    if (obj.TryGet("swallow_recipes", out JsonObject swallowRecipeList))
                    {
                        foreach ((string mainItem, JsonObject ingredientList) in swallowRecipeList.GetKeyPairEnumerator().Select(x =>(x.key, x.value.AsObject())))
                        {
                            Ingredient mainIngredient = new(mainItem, swallowRecipeList);
                            SwallowRecipeList[mainIngredient] = (ingredientList.Get("cost").AsInt(), new(ingredientList.Get("result")));
                        }
                    }
                    if (obj.TryGet("regurgitate_list", out JsonObject regObj))
                    {
                        RegurgitateList.cost = regObj.GetInt("cost");
                        RegurgitateList.objects = [.. from i in regObj.GetList("item_pool") let iObj = i.AsObject() select (new AbstractObject(iObj.Get("object")), iObj.Get("rarity").AsFloat())];
                    }
                }
            }
        }

        public class SpearCreatability
        {
            public IntVector2 Rows { get; } = new(5,3);
            public bool FeedFromSpears { get; } = true;
            public float GenerationSpeed { get; } = 0.05f;
            public SoundID PullSound { get; } = MoreSlugcatsEnums.MSCSoundID.SM_Spear_Pull;
            public SoundID SnapSound { get; } = MoreSlugcatsEnums.MSCSoundID.SM_Spear_Grab;
            public AbstractObject SpearObj { get; } // Null is fine
            public bool ReactsToSpears { get; } = true;
            public bool Dripping { get; } = true;
            public int FoodCost { get; }

            internal SpearCreatability(JsonAny json)
            {
                //lATER: God can't help me now x2
                /*	SPEAR CREATING:
                    SPRITE ELEMENT: what sprite are we using for the specks?
                    POSITIONING: maybe how do the specks lay on the tail? (the most complex part tbh)
                */

                if (json.TryParse(out JsonObject obj))
                {
                    if (obj.TryGet("speckle_amounts", out IntVector2 rows))
                    {
                        Rows = rows;
                    }
                    if (obj.TryGet("feeds_from_spears", out bool feedFromSpears))
                    {
                        FeedFromSpears = feedFromSpears;
                    }
                    if (obj.TryGet("generation_speed", out float speed))
                    {
                        GenerationSpeed = speed;
                    }
                    if(obj.TryGet("food_cost", out int cost))
                    {
                        FoodCost = cost;
                    }
                    if (obj.TryGet("head_shaking", out bool squint))
                    {
                        ReactsToSpears = squint;
                    }
                    if (obj.TryGet("pull_sound_id", out SoundID pullID))
                    {
                        PullSound = pullID;
                    }
                    if (obj.TryGet("snap_sound_id", out SoundID snapID))
                    {
                        SnapSound = snapID;
                    }
                    if (obj.TryGet("dripping", out bool drip))
                    {
                        Dripping = drip;
                    }
                    if (obj.TryGet("spear_template", out JsonAny abstractObj))
                    {
                        SpearObj = new AbstractObject(abstractObj);
                    }
                }
            }
        }

        public class ObjectInteractions
        {
            public static float? lastAteMushroomFPS;
            public bool PopBubbleFruit { get; } = false;
            public float BubbleWeedUsageMultiplier { get; } = 1f;
            public bool ExplosiveImmune { get; } = false;
            public int MushroomTimer { get; } = 320;
            public float MushroomFPS { get; } = 15f;
            public bool PoisonImmune { get; } = false;

            internal ObjectInteractions(JsonAny json)
            {
                //LATER: God can't help me now x3
                /*	
                    POPCORN: pop when standing near
                    SPOREPUFF: stun time from exposure, affected by amounts with possibility of death
                    BATNIP: Idk for this one honestly lol
                    ACID: Immunities to touching / swimming in it
                */

                if (json.TryParse(out JsonObject obj))
                {
                    if (obj.TryGet("pop_bubble_fruit", out bool popBubbleFruit))
                    {
                        PopBubbleFruit = popBubbleFruit;
                    }
                    if (obj.TryGet("bubble_weed_usage_multiplier", out float usageMultiplier))
                    {
                        BubbleWeedUsageMultiplier = usageMultiplier;
                    }
                    if(obj.TryGet("poison_immune", out bool poisonImmune))
                    {
                        PoisonImmune = poisonImmune;
                    }
                    if (obj.TryGet("explosive_immune", out bool explosiveImmune))
                    {
                        ExplosiveImmune = explosiveImmune;
                    }
                    if (obj.TryGet("mushroom_interactions", out JsonObject mushroom))
                    {
                        if (mushroom.TryGet("timer", out int timer))
                        {
                            MushroomTimer = timer;
                        }
                        if (mushroom.TryGet("frames_per_second", out int framesPerSecond))
                        {
                            MushroomFPS = framesPerSecond;
                        }
                    }
                }
            }
        }

        public class DoubleJump
        {
            public int[] JumpLimit { get; } = [7, 10];
            public SoundID JumpSoundID { get; } = SoundID.Fire_Spear_Explode;
            public LimitResult LimitReachedResult { get; } = LimitResult.Die;
            public bool Parry { get; } = true;
            public int FoodCost { get; } = 0;
            public float[] JumpBoost { get; } = [8f];
            public int[] StunTimers { get; } = [60];
			public bool JumpEffect { get; }

			public enum LimitResult
            {
                LongStun,
                Die, // Like artificer
                ConsumeFood // Like the Wanderer
            }

            internal DoubleJump(JsonAny json)
            {
                /*	EXPLOSIVE JUMPS
                    EFFECT: the type of effect that plays on the jump
                    LOOPING EFFECT: The visual that continuously plays when the soft limit is reached
                    LIMIT EFFECT: the visual effect that plays when the slugcat is exhausted
                    PARRY EFFECT: The visual that plays when parrying
                */

                //LATER: Implement some sort of abstract helper for spawning custom effects for all use cases

                if (json.TryParse(out JsonObject obj))
                {
                    if (obj.TryGet("jump_speed", out float[] jumpBoost))
                    {
                        JumpBoost = jumpBoost;
                    }
                    if (obj.TryGet("jump_sound_id", out SoundID soundID))
                    {
                        JumpSoundID = soundID;
                    }
                    if (obj.TryGet("limits", out int[] limits, 1, 2))
                    {
                        JumpLimit = limits;
                    }
                    if (obj.TryGet("limit_reached_result", out LimitResult effect))
                    {
                        LimitReachedResult = effect;
                    }
                    if (obj.TryGet("stun_timers", out int[] stunTimers, 1, 2))
                    {
                        StunTimers = stunTimers;
                    }
                    if (obj.TryGet("food_cost", out int cost))
                    {
                        FoodCost = cost;
                    }
                    if (obj.TryGet("parry", out bool parry))
                    {
                        Parry = parry;
                    }
					if (obj.TryGet("jump_effect", out bool jumpEffect))
					{
						//LATER: Replace
						JumpEffect = jumpEffect;
					}
                }
            }
        }
    }
}
