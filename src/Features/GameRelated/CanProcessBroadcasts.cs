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

namespace ExtendedSlugbase.Features.GameRelated;
public class CanProcessBroadcasts() : GameFeature<bool>("can_access_whitetokens", JsonUtils.ToBool)
{
	internal static class Implementation
	{
		/// <summary>
		/// Spawns a full <see cref="CollectToken.whiteToken"/> if the slugcat can process broadcasts.
		/// </summary>
		internal static void Room_Loaded(ILCursor c)
		{
			static bool DoesNotHaveBroadcasts(bool isNotSpear, Room self)
			{
				return isNotSpear && (!self.game.TryGetFeature(ExtGameFeatures.CanProcessBroadcasts, out bool canProcess) || !canProcess);
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchCallOrCallvirt(out _),
				x => x.MatchLdsfld(nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear).GetSlugcatFieldInfo()),
				x => x.MatchCallOrCallvirt(out _)
				); // AFTER: if (this.game.IsStorySession && this.game.StoryCharacter != MoreSlugcatsEnums.SlugcatStatsName.Spear)
			c.EmitLdarg0Delegate(DoesNotHaveBroadcasts);

			// Prevent misc broadcast pearls from spawning if recieving broadcasts because why would they spawn
			static bool HasBroadcastsAndIsBroadcastPearl(Room self, DataPearl.AbstractDataPearl.DataPearlType type)
			{
				return type == MoreSlugcatsEnums.DataPearlType.BroadcastMisc && self.game.TryGetFeature(ExtGameFeatures.CanProcessBroadcasts, out bool canProcess) && canProcess;
			}

			c.GotoNext(MoveType.After,
				x => x.MatchStfld(typeof(DataPearl.AbstractDataPearl).GetField(nameof(DataPearl.AbstractDataPearl.hidden)))
				); // AFTER: (abstractPhysicalObject as DataPearl.AbstractDataPearl).hidden = (this.roomSettings.placedObjects[num21].data as PlacedObject.DataPearlData).hidden;

			ILCursor jump = c.CloneAndGoToNext(x => x.MatchBr(out _));
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloc, 90);
			c.EmitDelegate(HasBroadcastsAndIsBroadcastPearl);
			c.Emit(OpCodes.Brtrue, jump.MarkLabel());
		}
	}
}
