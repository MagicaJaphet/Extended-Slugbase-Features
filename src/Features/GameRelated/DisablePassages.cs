using ExtendedSlugbase.Extensions;
using MagicaHookingLibrary.Helpers;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features.GameRelated;
public class DisablePassages() : GameFeature<DisablePassages.PassageProperties>("disable_passages", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static PassageProperties Factory(JsonAny json) => new(json);

	public class PassageProperties
	{
		public List<WinState.EndgameID> ForbiddenPassages { get; } = [];
		
		public bool CanUsePassages { get; } = true;
		
		// TODO: Implement
		public bool RequireSurvivorPassage { get; } = true;

		public PassageProperties(JsonAny json)
		{
			if (json.TryParse(out JsonObject obj))
			{
				if (obj.TryGet("can_passage", out bool canPassage))
				{
					CanUsePassages = canPassage;
				}
				if (obj.TryGet("forbidden_passages", out WinState.EndgameID[] passages))
				{
					ForbiddenPassages = [.. passages];
				}
			}
		}
	}

	internal static class Implementation
	{
		internal static bool SleepAndDeathScreen_AddPassageButton(SleepAndDeathScreen self)
		{
			return self.saveState != null && (!self.saveState.saveStateNumber.TryGetFeature(ExtGameFeatures.DisablePassages, out var passage) || !passage.CanUsePassages);
		}

		internal static void SleepAndDeathScreen_GetDataFromGame(ILCursor c)
		{
			static bool ShowObtainedPassages(bool isRed, KarmaLadderScreen.SleepDeathScreenDataPackage package)
			{
				return isRed && package.characterStats.name.TryGetFeature(ExtGameFeatures.DisablePassages, out var passage) && !passage.CanUsePassages;
			}

			c.TryMoveToNextSlugcatBool(
				nameof(SlugcatStats.Name.Red).GetSlugcatFieldInfo()
				);
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate(ShowObtainedPassages);
		}
	}
}
