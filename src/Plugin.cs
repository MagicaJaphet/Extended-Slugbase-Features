using BepInEx;
using BepInEx.Logging;
using ExtendedSlugbaseFeatures.Helpers;
using ExtendedSlugbaseFeatures.Hooks;
using ExtendedSlugbaseFeatures.Resources;
using MonoMod.RuntimeDetour;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ExtendedSlugbaseFeatures
{
	[BepInPlugin(MOD_ID, "Extended Slugbase Features", "1.1.0")]
	internal class Plugin : BaseUnityPlugin
	{
		internal const string MOD_ID = "magica.extendedslugbasefeatures";
		internal static string MOD_PATH = "";
		internal static new ManualLogSource Logger;

		// Add hooks
		internal void OnEnable()
		{
			Logger = base.Logger;
			_ = new Hook(typeof(FeatureManager).GetMethod(nameof(FeatureManager.Register), BindingFlags.Public | BindingFlags.Static), FeatureManagerRegisterHook);

			// Ensure the features load
			_ = new RoomSpecificScriptHelpers.CustomCutscene.CutsceneID("null", false);
			_ = RoomSpecificScriptHelpers.CustomCutscene.Registry;

			On.RainWorld.OnModsInit += Extras.WrapInit((rainWorld) =>
			{
				ModOptions.RegisterOI();

				MOD_PATH = ModManager.ActiveMods.FirstOrDefault(mod => mod.id == MOD_ID).path;
			});

			On.RainWorld.PostModsInit += Extras.WrapPostInit((rainWorld) =>
			{
				_ = new ExtFeatures();
				Resources.Resources.Enums.Register();
				RoomSpecificScriptHelpers.ScanFiles();

				UnityEngine.Debug.Log($"File exists : {File.Exists(Path.Combine(MOD_PATH, "atlases", "modicon-slugbase.png"))}");
				Futile.atlasManager.LoadAtlas(Path.Combine(MOD_PATH, "atlases", "extuisprites"));

				// Apply our hooks as late as possible to avoid conflictions with other mods which IL hook onto the same methods
				PlayerHooks.Apply();
				Hooks.WorldHooks.Apply();
				ResourceHooks.Apply();

				// Possible hook to add refreshability to the remix menu? would this break things??? idk
				//_ = new Hook(typeof(JsonRegistry<SlugcatStats.Name, SlugBaseCharacter>).GetMethod(nameof(JsonRegistry<SlugcatStats.Name, SlugBaseCharacter>.ReloadChangedFiles), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance), (Action<JsonRegistry<SlugcatStats.Name, SlugBaseCharacter>> orig, JsonRegistry<SlugcatStats.Name, SlugBaseCharacter> self) =>
				//{
				//	orig(self);
				//	ModOptions.RegisterOI();
				//});
			});
		}

		private void FeatureManagerRegisterHook(Action<Feature> orig, Feature feature)
		{
			orig(feature);

			string originMod = "SlugBase";

			var trace = new StackTrace(true);
			bool firstSlugbaseTrace = false;
			for (int i = 0; i < trace.FrameCount; i++)
			{
				var method = trace.GetFrame(i).GetMethod();
				var asm = method?.ReflectedType?.Assembly;
				if (!firstSlugbaseTrace && asm.GetName().Name != "SlugBase")
				{
					continue;
				}
				if (AbstractPhysicalObjectHelpers.dllBlacklist.Contains(asm.GetName().Name)) break;
				if (asm.GetName().Name == "SlugBase")
				{
					firstSlugbaseTrace = true;
					continue;
				}
				;
				if (asm != typeof(RainWorld).Assembly && asm != typeof(Feature).Assembly)
				{
					originMod = asm.GetName().Name;
					break;
				}
			}

			ModOptions._allFeatures.Add(new(feature, originMod));
		}

		public void Update()
		{
			RoomSpecificScriptHelpers.CustomCutscene.Registry.ReloadChangedFiles();
		}
	}
}