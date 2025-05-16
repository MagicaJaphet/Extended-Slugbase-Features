using System;
using BepInEx;
using SlugBase.Features;
using SlugBase;
using MonoMod.Cil;
using MoreSlugcats;
using Mono.Cecil.Cil;
using BepInEx.Logging;

namespace ExtendedSlugbaseFeatures
{
	[BepInPlugin(MOD_ID, "Extended Slugbase Features", "1.1.0")]
	internal class Plugin : BaseUnityPlugin
	{
		internal const string MOD_ID = "magica.extendedslugbasefeatures";
		internal static new ManualLogSource Logger;
		internal bool isInit;

		// Add hooks
		internal void OnEnable()
		{
			Logger = base.Logger;
			// Ensure the features load
			_ = new Resources.ExtFeatures();
			On.RainWorld.PostModsInit += PostModsInit;
		}

		internal void PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
		{
			orig(self);

			if (isInit) return;
			isInit = true;

			try
			{
				// Apply our hooks as early as possible to avoid conflictions with other mods which IL hook onto the same methods
				PlayerHooks.Apply();
				WorldHooks.Apply();
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogException(ex);
			}
		}
	}
}