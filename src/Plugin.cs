using System;
using BepInEx;
using SlugBase.Features;
using SlugBase;
using MonoMod.Cil;
using MoreSlugcats;
using Mono.Cecil.Cil;
using BepInEx.Logging;
using MonoMod.RuntimeDetour;
using ExtendedSlugbaseFeatures.Hooks;
using ExtendedSlugbaseFeatures.Helpers;
using ExtendedSlugbaseFeatures.Resources;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ExtendedSlugbaseFeatures
{
	[BepInPlugin(MOD_ID, "Extended Slugbase Features", "1.1.0")]
	internal class Plugin : BaseUnityPlugin
	{
		internal const string MOD_ID = "magica.extendedslugbasefeatures";
		internal static new ManualLogSource Logger;
		internal static string MOD_PATH;
		private List<string> addToStringsQueue = [];

		internal static string GetStringsPath()
		{
			return string.Join(Path.DirectorySeparatorChar.ToString(), [MOD_PATH, "text", "text_eng", "strings.txt"]);
		}

		// Add hooks
		internal void OnEnable()
		{
			Logger = base.Logger;
			// Ensure the features load
			_ = new Features();
			_ = new RoomSpecificScriptHelpers.CustomCutscene.CutsceneID("null", false);
			_ = RoomSpecificScriptHelpers.CustomCutscene.Registry;

			On.RainWorld.OnModsInit += Extras.WrapInit((rainWorld) =>
			{
				ModOptions.RegisterOI();

				MOD_PATH = ModManager.ActiveMods.Find(mod => mod.id == MOD_ID).path;
				On.InGameTranslator.Translate += InGameTranslator_Translate;
				Application.quitting += Application_quitting;
			});

			On.RainWorld.PostModsInit += Extras.WrapPostInit((rainWorld) =>
			{
				Resources.Resources.Enums.Register();
				RoomSpecificScriptHelpers.ScanFiles();

				// Apply our hooks as late as possible to avoid conflictions with other mods which IL hook onto the same methods
				PlayerHooks.Apply();
				WorldHooks.Apply();
				ResourceHooks.Apply();


				// Possible hook to add refreshability to the remix menu? would this break things??? idk
				//_ = new Hook(typeof(JsonRegistry<SlugcatStats.Name, SlugBaseCharacter>).GetMethod(nameof(JsonRegistry<SlugcatStats.Name, SlugBaseCharacter>.ReloadChangedFiles), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance), (Action<JsonRegistry<SlugcatStats.Name, SlugBaseCharacter>> orig, JsonRegistry<SlugcatStats.Name, SlugBaseCharacter> self) =>
				//{
				//	orig(self);
				//	ModOptions.RegisterOI();
				//});
			});
		}

		private void Application_quitting()
		{
			if (addToStringsQueue.Count > 0 && File.Exists(GetStringsPath()))
			{
				using FileStream stream = new(GetStringsPath(), FileMode.OpenOrCreate);
				using StreamWriter write = new(stream);
				foreach (var item in addToStringsQueue)
				{
					write.WriteLine($"{item}|{{TRANSLATION NEEDED FOR {item}}}");
				}
				write.Close();
				stream.Close();
			}
		}

		private string InGameTranslator_Translate(On.InGameTranslator.orig_Translate orig, InGameTranslator self, string s)
		{
			var result = orig(self, s);
			if (!string.IsNullOrEmpty(s) && s.Length > 0 && s.Contains("slugbase") && s.Contains('[') && File.Exists(GetStringsPath()))
			{
				string[] lines = File.ReadAllLines(GetStringsPath());
				if (lines.Length > 0)
				{
					bool hasKey = false;
					foreach (var line in lines)
					{
						string compareLine = line.Trim();
						if (compareLine.Length > 0)
						{
							if (compareLine.Contains('|'))
							{
								compareLine = compareLine.Split('|')[0];
							}

							if (string.Compare(s, compareLine, StringComparison.OrdinalIgnoreCase) == 0) hasKey = true;
						}
					}

					if (!hasKey)
					{
						addToStringsQueue.Add(s);
					}
				}
			}
			return result;
		}

		public void Update()
		{
			RoomSpecificScriptHelpers.CustomCutscene.Registry.ReloadChangedFiles();
		}
	}
}