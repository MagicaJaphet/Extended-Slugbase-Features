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

namespace ExtendedSlugbaseFeatures
{
	[BepInPlugin(MOD_ID, "Extended Slugbase Features", "1.1.0")]
	internal class Plugin : BaseUnityPlugin
	{
		internal const string MOD_ID = "magica.extendedslugbasefeatures";
		internal static new ManualLogSource Logger;

		// Add hooks
		internal void OnEnable()
		{
			Logger = base.Logger;
			// Ensure the features load
			_ = new Features();
			_ = new RoomSpecificScriptHelpers.CustomCutscene.CutsceneID("null", false);
			_ = RoomSpecificScriptHelpers.CustomCutscene.Registry;
			On.RainWorld.PostModsInit += Extras.WrapInit((rainWorld) =>
			{
				Resources.Resources.Enums.Register();
				RoomSpecificScriptHelpers.ScanFiles();

				// Apply our hooks as late as possible to avoid conflictions with other mods which IL hook onto the same methods
				PlayerHooks.Apply();
				WorldHooks.Apply();
				ResourceHooks.Apply();
			});
		}

		public void Update()
		{
			RoomSpecificScriptHelpers.CustomCutscene.Registry.ReloadChangedFiles();
		}
	}
}