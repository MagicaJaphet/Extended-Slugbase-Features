# Assets
## Modified Assets
### Custom Scenes
- Compatibiliity with [Extended MenuScenes](https://steamcommunity.com/sharedfiles/filedetails/?id=3666470764) has been added.
    - Images now support ``"image_color"`` and ``"color_opacity"`` fields.
        - ``"image_color"``: When set to an ``int`` or ``string``, will attempt to attach itself to a valid ``"custom_color"`` slot for the current slugcat, allowing dynamic color updating. When set to a ``color``, will apply a static color.
        - ``"color_opacity"``: A ``float<0-1>`` value that affects the amount of the ``"image_color"`` that is applied to the image. 
        - The color is applied using a multiply onto the image. Supports depth shaders as well thanks to custom shaders by Haizlbliek.

# Features
These are JSON key value pairs that goes into the ``features: {}`` of your slugcat's main JSON file. Examples will be provided of the correct format, for additional help it's reccommended to write in a program or site that checks JSON formatting.

Features with ``<optional>`` keys have their default values in their examples. Some values may have a minimum or maximum value. If so, they are appropriately marked by ``<min-max>``.

## New JSON Types
- ``<abstract object>``: A JSON object which contains a string and object dictionary to parse into a type that inherits ``AbstractPhysicalObject``. Will call the class's constructor when attempting to spawn the object. Works very similarly to Dev Console's ``raw_spawn`` command, with allowing additional fields and properties to be set if they are accessible. In simplier terms, accepts a new class instance to create a customizable object or creature.
```JSON
"<AbstractPhysicalObject>": {
    "<parameter name>": <value>,
    "<field name>" : <value>,
    "<property name>" : <value>
}
```

## ExtEnum / Enum Values
For ``CreatureTemplate.Type`` or ``AbstractPhysicalObject.Type`` see [Slugbase's feature page](https://slimecubed.github.io/slugbase/articles/features.html). Because ``abstract object`` theoretically may require any ExtEnum or Enum type, only those that are specified in the features section will be listed here.
### CreatureTemplate.Relationship.Type
- Vanilla: ``DoesntTrack``, ``Ignores``, ``Eats``, ``Afraid``, ``StayOutOfWay``, ``AgressiveRival``, ``Attacks``, ``Uncomfortable``, ``Antagonizes``, ``PlaysWith``, ``SocialDependent``, ``Pack``

### Creature.DamageType
- Vanilla: ``Blunt``, ``Stab``, ``Bite``, ``Water``, ``Explosion``, ``Electric``, ``None``

### LimitResult
- Extended Slugbase: ``LongStun``, ``Die``, ``ConsumeFood``

### ObjectGrabability
- Vanilla: ``CantGrab``, ``OneHand``, ``BigOneHand``, ``TwoHands``, ``Drag``

### SoundID
Way too many to list, new SoundIDs are also relatively easy to add. I would reccommend decompiling the code and looking at the list yourself.


## Modified Features
Some base features from Slugbase have slight alterations. Only the new information will be present, but a link to each feature's original documentation will be linked.

### ["custom_colors"](https://slimecubed.github.io/slugbase/articles/features.html#custom_colors)
- Color behavior have been modified.
  - ``"story"`` and ``"arena"`` now accept ``int[2]`` values, in the case of these being present, the game will attempt to use the room palette's corrosponding color for that index. For example, ``[2, 0]`` corrosponds on the palette key to the palette's black color.
    -  When not in game, the palette defaults to the outskirt's.
    - An interesting side effect of this is indexes between ``x:[28-31]`` and ``y:[2-15]`` corrospond to effect color keys when in game due to the room camera writing the effect colors to the palette in-game. When outside of an active game, the color defaults to white. For reference for these, I would reccommend looking at the effect color png instead of the palette key.
    - PS: Most rooms without plants or signs don't have an assigned effect color, which defaults to ``0`` or pink. If you want region consistency, you'll have to modify room settings yourself.
- Watcher's black color effect now replaces the default black color behavior. Because pure black equates to transparency in-game, setting the color to pure black will now default the color to a palette's black color instead.
- Watcher's blue color fade feature is directly integrated into the color slots.
    - ``"story_fade"``: Accepts the same values as other slots, but now only appears when a room palette's darkness effect is above ``0``.
    - ``"arena_fade"``: Accepts the same values, but in a list like ``"arena"``.
    - ``"darkness_variance"``: A ``float[2]`` value that affects how the fade colors are used when room darkness is or is not present. The first value is at no darkness, the second is at max darkness.
        - Ex: ``"darkness_variance": [0.08, 0.04]``

## Features
### "ignore_dlc_errors"
``bool``\
Ex: ``"ignore_dlc_errors": true``

When true, will not throw a DLC JSON exception when the required DLC is not enabled. This does not circumvent DLC checks when performing a DLC check. Ensure that your mod's dependencies reflect the required DLCs you need for the feature list of your slugcat if this is false.

## Game Features
### "limited_cycles"
```JSON
{
	<optional>"cycles": int,
    <optional>"death_menu_scene": "<MenuScene.SceneID>"
}
```
Ex:
```JSON
"limited_cycles": {
	"cycles": 20,
    "death_menu_scene": "Slugcat_Dead_Red"
}
```
Limits the slugcat's cycles to the set amount. When reaching the last cycle, forces harder gameplay and death beyond this point causes permadeath.

<hr>

### "creature_relationships"
```JSON
{
	"<CreatureTemplate.Type>": { "<CreatureTemplate.Relationship.Type>" : float<0-1>, }
}
```
Ex:
```JSON
"creature_relationships": {
	"LanternMouse": { "Ignore" : 1, }
}
```
Overrides static relationships of creatures with the players in the slugcat's game, creatures with reputations are mostly unaffected by these values. The float value represents the intensity of the relationship.

<hr>

### "overseer_overwrite"
```JSON
[
	{
		"owner": int,
		"color": color,
	}
]
```
Ex:
```JSON
"overseer_overwrite": [
	{
		"owner": 1,
		"color": "FF0000",
	}
]
```
Overrides any number with a new color when checking the ownerIterator value of an overseer. ["guide_overseer"](https://slimecubed.github.io/slugbase/articles/features.html#guide_overseer) can also be set to any number, and when used in conjunction allows unique overseers to co-exist with existing ones.

<hr>

### "get_karma_from_scavs" [MSC]
``bool``\
Ex: ``"get_karma_from_scavs": true``

Allows the player to gain karma to pass through gates temporarily while holding Scavenger corpses.

<hr>

### "ghost_pings" [MSC]
``bool``\
Ex: ``"ghost_pings": true``

When true, as long as a valid Echo exists within a region, flashes the Saint Echo ping effect at the start of every cycle or after crossing a gate.

<hr>

### "can_pass_OE_gate" [MSC]
``bool[1..2]``\
Ex: ``"can_pass_OE_gate": true``, ``"can_pass_OE_gate": [true, false]``

Controls whether the slugcat can normally pass the OE gate unrestricted. The second bool determines whether the game should check if Gourmand has been beaten first.

<hr>

### "max_slugpup_spawns" [MSC]
``int``\
Ex: ``"max_slugpup_spawns": 9999``

When set, allows slugpups to spawn naturally in the slugcat's campaign. The number corresponds to how many slugpups are allowed to exist at the same time in the campaign.

<hr>

### "spawn_karma_flowers"
``bool``\
Ex: ``"spawn_karma_flowers": true``

Overrides whether Karma Flowers can spawn in the slugcat's campaign.

<hr>

### "enlightened"
``bool``\
Ex: ``"enlightened": true``

When set to true, allows the slugcat to see Void Spawn and speak to Echoes and Looks To The Moon by default without the mark.

<hr>

### "has_id_drone" [MSC]
``bool``\
Ex: ``"has_id_drone": true``

When set to true, the slugcat will spawn with Artificer's ID drone.

<hr>

### "can_access_whitetokens" [MSC]
``bool``\
Ex: ``"can_access_whitetokens": true``

When set to true, allows the slugcat to access white token objects in they exist in their timeline.

<hr>

### "reveal_mark_overtime"
``int``\
Ex: ``"reveal_mark_overtime": 5``

When set, the alpha of the mark will fade in over the set amount of cycles. This also applies to the main menu art mark.

<hr>

### "start_position"
``int[2][1..]``\
Ex:``"start_position": [5, 10]``, ``"start_position": [[5, 10], [10, 5]]``

IntVector2 values that corrospond to the tile position the slugcat should start in. The length of the array should match the length of "start_room". Tile positions can be viewed in-game with the [DebugMouse](https://rainworldmodding.miraheze.org/wiki/DebugMouse).

<hr>

### "start_stomach_item"
``abstract object``\
Ex:
```JSON
"start_stomach_item": {
    "AbstractRifle": {
        "ammoType": "Grenade",
        "ammo" : {
            "Grenade": 500,
            "Rock": 0,
            "Firecracker": 0,
            "Light": 0,
            "Bees": 0,
            "Fruit": 0,
            "FireEgg": 0,
            "Pearl": 0,
            "Ash": 0,
            "Void": 0,
            "Noodle": 0,
            "Singularity": 0,
		},
    },
}
```

When set, the slugcat will have an item in their stomach on their starting cycle. Currently supports most types that inherit ``AbstractPhysicalObject``. The provided example is the correct format for a Joke Rifle as without setting the other ammo to ``0`` will cause the game to error trying to save (For now until I fix that).

## Player Features

### "grapple_tongue" [MSC]
```JSON
{
    <optional>"retract_lengths": float[2],
    <optional>"retractable": bool,
    <optional>"length": float,
    <optional>"segments": int,
	<optional>"retract_speed": float,
}
```
Ex:
```JSON
"grapple_tongue": {
    "retract_lengths": [ 50, 170 ],
    "retractable": true,
    "length": 150,
    "segments": 20,
	"retract_speed": 1,
},
```

When present, allows the slugcat to use Saint's tongue ability. There are a lot of customizable factors:
- ``"retractable"``: Determines if the length of the tongue can be changed by pressing up or down.
- ``"retract_lengths"``: Controls how long the tongue can be.
- ``"retract_speed"``: Controls how fast the tongue length can be changed.
- ``"length"``: Controls the "ideal" length of the tongue, can be thought of as the default length.
- ``"segments"``: Controls how detailed the rendering of the tongue is, as it functions as a TriangleMesh. Lower segment counts make the tongue visually appear more sharp around edges.

<hr>

### "grab_overrides"
```JSON
{
    "<AbstractPhysicalObject.Type>": "<ObjectGrabability>",
    "<CreatureTemplate.Type>": "<ObjectGrabability>",
}
```
Ex:
```JSON
"grab_overrides": {
    "JellyFish": "Drag",
    "YellowLizard": "OneHand",
    "Spear": "OneHand",
}
```
Overrides how the slugcat can grasp object or creature types. To simulate dual wielding spears, set ``Spear`` to ``OneHand``. This does not affect how heavy the object is, as it's relative to the slugcat's mass.

<hr>

### "bite_lethality_mutliplier"
``float[1..2]``\
Ex: ``"bite_lethality_mutliplier": 0``, ``"bite_lethality_mutliplier": [0, 5]``

Affects the chance of a creature bite killing the player instantly. The first number is added to the difficulty, which is divided by either ``5`` or the second number in the array. ``0`` represents no lethality, while ``1`` represents guarenteed lethality.

<hr>

### "object_interactions"
```JSON
{
    <optional>"pop_bubble_fruit": bool,
    <optional>"bubble_weed_usage_multiplier": float<0-1>,
    <optional>"poison_immune": bool,
    <optional>"explosive_immune": bool,
    <optional>"mushroom_interactions": {
        <optional>"timer": int,
        <optional>"frames_per_second": int
    }
}
```
Ex:
```JSON
"object_interactions": {
    "bubble_weed_usage_multiplier": 1,
    "explosive_immune": false,
    "mushroom_interactions": {
        "timer": 320,
        "frames_per_second": 15,
    },
    "poison_immune": false
    "pop_bubble_fruit": false,
}
```

Various object specific interactions that can be modified.
- ``"pop_bubble_fruit"``: When true, pops bubble fruit like Rivulet when holding them.
- ``"bubble_weed_usage_multiplier"``: A float value to multiply how fast the slugcat consumes Bubble Weed.
- ``"poison_immune"``: Affects whether being injected and eating poisonous things actually poison the slugcat.
- ``"explosive_immune"``: Affects whether indirect explosions kill slugcat from impact like Artificer.
- ``"mushroom_interactions"``:
    - ``"timer"``: How long (relative to the frames per second) the mushroom effect should last.
    - ``"frames_per_second"``: How fast the game should run under the effects of a mushroom. ``40`` is the default FPS the game runs in. This number also affects the timer, as the timer will tick down every frame.

<hr>

### "body_warmth" [MSC (or) Watcher]
``float<0-1>``\
Ex: ``"body_warmth": 0.4``

Affects how warm by default the slugcat is when exposed to a blizzard like Artificer.

<hr>

### "only_tosses_spears"
``bool``\
Ex: ``"only_tosses_spears": true``

When true, attempting to throw spear causes the slugcat to toss them instead like Saint.

<hr>

### "no_stun_grasp_penalty"
```JSON
{
    "<Creature.DamageType>": bool,
}
```
Ex:
```JSON
"no_stun_grasp_penalty": {
    "Explosion": true,
    "None": true,
    "Blunt": true,
}
```
Overrides damage types causing slugcat to drop their grasps when stunned. For example, being explosive immune and having no ``Explosion`` damage penalty makes the slugcat not drop their items when taking explosion damage.

<hr>

### "take_spears_from_wall"
``bool``\
Ex: ``"take_spears_from_wall": true``

When true, allows slugcat to take spears from walls like Artificer or the remix setting.

<hr>

### "explosive_jump" [MSC]
```JSON
{
    <optional>"limits": int[1..2],
    <optional>"jump_sound_id": "<SoundID>",
    <optional>"parry": bool,
    <optional>"jump_speed": float[1..2],
    <optional>"food_cost": int,
    <optional>"limit_reached_result": "<LimitReached>",
    <optional>"stun_timers": int[1..2],
}
```
Ex:
```JSON
"explosive_jump": {
    "limits": [7, 10],
    "jump_sound_id": "Fire_Spear_Explode",
    "parry": true,
    "food_cost": 0,
    "jump_speed": [8],
    "limit_reached_result": "Die",
    "stun_timers": [60],
}
```
Allows the slugcat to double jump, with some customiziability:
- ``"limits"``: The soft and hard limits of the jump ability. When Artificer reaches the soft limit, they will be stunned until reaching their hard limit where they explode.
- ``"jump_sound_id"``: The sound to play when double jumping.
- ``"parry"``: Whether holding down when performing the double jump should perform an Artificer parry.
- ``"jump_speed"``: The effectiveness of the double jump, values lower than ``6`` usually do not affect velocity.
- ``"limit_reached_result"``: The action to perform when reaching the hard jump limit.
- ``"food_cost"``: If the hard limit action is ``ConsumeFood``, will attempt to consume food. If the slugcat has inefficient food, instead the limit will set the slugcat to a starving state until food is completely replinished.
- ``"stun_timers"``: How long each stun should last when reaching the soft limit, the second value is used if the hard limit is ``LongStun``. If the stun is set to 0, there is no stun penalty.

<hr>

### "gills" [MSC]
```JSON
{
    <optional>"rows": int,
    <optional>"bounciness": float<0-1>,
    <optional>"drag": float,
    <optional>"length": float,
    <optional>"width": float,
    <optional>"spread": float<0-1>,
}
```
Ex:
```JSON
"gills": {
    "rows": 3,
    "bounciness": 1,
    "drag": 1,
    "spread": 0.65,
}
```
When present, gives the slugcat Salamander gills like Rivulet, with some customiziability:
- ``"rows"``: The amount gill rows the slugcat has.
- ``"bounciness"``: How bouncy the gills are when they move.
- ``"drag"``: The air friction to be applied to the gills.
- ``"length"``: How long the gills are.
- ``"width"``: How tall the gills are.
- ``"spread"``: The distance between each gill.

<hr>

### "spear_specks" [MSC]
```JSON
{
    "speckle_amounts": int[2],
    "feeds_from_spears": bool,
    "pull_sound_id": "<SoundID>",
    "snap_sound_id": "<SoundID>",
    "generation_speed": float<0-1>,
    "food_cost": int,
    "head_shaking": bool,
    "dripping": bool,
    "spear_template": {
        <abstract object>
    }
}
```
Ex:
```JSON
"spear_specks": {
    "speckle_amounts": [5, 3],
    "feeds_from_spears": true,
    "pull_sound_id": "SM_Spear_Pull",
    "snap_sound_id": "SM_Spear_Grab",
    "generation_speed": 0.05,
    "food_cost": 0,
    "head_shaking": true,
    "dripping": true,
}
```
When present, allows the slugcat to craft items from specks on their tail like Spearmaster. Has some customizability:
- ``"speckle_amounts"``: The amount of rows and columns of specks on the tail. Values ``2`` or lower may have unexpected visual results.
- ``"feeds_from_spears"``: Whether the slugcat should feed from their tail objects. Currently only supports spears of any variety.
- ``"pull_sound_id"``: The sound to play on loop when pulling the item out.
- ``"snap_sound_id"``: The sound to play when grabbing the item.
- ``"generation_speed"``: The speed of item generation.
- ``"food_cost"``: The food cost of creating an item.
- ``"head_shaking"``: Determines if the slugcat should visibly react to item generation like Spearmaster does.
- ``"dripping"``: Determines if there should be a visual dripping effect when generating items. Will likely be replaced in the future.
- ``"spear_template"``: The item template that determines the item that will be pulled out of the tail.

<hr>

### "crafting" [MSC]
```JSON
"crafting": {
    <optional>"swallow_recipes": {
        "<AbstractPhysicalObject.Type>": { "cost": int, "result": { <abstract object> } },
        "<CreatureTemplate.Type>": { "cost": int, "result": { <abstract object> } }
    },
    <optional>"one_handed_recipes": {
        "<AbstractPhysicalObject.Type>": { "cost": int, "result": { <abstract object> } },
        "<CreatureTemplate.Type>": { "cost": int, "result": { <abstract object> } }
    },
    <optional>"two_handed_recipes": {
        "<AbstractPhysicalObject.Type>": {
            "<AbstractPhysicalObject.Type>": { <abstract object> },
            "<CreatureTemplate.Type>": { <abstract object> },
        },
        "<CreatureTemplate.Type>": {
            "<AbstractPhysicalObject.Type>": { <abstract object> },
            "<CreatureTemplate.Type>": { <abstract object> },
        }
    },
    <optional>"regurgitate_list": {
        "cost": int,
        "item_pool": [ { "object": { <abstract object> }, "rarity": float<0-1> } ]
    },
},
```
Ex:

```JSON
"crafting": {
    "swallow_recipes": {
        "Rock": {
            "cost": 1,
            "result": {
                "AbstractPhysicalObject": {
                    "type": "ScavengerBomb",
                }
            }
        }
    },
    "one_handed_recipes": {
        "Spear": {
            "cost": 1,
            "result": {
                "AbstractSpear": {
                    "explosive": true,
                }
            }
        }
    },
    "two_handed_recipes": {
        "ScavengerBomb": {
            "ScavengerBomb": {
                "AbstractPhysicalObject": {
                    "type": "SingularityBomb"
                }
            },
        }
    },
    "regurgitate_list": {
        "cost": 0,
        "item_pool": [
            { 
                "object": { 
                    "AbstractPhysicalObject": {
                        "type": "ScavengerBomb",
                    }
                },
                "rarity": 1,
            }
        ]
    },
},
```
When present and valid recipes are given, allows the slugcat to craft, reguritate, or swallow items to make new items. This one is a very heavy feature to delve into, but implementation is relatively simple:
- ``"swallow_recipes"``: Recipes to use when swallowing an item, if your slugcat cannot swallow then they also cannot use this.
    - ``"<Type>"``: The item or creature type to convert.
        - ``"cost"``: The food cost required to make any of the items.
        - ``"result"``: The item to produce from the object or creature type.

<hr>

- ``"one_handed_recipes"``: Recipes to use when holding a single valid item.
    - ``"<Type>"``: The item or creature type to convert.
        - ``"cost"``: The food cost required to make any of the items.
        - ``"result"``: The item to produce from the object or creature type.

<hr>

- ``"two_handed_recipes"``: Recipes to use when holding two valid crafting items. Only one side of the table needs to be defined, the other possible combination will be automatically filled. Ex: ``ScavengerBomb + DataPearl = KarmaFlower`` will automatically be filled in as ``DataPearl + ScavengerBomb = KarmaFlower`` aswell.
    - ``"<Type>"``: The first type to consider. Also useful to use the primary for table organization if this type is used in multiple recipes.
        - ``"<Type>"``: The second type to consider.
            - ``"<abstract object>"``: The object to produce as a result.

<hr>

- ``"regurgitate_list"``: An item pool to pull if the slugcat can swallow and regurgitate items.
    - ``"cost"``: The cost to regurgitate a new item.
    - ``"item_pool"``: A list of objects to pull from.
        - ``"object"``: The resulting object.
        - ``"rarity"``: The rarity value of the object. The closer to 0, the more rare the item will be. If there are multiple valid items, the pool will roll another random value to pull the final item.

<hr>

### "saint_eyes" [MSC]
``bool``\
Ex: ``"saint_eyes": true``

When true, the slugcat's eyes will always be closed unless dead.

<hr>

### "can_slam" [MSC]
``bool``\
Ex: ``"can_slam": true``

When true, allows the slugcat to use Gourmand's slamming ability.

<hr>

### "cant_swallow_objects"
``bool``\
Ex: ``"cant_swallow_objects": false``

When true, disallows the slugcat from swallowing or regurgitating objects.

## Timeline Features
When implementing a custom timeline, there are some features of other timelines that one may also want in their own, but are otherwise hardcoded. These features aim to help un-hardcode those. They can only be used when a slugbase timeline is used.

### "show_rain_timer"
``bool``\
Ex: ``"show_rain_timer": false``

Overrides the rain timer dots being drawn, like in Saint's timeline. Also, inheritting Saint's timeline will also implement this by default now.