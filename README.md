# Extended Slugbase Features
![til](./extended-slugbase.gif)

A framework aiming to extend the basic features of Slugbase to include quality of life functions for modders. This documentation assumes you know the basics of [Slugbase](https://slimecubed.github.io/slugbase/articles/gettingstarted.html?target="_blank"). Otherwise, do  check out their documentation first.

### The general idea list can be found in [IDEASGUY.md](./IDEASGUY.md), feel free to make suggestions on Raincord by pinging me (<@192423177320792065>).

# Basic Features
These are features that would be added in the `"features": {}` object of your Slugbase character's JSON.

# Default Object Properties
This section is used to define specifics used in object properties to parse information. Each parameter will specify which DLC may be required, and what type of variable the parameter is passed as.

Object spawning is dynamic by nature, and supports any valid Abstract class. The basic structure of a JSON object for spawning a new object looks like this:
```JSON
"AbstractPhysicalObject": { "type": "Rock" }
```
This by default will spawn a Rock object, but other objects may be more complicated to create an instance of. Object spawning in general follows [DevConsole](https://github.com/SlimeCubed/DevConsole/wiki/Built-In-Commands#spawn-type-id-arg1-arg2-)'s method of spawning, where the spawn command passes parameters to the type of object you want to create an instance of.

In simpler terms, you need to initiate a JSON object with a valid class which uses AbstractPhysicalObject as it's base. For the most part, you could pass a valid type into the ``"type": "object"`` field, but for the sake of ease this document will try to list the most likely use cases for the current features.

## Common ``AbstractPhysicalObject`` Classes
### Vanilla
These do not require any additional DLC to use.
- ``AbstractPhysicalObject`` - The base class that all valid Abstract types use.
    - **\[REQUIRED\]** ``"type": "<type>"``\
    Some subclasses require this field, the ``AbstractObjectType`` this object will be when it's spawned. See [Slugbase's section](https://github.com/SlimeCubed/SlugBaseRemix/blob/master/Docs/articles/features.md#abstractphysicalobjecttype) on valid types.
- ``AbstractConsumable`` - A common class that is used for foods.
    - **\[REQUIRED\]** ``"type": "<type>"``\
    Check out the ``IsTypeConsumable()`` method in this class to see the common types used here.
- ``AbstractDataPearl`` - The ``DataPearl`` or pearl class.
    - **\[REQUIRED\]** ``"type": "<type>"``\
    This parameter should be passed as ``DataPearl``, unless in the case of a unique pearl class that inherits ``DataPearl``.
    - **\[REQUIRED\]** ``"dataPearlType": "<type>"``\
     The ``DataPearlType`` of ``DataPearl`` that determines it's color and contents.
        - Vanilla pearls : ``CC``, ``DS``, ``GW``, ``HI``, ``LF_bottom``, ``LF_west``, ``Misc``, ``Misc2``, ``PebblesPearl``, ``Red_stomach``, ``SB_filtration``, ``SB_ravine``, ``SH``, ``SI_top``, ``SI_west``, ``SL_bridge``, ``SL_chimney``, ``SL_moon``, ``SU``, ``UW``
        - Downpour pearls: ``BroadcastMisc``, ``CL``, ``DM``, ``LC``, ``LC_second``, ``MS``, ``OE``, ``Rivulet_stomach``, ``RM``, ``SI_chat3``, ``SI_chat4``, ``SI_chat5``, ``Spearmasterpearl``, ``SU_filt``, ``VS``
        - Should by default support CRS custom pearls, use the same ID that you'd use to spawn the pearl in Dev Tools.
- ``AbstractCreature`` - The ``Creature`` class.
    - **\[REQUIRED\]** ``"creatureTemplate": "<type>"``\
    Instead of type, this class requires a valid ``CreatureTemplate.Type``. See [Slugbase's section](https://github.com/SlimeCubed/SlugBaseRemix/blob/master/Docs/articles/features.md#creaturetemplatetype) on valid types.
- ``AbstractSpear`` - The ``Spear`` class.
    - ``"explosive": false``\
    Determines whether the spear is explosive.
    - \[MSC\] ``"hue": 0``\
    A ``0`` to ``1`` float value that spawns a ``Firebug`` spear. If ``explosive`` is true, gets overridden by this.
    - \[MSC\] ``"electric": false``\
    Determines if the spear is electric, overrides explosive.
        - ``"electricCharge":  3``\
        The amount of electric charges the spear has.
    - \[MSC\] ``"needle": false``\
    Determines if the spear is a Spearmaster spear.
    - \[Watcher\] ``"poison:" 0``\
    A ``0`` to ``1`` float value that coats the spear in ``Tardigrade`` poison.
        - ``"poisonHue": 0``\
        A ``0`` to ``1`` float value that controls the color of the poison.
- ``EggBugEgg+AbstractBugEgg`` - The class used for Eggbug eggs.
    - **\[REQUIRED\]** ``"hue":``\
    Used to color the egg, a ``0`` to ``1`` float value. Ex: ``0.4``
    - \[MSC\] Replace ``EggBugEgg+AbstractBugEgg`` with ``MoreSlugcats.FireEgg+AbstractBugEgg`` for ``FireBug`` eggs.
- ``AbstractOverseerCarcass`` - The class used for Overseer eyes.
    - **\[REQUIRED\]** ``"color":``\
    A ``UnityEngine.Color`` value. Default is ``default``.
    - **\[REQUIRED\]** ``"ownerIterator":``\
    The number of iterator the Overseer belonged to, determining it's actual color.
        - ``1``: Yellow
        - ``2``: Green
        - ``3``: Red
        - ``4``: White
        - ``5``: Purple
- ``AbstractVultureMask`` - The class used for ``Vulture`` masks.
    - **\[REQUIRED\]** ``"colorSeed": 1``\
    An int value to determine the color of the mask.
    - ``"king": false``\
    Determines if the mask is a ``King Vulture``'s mask.
    - \[MSC\] ``"scavKing": false``\
    Determines if the mask is the ``King Scavenger``'s mask, overrides ``King Vulture``.
    - \[MSC\] ``"spriteOverride": ""``\
    Seemingly unused parameter for the ``King Scavenger`` mask variant.
- ``AbstractBubbleGrass`` - The class used for ``Bubble Grass``.
    - **\[REQUIRED\]** ``"oxygen": 0``\
    A ``0`` to ``1`` value that determines how much oxygen is left in the plant. The default is ``1`` for full.


## General Features
### `"watcher_blue"`
`float`\
Ex: `"watcher_blue": 1`\
By default, when setting a [custom_colors](https://slimecubed.github.io/slugbase/articles/features.html?tabs=slugcatname#custom_colors?target="_blank") slot to pure black ``#000000``, which is usually used for transparency, it will attempt to use the palette's black color instead. This setting when specified, will use Nightcat or Watcher's blueish black color to the specified amount.
```csharp
Color.Lerp(palette.blackColor, Custom.HSL2RGB(0.63055557f, 0.54f, 0.5f), Mathf.Lerp(0.08f, 0.04f, palette.darkness) * <watcher_blue>)
```

## Gameplay Features

### ``"take_spears_from_wall"``
``boolean``\
Ex: ``"take_spears_from_wall": true``\
Allows the ability to take embedded spears out of walls.

## World Features
### `"start_position"`
```JSON
{
    "<room_name>": [0, 0]
}
```
Ex:
```JSON
"start_position":
{
    "SI_C04": [20, 5],
    "SU_A07": [10, 10]
}
```
If the [start_room](https://slimecubed.github.io/slugbase/articles/features.html#start_room?target="_blank") array exists, attempts to set slugcat's position in room tiles based on the room's name. Room tiles can be measured with the Dev Tool [DebugMouse](https://rainworldmodding.miraheze.org/wiki/DebugMouse?target="_blank").

### `"start_stomach_item"`
```JSON
{
    "AbstractPhysicalObject": 
        {
            "type": "<AbstractPhysicalObject.Type>",
            "<property>": null
        }
}
```
Ex:
```JSON
"start_stomach_item": {
    "AbstractDataPearl": 
        {
            "type": "DataPearl",
            "dataPearlType": "CC"
        }
}
```
If set, spawns in the stomach of the Player on first realization. Allows to pass any valid object type, even spears. For a reference of types, check out [AbstractPhysicalObject.Type](github.com/SlimeCubed/SlugBaseRemix/blob/master/Docs/articles/features.md#abstractphysicalobjecttype?&target="_blank"). The object covers most ``AbstractPhysicalObject`` classes, but the most common ones and fields are listed [here]().

### `"intro_cutscene"`
```JSON 
{	
    "<room_name>": {
		"player_grasps": {
                "AbstractPhysicalObject": 
                    {
                        "type": "<AbstractPhysicalObject.Type>",
                        "<property>": null
                    }
            },
		"inputs": [ 
            { "repeat": 0, 
            "time": 0, 
            "x": 0, 
            "y": 0, 
            "jmp": true, 
            "pckp": true, 
            "mp": true, 
            "crouchToggle": true }
		],
        "food": 0
	}		
}
```
Ex:
```JSON 
"intro_cutscene": {	
    "SI_C04": {
		"player_grasps": { 
            "AbstractSpear": { "explosive": true },
            "AbstractPhysicalObject": { "type": "Rock" } 
        },
		"inputs": [ 
            { "time": 100, "x": 1, "y": 1, "crouch": true },
            { "time": 20, "x": 1, "y": 1 }
		],
        "food": 5
	}		
}
```
The intro cutscene  feature is nuanced, and may seem complicated at glance. To start with, we initialize the cutscene by specifying which room the information should be used in. If your Slugbase character has multiple starting rooms, it's good to make a separate script for each possibility.

- `"<room_name>": { }` is used to specify the name of the room this script runs in, which should match the name of one of the rooms in your [start_room](https://slimecubed.github.io/slugbase/articles/features.html#start_room?target="_blank") array. It stores all of the information you'll need inside the brackets. Make sure to parse each room with a comma, if there are multiple.
- `"food": 0` is used to set the amount of food the slugcat starts with. Quarter values are accepted.
- `"player_grasps": {   }` is used for storing object information which will spawn objects in the Player's hand, if their grasps are free. This formula follows the same as [start_stomach_item](#start_stomach_item), with the additional bonus of the ability to spawn in 2 or more objects.
- `"inputs": [   ]` is a list of inputs, translated from the Player.InputPackage class. They do not need a specific key bind set up to work, and rather work based off of the variables passed into it. Parse each input with `{    },`'s.
    - `"repeat": int` is used to specify how many times this input should be repeated. Useful for multi-frame inputs like jump (Simply specifying true on the jump value will only make the Slugcat jump for one frame).
    - `"time": int` is used to tell the script how long in frames the input should run (~40 frames a second by default).
    - `"x": int` and `"y": int` both are simple integer values which represent the direction being held.
        - `0` represents no direction.
        - `1` represents up and right.
        - `-1` represents down and left.
    - `"jump": boolean` or `"jmp": boolean` is used for the jump input. Jump inputs on an automated controller do not carry over in long presses, and should be specified per frame to perform full jumps.
    - `"grab": boolean` or `"pckp": boolean` is used for the grab/pickup input. The same goes here as it does for jump, long presses do not cause Slugcat to swallow/eat.
    - `"map": boolean` or `"mp": boolean` is used for the map input.
    - `"crouch": boolean` or `"crouchToggle": boolean` is used by controllers to signify when the Slugcat should crouch or stand.

There is currently no tool to translate these inputs into this specified format, but some other mods like Preservatory include built in debug tools for recording inputs. Inputs can be updated in live game time, and replayed by restarting the cycle (Fastest way is pressing R in Dev Tools).

### `"grab_overrides"`
```JSON
{
	"<AbstractPhysicalObject.Type>": "<Player.ObjectGrabability>"
},
```
Ex:
```JSON
"grab_overrides": {
	"Rock": "Drag",
	"JetFish": "OneHand",
},
```
Allows for custom grabability requirement overrides, enabling the ability to make normally one/two handed items have a different grabbing functionality. It does not affect the weight of the object, but does affect how the slugcat holds it.
#### Valid `Player.ObjectGrabability` types
- ``BigOneHand`` - Used by weapons.
- ``CantGrab`` - Makes slugcat unable to grab item, default case.
- ``Drag`` - Makes slugcat drag an item with two hands.
- ``OneHand`` - One handed item, default for non weapon items.
- ``TwoHands`` - Makes slugcat hold an item with two hands, functionally similar to ``Drag``.

### `"bite_lethality_mutliplier"`
`float[]`\
Ex: `"bite_lethality_mutliplier": 0.7, 5`\
Overrides the default ``Player.DeathByBiteMultiplier`` value, which determines how lethal a lizard bite is times this multiplier. The first value controls the default multiplier of ``0.7``, the second controls how the ``StoryGameSession.difficulty`` is divided with a default of `5`.
```csharp
return multipliers[0] + (self.room.game.GetStorySession.difficulty / (multipliers.Length == 1 ? 5f : multipliers[1]));
```
If the game is not in a Story session:
```csharp
return multipliers[0] + 0.05f;
```

### `"spawn_karma_flowers"`
`boolean`\
Ex: `"spawn_karma_flowers": true`\
If specified, allows control over if Karma Flowers spawn in the Slugbase character's campaign.

### `"enlightened"`
`boolean`\
Ex: `"enlightened": true`\
If true, allows the Slugbase character to be able to speak to Iterators and Echoes without the mark, and see Voidspawn without the glow.

### `"reveal_mark_overtime"`
`int`\
Ex: `"reveal_mark_overtime": 14`\
If a valid int is passed along with [the_mark](https://slimecubed.github.io/slugbase/articles/features.html?tabs=slugcatname#the_mark?target="_blank") being true, uses Hunter's gradual mark reveal mechanic where the Mark doesn't visually appear for several cycles to keep it's existence hidden. The number represents the amount of cycles before the mark is at full opacity.


# MSC Features
These are features that would be added in the `features` list of your Slugbase character's JSON. They can only be used if the user has MSC enabled due to their DLC limitations. If you use any of these, make sure to include MSC as a mod dependency.

## World Features
### `"can_pass_OE_gate"`
`boolean[]`\
Ex: `"can_pass_OE_gate": [true, true]`\
When true, allows the Story slugcat to pass through the OE gate. If no second value is provided, or the second value is `true`, checks if Gourmand has been properly beaten before unlocking.

## Cosmetic Features
These features are for looks only, they do not impact gameplay.
### `"gill_rows"`
`int`\
Ex: `"gill_rows": 3"`\
Gives the Player cosmetic Rivulet gills, automatically detecting if [custom_colors](https://slimecubed.github.io/slugbase/articles/features.html?tabs=slugcatname#custom_colors?target="_blank") contains colors for `"Gills"`. The total number of gills will be `gill_rows * 2`.

### `"saint_fluff"`
`boolean`\
Ex: `"saint_fluff": true`\
Gives the Player Saint's fluffy head sprite.

### `"arti_eyes"`
`boolean`\
Ex: `"arti_eyes": true`\
Gives the Player Artificer's closed eye face sprite, does not include the scar sprite. Cannot be flipped.

### `"saint_eyes"`
`boolean`\
Ex: `"saint_eyes": true`\
Gives the Player Saint's face sprite, overwrites [arti_eyes](#arti_eyes) if true, unless open.

## Cosmetic / Gameplay Features
These features contain gameplay changes, and cosmetic changes.
### `"spear_specks"`
`int[]`\
Ex: `"can_spawnspears": [5, 3]`\
Gives the Player Spearmaster tail specks with the specified number of rows and lines, along with the ability to spawn needle spears. Automatically detects if [custom_colors](https://slimecubed.github.io/slugbase/articles/features.html?tabs=slugcatname#custom_colors?target="_blank") contains colors for `"Spears"`. Input for spawning spears changes slightly if the Player can still eat without spears or swallow, requiring the Player to hold up and grab instead. The Player is not required to eat from the spears by default.

## Gameplay Features
These features contain gameplay changes, including general world properties.

### `"max_slugpup_spawns"`
`integer`\
Ex: `"max_slugpup_spawns": 5`\
The maximum number of Slugpups which can spawn at any given time in the Slugbase character's campaign.

### `"can_access_whitetokens"`
`boolean`\
Ex: `"can_access_whitetokens": true`\
Allows the Slugbase character to access Broadcasts in their campaign, if they exist in their worldstate.

### `"can_dualwield"`
`boolean`\
Ex: `"can_dualwield": true`\
If true, allows the Player to hold two Spears at once.

### `"feeds_from_spears"`
`boolean`\
Ex: `"feeds_from_spears": true`\
If true along with [can_spawnspears](#can_spawnspears), only allows the Player to feed from freshly made Spears. Diet is adjustable, allowing feeding from plants if specified.
- Each stab counts towards one pip times the food multiplier for that creature/food.
- Corpses can be eaten from if their `meatpoints` are above `0`, if the Corpse multiplier is above `0`.
- Gooieducks are multi-spearable, Popcorn plants and Pomegranates act normally.
- Currently Slimemold is still inedible due to the inability to spear them, along with other typically non-spearable objects.

### `"cant_swallow_objects"`
`boolean`\
Ex: `"cant_swallow_objects": true`\
If true, disallows the Player from swallowing/spitting up objects from their stomach.


### `"can_slam"`
`boolean`\
Ex: `"can_slam": true`\
If true, allows the Player to slam creatures from a high height, inflicting damage like Gourmand.

### `"explosive_jump"`
`int[]`\
Ex: `"explosive_jump": [5, 10]`\
If true, allows the Player to use Artificer's explosive jump, including the down parry ability. The numbers represent the amount of times this ability can be used before approaching Artificer's burnout, and the max amount of times before they die. If no second value is specified, the feature will attempt to use the first number and divide it for the explosion thresholds, with the value being the max.

### `"craft_explosives_cost"`
`int`\
Ex: `"craft_explosives_cost": 1`\
Allows the Player to craft explosives from Spears and swallow objects to convert them like Artificer, with the equivalent food cost number.

### `"get_karma_from_scavs"`
`boolean`\
Ex: `"get_karma_from_scavs": true`\
Allows the Player to gain temporary Karma from holding Scavenger corpses.

### `"pop_held_bubblefruit"`
`boolean`\
Ex: `"pop_held_bubblefruit": true`\
Allows the Player to pop held Bubble Fruit like Rivulet.

### `"only_tosses_spears"`
`boolean`\
Ex: `"only_tosses_spears": true`\
Overrides the Player's ability to throw spears with Saint's spear toss.

### `"take_spears_from_wall"`
`boolean'\
Ex: `"take_spears_from_wall": true`\
Allows Player to take embedded spears from walls like Artificer.