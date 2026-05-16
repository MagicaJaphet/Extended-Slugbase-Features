using ExtendedSlugbase.Assets;
using ExtendedSlugbase.DataTypes;
using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using MagicaHookingLibrary.Helpers;
using MagicaHookingLibrary.Interfaces;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RWCustom;
using SlugBase;
using SlugBase.DataTypes;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static ExtendedSlugbase.Extensions.SlugBaseExtensions;
using static ExtendedSlugbase.Extensions.SlugbaseHelpers;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class SlugbaseHooks : IOwnHooks
	{
		internal static void Plugin_ctor()
		{
			_ = new Hook(typeof(ColorSlot).GetConstructor([typeof(int), typeof(JsonAny)]), SlugbaseHooks.ColorSlot_ctor);
			_ = new Hook(Register, SlugbaseHooks.FeatureManagerRegisterHook);
			_ = new Hook(typeof(SlugBaseCharacter.FeatureList).GetMethod(nameof(SlugBaseCharacter.FeatureList.Set), BindingFlags.Public | BindingFlags.Instance), SlugbaseHooks.FeatureListSet);
			_ = new Hook(AddMany, SlugbaseHooks.FeatureListAddMany);
		}

		internal static void ColorSlot_ctor(Action<ColorSlot, int, JsonAny> orig, ColorSlot self, int index, JsonAny json)
    	{
    		try
    		{
    			orig(self, index, json);
    		}
    		catch (JsonException) {}

    		var obj = json.AsObject();
    		ExtColorSlot extSlot = new(obj);
    		ExtColorSlot.ExtendedColorSlots.Add(self, extSlot);

    		// Then regrab the original values in case they're intvector2
    		extSlot.ParseColor(obj.Get("story"), out var col, out var pal);
    		self.Default = col ?? default;
    		extSlot.DefaultPaletteIndex = pal;

    		if (obj.TryGet("arena", out JsonList list, throwIfParseError: false))
    		{
    			Color[] arenaColors = new Color[list.Count];
    			IntVector2?[] arenaPalettes = new IntVector2?[list.Count];

    			for (int i = 0; i < list.Count; i++)
    			{
    				var item = list[i];
    				extSlot.ParseColor(item, out var arenaCol, out var arenaPal);
    				arenaColors[i] = arenaCol ?? default;
    				arenaPalettes[i] = arenaPal;
    			}

    			self.Variants = arenaColors;
    			extSlot.VariantPaletteIndexes = arenaPalettes;
    		}
    	}

        internal static void FeatureManagerRegisterHook(Action<Feature> orig, Feature feature)
    	{
    		orig(feature);
		
            Assembly originAss = ReflectionHelpers.GetTraceAssembly(typeof(Feature).Assembly);

    		if (SlugbaseHelpers.RegisteredFeatures.TryGetValue(feature.ID, out var info))
    		{
    			info.originAssembly = originAss;
    			SlugbaseHelpers.RegisteredFeatures[feature.ID] = info;
    		} 
    		else
    		{
    			SlugbaseHelpers.RegisteredFeatures.Add(feature.ID, new() { originAssembly = originAss });
    		}
    	}

    	internal static void FeatureListSet(Action<SlugBaseCharacter.FeatureList, Feature, JsonAny> orig, SlugBaseCharacter.FeatureList self, Feature feature, JsonAny json)
    	{
    		SlugbaseHelpers.CheckForInvalidDLC(feature.ID, json, ExtFeatureTypes.throwDLCErrors[self]);

    		orig(self, feature, json);
    	}

    	internal static void FeatureListAddMany(Action<SlugBaseCharacter.FeatureList, JsonObject> orig, SlugBaseCharacter.FeatureList self, JsonObject json)
    	{
			ExtFeatureTypes.throwDLCErrors.Add(self, !json.TryGet(ExtFeatureTypes.ignoreDLCErrors.ID, out bool throwErrors) || !throwErrors);
    		foreach ((string key, JsonAny value) in json.GetKeyPairEnumerator())
    		{
    			if (SlugbaseHelpers.InvokeTryGetFeature(key, out var feature))
    			{
    				SlugbaseHelpers.CheckForInvalidDLC(feature.ID, value, ExtFeatureTypes.throwDLCErrors[self]);
    			}
    		}
    		orig(self, json);
    	}

		public void PreApply()
    	{
			_ = new Hook(typeof(SlugBase.Assets.CustomScene).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(MenuScene.SceneID), typeof(JsonObject)], null), ExtCustomScene_ctor);
    		_ = new Hook(SlugbaseHelpers.FeatureHooks.GetMethod("PlayerGraphics_ApplyPalette", BindingFlags.NonPublic | BindingFlags.Static), Slugbase_ApplyPalette);
    		_ = new Hook(SlugbaseHelpers.FeatureHooks.GetMethod("PlayerGraphics_DefaultBodyPartColorHex", BindingFlags.NonPublic | BindingFlags.Static), Slugbase_DefaultBodyPartColorHex);
    		_ = new Hook(SlugbaseHelpers.FeatureHooks.GetMethod("PlayerGraphics_DefaultSlugcatColor", BindingFlags.NonPublic | BindingFlags.Static), Slugbase_DefaultSlugcatColor);
    		_ = new Hook(SlugbaseHelpers.FeatureHooks.GetMethod("PlayerGraphics_DrawSprites", BindingFlags.NonPublic | BindingFlags.Static), SlugbaseHooks.PlayerGraphics_DrawSprites);
    	}

		private static void ExtCustomScene_ctor(Action<SlugBase.Assets.CustomScene, MenuScene.SceneID, JsonObject> orig, SlugBase.Assets.CustomScene self, MenuScene.SceneID id, JsonObject json)
		{
			orig(self, id, json);

			ExtCustomScene.ExtCustomScenes.Add(self, new(self, json));
		}

		// Default behavior: Overwrite pure black with an offshade
		private static Color Slugbase_DefaultSlugcatColor(On.PlayerGraphics.orig_DefaultSlugcatColor orig, SlugcatStats.Name i)
    	{
    		var color = orig(i);
    		if (SlugBaseCharacter.TryGet(i, out var chara)
    			&& PlayerFeatures.SlugcatColor.TryGet(chara, out var col))
    		{
    			return col.ReturnPaletteOrOffBlack();
    		}
    		return color;
    	}

    	// Default behavior: Overwrite pure black with an offshade
    	private static List<string> Slugbase_DefaultBodyPartColorHex(On.PlayerGraphics.orig_DefaultBodyPartColorHex orig, SlugcatStats.Name name)
    	{
    		var list = orig(name);

    		if (SlugBaseCharacter.TryGet(name, out var chara)
                    && PlayerFeatures.CustomColors.TryGet(chara, out var colorSlots))
                {
                    list.Clear();
                    list.AddRange(colorSlots.Select(slot => Custom.colorToHex(slot.Default.ReturnPaletteOrOffBlack())));
                }

    		return list;
    	}

    	// Color Fades: Replace default body color with potential faded colors
    	// This is technically an il hook but it's on so it stays here lol
    	private static void Slugbase_ApplyPalette(Action<ILContext> orig, ILContext il)
    	{
    		ILCursor c = new(il);

    		static Color ColorWithFade(PlayerGraphics self, Color color, RoomPalette palette)
    		{
    			if (self.player.TryGetColorSlots(out var slots) && self.TryGetCustomColor(slots, "Body", out var newColor))
    			{
    				return newColor;
    			}
    			else if (PlayerColor.Body.GetColor(self) is Color bodyColor)
    			{
    				// Original code
    				Color starveColor = Color.Lerp(bodyColor, Color.gray, 0.4f);
    				float starveAmount = self.player.Malnourished ? self.malnourished : Mathf.Max(0f, self.malnourished - 0.005f);
    				newColor = Color.Lerp(bodyColor, starveColor, starveAmount);

    				return newColor;
    			}
    			return color;
    		}

    		// Body color with fade
    		c.GotoNext(
    			MoveType.AfterLabel,
    			x => x.MatchLdarg(0),
    			x => x.MatchLdloc(1),
    			x => x.MatchCallOrCallvirt<GraphicsModule>(nameof(GraphicsModule.HypothermiaColorBlend))
    			);

    		c.Emit(OpCodes.Ldarg_0);
    		c.Emit(OpCodes.Ldloc_1);
    		c.Emit(OpCodes.Ldarg_3);
    		c.EmitDelegate(ColorWithFade);
    		c.Emit(OpCodes.Stloc_1);
    	}

        internal static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    	{
    		orig(self, sLeaser, rCam, timeStacker, camPos);

    		if (self.player.TryGetColorSlots(out var slots) && self.TryGetCustomColor(slots, "Eyes", out Color eyeColor))
    		{
    			sLeaser.sprites[9].color = eyeColor;
    		}
    		else if (PlayerColor.Eyes.GetColor(self) is Color eyeColor2)
    		{
    			sLeaser.sprites[9].color = eyeColor2;
    		}
    	}

    	public void OnApply()
    	{
    	}

    	public void PostApply()
    	{
    	}
	}
}
