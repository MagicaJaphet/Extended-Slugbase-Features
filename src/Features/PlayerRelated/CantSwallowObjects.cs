using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using MagicaHookingLibrary.Helpers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features.PlayerRelated;
public class CantSwallowObjects() : PlayerFeature<bool>("cant_swallow_objects", JsonUtils.ToBool)
{
	internal static class Implementation
	{
		internal static void Player_GrabUpdate_1(ILCursor c, VariableDefinition swallowFeature)
		{
			static bool CanSwallow(bool isNotSpear, bool cantSwallow)
			{
				return isNotSpear && !cantSwallow;
			}

			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear).GetSlugcatFieldInfo()
				);
			c.EmitFeatureDelegate(swallowFeature, CanSwallow);
		}

		internal static void Player_GrabUpdate_2(ILCursor c, VariableDefinition swallowFeature)
		{
			static bool GenerateSpearInput(int inputY, bool cantSwallow, Player self)
			{
				int y = cantSwallow || !SlugBaseCharacter.TryGet(self.SlugCatClass, out _) ? 0 : 1;
				return !(self.input[0].y == y); // Because it's a brtrue, we have to reverse the logic
			}

			c.GotoNext(
				MoveType.AfterLabel,
				x => x.MatchBrtrue(out _),
				x => x.MatchLdarg(0)
				); // this.input[0].y == 0
			c.EmitFeatureDelegate(swallowFeature, GenerateSpearInput, true); // Consume the stack so we can just put our own bool
		}
	}
}