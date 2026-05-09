using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SlugBase;
using SlugBase.Features;
using UnityEngine;

namespace ExtendedSlugbase.Features.GameRelated;
public class RevealMarkOverCycles() : GameFeature<int>("reveal_mark_overtime", JsonUtils.ToInt)
{
	internal static class Implementation
	{
		/// <summary>
		/// Implements the mark fade on the select screen.
		/// </summary>
		internal static void Menu_SlugcatSelectMenu_SlugcatPage_GrafUpdate(ILCursor c)
		{
			static float MarkFadeOnMenu(SlugcatSelectMenu.SlugcatPage self, float markAlpha)
			{
				float mult = 0f;
				if (self is SlugcatSelectMenu.SlugcatPageContinue page
				&& self.slugcatNumber.TryGetFeature(ExtGameFeatures.RevealMarkOverCycles, out int cycles))
				{
					mult = Mathf.Pow(Mathf.InverseLerp(4f, cycles, page.saveGameData.cycle), 3.5f);
				}
				markAlpha *= mult;
				return markAlpha;
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchCallOrCallvirt(out _),
				x => x.MatchMul(),
				x => x.MatchStloc(2),
				x => x.MatchLdarg(0)
				); // AFTER: num3 *= ((this is SlugcatSelectMenu.SlugcatPageContinue) ? Mathf.Pow(Mathf.InverseLerp(4f, 14f, (float)(this as SlugcatSelectMenu.SlugcatPageContinue).saveGameData.cycle), 3.5f) : 0f);
			c.Emit(OpCodes.Ldloc, 2);
			c.EmitDelegate(MarkFadeOnMenu);
			c.Emit(OpCodes.Stloc, 2);
			c.Emit(OpCodes.Ldarg_0); // Place back onto the stack
		}

		/// <summary>
		/// Implements mark fade on the slugcat in-game.
		/// </summary>
		internal static void PlayerGraphics_Ctor(PlayerGraphics self)
		{
			var game = self.player.abstractCreature.world.game;
			if (game.IsStorySession
			&& game.TryGetFeature(GameFeatures.TheMark, out bool hasMark) && hasMark
			&& game.TryGetFeature(ExtGameFeatures.RevealMarkOverCycles, out int cycles))
			{
				self.markBaseAlpha = Mathf.Pow(Mathf.InverseLerp(4f, cycles, self.player.abstractCreature.world.game.GetStorySession.saveState.cycleNumber), 3.5f);
			}
		}
	}
}
