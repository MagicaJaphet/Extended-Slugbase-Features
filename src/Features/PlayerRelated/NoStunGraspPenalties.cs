using ExtendedSlugbase.Extensions;
using ExtendedSlugbase.Features;
using MagicaHookingLibrary.Helpers;
using MonoMod.Cil;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ExtendedSlugbase.Features.PlayerRelated.CanCraftObjects;

namespace ExtendedSlugbase.Features.PlayerRelated;
public class NoStunGraspPenalties() : PlayerFeature<Dictionary<Creature.DamageType, bool>>("no_stun_grasp_penalty", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static Dictionary<Creature.DamageType, bool> Factory(JsonAny json)
	{
		Dictionary<Creature.DamageType, bool> stunOverrides = [];
		foreach ((string key, JsonAny value) in json.AsObject().GetKeyPairEnumerator())
		{
			if (ExtEnumHelpers.TryGetExtEnum(key, out Creature.DamageType damage))
			{
				stunOverrides.Add(damage, value.AsBool());
			}
			else
			{
				throw new JsonException($"{key} is not a value of Creature.DamageType!", value);
			}
		}
		return stunOverrides;
	}

	internal static class Implementation
	{
		internal static void Player_Stun(ILContext il)
		{
			ILCursor c = new(il);

			static bool IsImmuneToStun(bool isNotBlunt, Player self)
			{
				return isNotBlunt && !(self.TryGetFeature(ExtPlayerFeatures.NoStunGraspPenalties, out var penalities) && penalities.TryGetValue(self.stunDamageType, out bool isImmune) && isImmune);
			}

			for (int i = 0; i < 3; i++)
			{
				c.GotoNext(
					MoveType.After,
					x => x.MatchLdsfld(typeof(Creature.DamageType).GetField(nameof(Creature.DamageType.Blunt))),
					x => x.MatchCallOrCallvirt(out _)
					);
				c.EmitLdarg0Delegate(IsImmuneToStun);
			}
		}
	}
}
