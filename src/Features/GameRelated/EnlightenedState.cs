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

namespace ExtendedSlugbase.Features.GameRelated;
public class EnlightenedState() : GameFeature<bool>("enlightened", JsonUtils.ToBool)
{
	internal static class Implementation
	{
		/// <summary>
		/// Allows <see cref="Ghost"/> to speak if the slugcat has the mark, or is enlightened.
		/// </summary>
		internal static void Ghost_Update(ILCursor c)
		{
			static bool CanTalkToGhosts(bool hasMark, Ghost self)
			{
				return hasMark || self.room.game.StoryCharacter.TryGetFeature(ExtGameFeatures.EnlightenedState, out bool enlightened) && enlightened;
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchLdfld(typeof(DeathPersistentSaveData).GetField(nameof(DeathPersistentSaveData.theMark)))
				); // if (this.room.game.session is StoryGameSession && ((this.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.theMark
			c.EmitLdarg0Delegate(CanTalkToGhosts);
		}

		/// <summary>
		/// Uses <see cref="SLOracleBehaviorHasMark"/> if the slugcat has the mark, or is enlightened.
		/// </summary>
		internal static void Oracle_ctor(ILCursor c)
		{
			static bool CanTalkToGhosts(bool hasMark, Oracle self)
			{
				return hasMark || self.room.game.StoryCharacter.TryGetFeature(ExtGameFeatures.EnlightenedState, out bool enlightened) && enlightened;
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchLdfld(typeof(DeathPersistentSaveData).GetField(nameof(DeathPersistentSaveData.theMark)))
				); // if (this.room.game.session is StoryGameSession && ((this.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.theMark
			c.EmitLdarg0Delegate(CanTalkToGhosts);
		}

		/// <summary>
		/// Allows the slugcat to see void spawn.
		/// </summary>
		internal static bool SaveState_CanSeeVoidSpawn(Func<SaveState, bool> orig, SaveState save)
		{
			return orig(save) || save.saveStateNumber.TryGetFeature(ExtGameFeatures.EnlightenedState, out bool enlightened) && enlightened;
		}
	}
}
