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
	[BepInPlugin(MOD_ID, "Extended Slugbase Features", "1.0.0")]
	partial class Plugin : BaseUnityPlugin
	{
		internal const string MOD_ID = "magica.extendedslugbasefeatures";
		internal static new ManualLogSource Logger;
		internal bool isInit;

		// Add hooks
		internal void OnEnable()
		{
			Logger = base.Logger;
			_ = new Resources();
			On.RainWorld.PostModsInit += RainWorld_PostModsInit;
		}

		internal void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
		{
			orig(self);

			if (isInit) return;
			isInit = true;

			try
			{
				PlayerHooks.Apply();
				WorldHooks.Apply();
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogException(ex);
			}
		}
	}

	internal static class PlayerExtension
	{
		internal static void MoveCursorToNextSlugcatInstance(this ILCursor cursor, Func<Instruction, bool> instruction)
		{
			cursor.GotoNext(MoveType.After,
							instruction,
							x => x.MatchCall(out _));
			cursor.MoveAfterLabels();
		}

		internal static bool HasFeature(this Player player, Feature<bool> feature)
		{
			return player != null && SlugBaseCharacter.TryGet(player.SlugCatClass, out var character) && feature.TryGet(character, out bool hasFeature) && hasFeature;
		}
		internal static bool HasFeature(this SlugcatStats.Name name, Feature<bool> feature, out SlugBaseCharacter character)
		{
			character = null;
			return SlugBaseCharacter.TryGet(name, out character) && feature.TryGet(character, out bool hasFeature) && hasFeature;
		}
		internal static bool HasFeature(this Player player, Feature<bool> feature, out SlugBaseCharacter character)
		{
			character = null;
			return player != null && SlugBaseCharacter.TryGet(player.SlugCatClass, out character) && feature.TryGet(character, out bool hasFeature) && hasFeature;
		}
		internal static bool HasFeature(this Player player, GameFeature<bool> feature)
		{
			return player?.room != null && feature.TryGet(player.room.game, out bool hasFeature) && hasFeature;
		}
		internal static bool HasFeature(this Player player, PlayerFeature<bool> feature)
		{
			return player != null && feature.TryGet(player, out bool hasFeature) && hasFeature;
		}
		internal static bool HasFeature(this RainWorldGame game, GameFeature<bool> feature)
		{
			return game != null && feature.TryGet(game, out bool hasFeature) && hasFeature;
		}
	}
}