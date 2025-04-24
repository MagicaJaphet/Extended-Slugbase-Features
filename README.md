# Extended Slugbase Features
![til](./extended-slugbase.gif)

A framework aiming to extend the basic features of Slugbase to include quality of life functions for modders. This documentation assumes you know the basics of [Slugbase](https://slimecubed.github.io/slugbase/articles/gettingstarted.html?target="_blank"). Otherwise, do  check out their documentation first.

### The general idea list can be found in [IDEASGUY.md](./IDEASGUY.md), feel free to make suggestions on Raincord by pinging me (<@192423177320792065>).

# Basic Features
These are features that would be added in the `features` list of your Slugbase character's JSON.

# Default Object Properties
This section is used to define specifics used in object properties to parse information. Each parameter will specify which DLC may be required, and what type of variable the parameter is passed as.

## [DataPearl.AbstractDataPearl.DataPearlType](#tab/datapearltype)
`"dataPearlType": string` defines the type and color of the specified DataPearl, usually corrosponding to the origin of the pearl by it's region acronym or Slugcat. Vanilla SI pearls  choose between 5 chatlogs at random when Moon reads them.
- Vanilla: `Misc`, `Misc2`, `CC`, `SI_west`, `SI_top`, `LF_west`, `LF_bottom`, `HI`, `SH`, `DS`, `SB_filtration`, `SB_ravine`, `GW`, `SL_bridge`, `SL_moon`, `SU`, `UW`, `PebblesPearl`, `SL_chimney`, `Red_stomach`

The only major version difference to note with MSC is that SI's DataPearls were split up evenly, one chatlog per pearl. Pebble's music pearl in specific are defined as `RM` for Rivulet, and `CL` for Saint.
- MSC: `Spearmasterpearl`, `SU_filt`, `SI_chat3`, `SI_chat4`, `SI_chat5`, `DM`, `LC`, `OE`, `MS`, `RM`, `Rivulet_stomach`, `LC_second`, `CL`, `VS`, `BroadcastMisc`

## [AbstractSpear](#tab/abstractspear)
Spear types are defined by setting specific spear values. Some need a `boolean` set to true, while others are dependant on a `float`. Most properties cannot overlap by default, so it's wise to choose one at best. Below are the listed default values to obtain various properties.
### Vanilla
- `"explosive": true`

### MSC
`hue` is set when [FireBug](https://rainworld.miraheze.org/wiki/Firebug?target="_blank") explode and release their spears. `needle` refers to Spearmaster Spears.
- `"electricCharge": 3`
- `"needle": true`
- `"hue": 0`

### Watcher
`poison` and `poisonHue` go hand in hand, normally only obtainable by stabbing a [Tartigrade](https://rainworld.miraheze.org/wiki/Tardigrade?target="_blank").
- `"electricCharge": 3`
- `"poison": 1`
    - `"poisonHue": 0`

## WaterNut
Object properties support the `swollen` attribute, used when [Bubble Fruit](https://rainworld.miraheze.org/wiki/Bubble_Fruit?target="_blank") pop and become edible.

## General Features
### `"use_watchersblackamount"`
`float`\
Ex: `"use_watchersblackamount": 1`\
By default, when setting a [custom_colors](https://slimecubed.github.io/slugbase/articles/features.html?tabs=slugcatname#custom_colors?target="_blank") slot to pure black (#000000), which is usually used for transparency, it will attempt to use the palette's black color instead. This setting when specified, will use Nightcat or Watcher's blueish black color to the specified amount.
```csharp
Color.Lerp(palette.blackColor, Custom.HSL2RGB(0.63055557f, 0.54f, 0.5f), Mathf.Lerp(0.08f, 0.04f, palette.darkness) * <use_watchersblackamount>)
```

## World Features
### "start_position"
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

### "start_stomach_item"
```JSON
{
    "type": "<AbstractPhysicalObject.Type>",
    "<property>": string / int / float / bool
}
```
Ex:
```JSON
"start_stomach_item": {
    "type": "DataPearl",
    "dataPearlType": "CC"
}
```
If set, spawns in the stomach of the Player on first realization. Allows to pass any valid object type, even spears. For a reference of types, check out [AbstractPhysicalObject.Type](github.com/SlimeCubed/SlugBaseRemix/blob/master/Docs/articles/features.md#abstractphysicalobjecttype?&target="_blank"). The property parameter covers [Spear](#abstractspear), [DataPearl](#datapearlabstractdatapearldatapearltype), and [WaterNut](#waternut) objects but can be extended by the built-in custom object handler via a code mod.

### "intro_cutscene"
```JSON 
{	
    "<room_name>": {
		"player_grasps": [ 
                { "type":"<AbstractPhysicalObject.Type>", 
                "<property>": string / int / float / bool }
            ],
		"inputs": [ 
            { "repeat": 0, 
            "time": 0, 
            "x": [-1..1], 
            "y": [-1..1], 
            "jmp": true, 
            "pckp": true, 
            "mp": true, 
            "crouchToggle": true }
		]
	}		
}
```
Ex:
```JSON 
"intro_cutscene": {	
    "SI_C04": {
		"player_grasps": [ 
                { "type": "Rock" }, 
                { "type": "Spear", "electricCharge": 3 } 
            ],
		"inputs": [ 
            { "time": 100, "x": 1, "y": 1, "crouch": true },
            { "time": 20, "x": 1, "y": 1 }
		]
	}		
}
```
The intro cutscene  feature is nuanced, and may seem complicated at glance. To start with, we initialize the cutscene by specifying which room the information should be used in. If your Slugbase character has multiple starting rooms, it's good to make a separate script for each possibility.

- `"<room_name>": { }` is used to specify the name of the room this script runs in, which should match the name of one of the rooms in your [start_room](https://slimecubed.github.io/slugbase/articles/features.html#start_room?target="_blank") array. It stores all of the information you'll need inside the brackets. Make sure to parse each room with a comma, if there are multiple.
- `"player_grasps": [   ]` is used for storing object information which will spawn objects in the Player's hand, if their grasps are free. This formula follows the same as [start_stomach_item](#start_stomach_item), with the additional bonus of the ability to spawn in 2 or more objects.
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

### `"spawn_karmaflowers"`
`boolean`\
Ex: `"spawn_karmaflowers": true`\
If specified, allows control over if Karma Flowers spawn in the Slugbase character's campaign.

### `"enlightened"`
`boolean`\
Ex: `"enlightened": true`\
If true, allows the Slugbase character to be able to speak to Iterators and Echoes without the mark, and see Voidspawn without the glow.

### `"revealmarkovertime"`
`boolean`\
Ex: `"revealmarkovertime": true`\
If true along with [the_mark](https://slimecubed.github.io/slugbase/articles/features.html?tabs=slugcatname#the_mark?target="_blank"), uses Hunter's gradual mark reveal mechanic where the Mark doesn't visually appear for several cycles to keep it's existence hidden.


# MSC Features
These are features that would be added in the `features` list of your Slugbase character's JSON. They can only be used if the user has MSC enabled due to their DLC limitations. If you use any of these, make sure to include MSC as a mod dependency.

## Cosmetic Features
These features are for looks only, they do not impact gameplay.
### `"rivuletgills"`
`boolean`\
Ex: `"rivuletgills": true"`\
Gives the Player cosmetic Rivulet gills, automatically detecting if [custom_colors](https://slimecubed.github.io/slugbase/articles/features.html?tabs=slugcatname#custom_colors?target="_blank") contains colors for `"Gills"`.

### `"saintfluff"`
`boolean`\
Ex: `"saintfluff": true`\
Gives the Player Saint's fluffy head sprite.

### `"artieyes"`
`boolean`\
Ex: `"artieyes": true`\
Gives the Player Artificer's closed eye face sprite, does not include the scar sprite. Cannot be flipped.

### `"sainteyes"`
`boolean`\
Ex: `"sainteyes": true`\
Gives the Player Saint's face sprite, overwrites `artieyes` if true, unless open.

## Cosmetic / Gameplay Features
These features contain gameplay changes, and cosmetic changes.
### `"can_spawnspears"`
`boolean`\
Ex: `"can_spawnspears": true`\
Gives the Player Spearmaster tail specks, along with the ability to spawn needle spears. Automatically detects if [custom_colors](https://slimecubed.github.io/slugbase/articles/features.html?tabs=slugcatname#custom_colors?target="_blank") contains colors for `"Spears"`. Input for spawning spears changes slightly if the Player can still eat without spears or swallow, requiring the Player to hold up and grab instead. The Player is not required to eat from the spears by default.

## Gameplay Features
These features contain gameplay changes, including general world properties.

### `"max_slugpupspawns"`
`integer`\
Ex: `"max_slugpupspawns": 5`\
The maximum number of Slugpups which can spawn at any given time in the Slugbase character's campaign.

### `"can_accesswhitetokens"`
`boolean`\
Ex: `"can_accesswhitetokens": true`\
Allows the Slugbase character to  access Broadcasts in their campaign, if they exist in their worldstate.

### `"can_dualwield"`
`boolean`\
Ex: `"can_dualwield": true`\
If true, allows the Player to hold two Spears at once.

### `"feedsfromspears"`
`boolean`\
Ex: `"feedsfromspears": true`\
If true along with [can_spawnspears](#can_spawnspears), only allows the Player to feed from freshly made Spears. Diet is adjustable, allowing feeding from plants if specified.
- Each stab counts towards one pip times the food multiplier for that creature/food.
- Corpses can be eaten from if their `meatpoints` are above `0`, if the Corpse multiplier is above `0`.
- Gooieducks are multi-spearable, Popcorn plants and Pomegranates act normally.
- Currently Slimemold is still inedible due to the inability to spear them, along with other typically non-spearable objects.

### `"cant_swallowobjects"`
`boolean`\
Ex: `"cant_swallowobjects": true`\
If true, disallows the Player from swallowing/spitting up objects from their stomach.


### `"can_slam"`
`boolean`\
Ex: `"can_slam": true`\
If true, allows the Player to slam creatures from a high height, inflicting damage like Gourmand.

### `"can_explosivejump"`
`boolean`\
Ex: `"can_explosivejump": true`\
If true, allows the Player to use Artificer's explosive jump, including the down parry ability.

### `"can_craftexplosives"`
`boolean`\
Ex: `"can_craftexplosives": true`\
If true, allows the Player to craft explosives from Spears and swallow objects to convert them like Artificer.
