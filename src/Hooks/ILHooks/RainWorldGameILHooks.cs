using System;
using System.Linq;
using ExtendedSlugbase.Features;
using ExtendedSlugbase.Helpers;
using MagicaHookingLibrary.Interfaces;
using RWCustom;
using SlugBase.Features;
using MonoMod.Cil;
using MoreSlugcats;
using static MagicaHookingLibrary.Helpers.HookHelpers;
using Mono.Cecil.Cil;
using static ExtendedSlugbase.Objects.PlayerObjects;
using MagicaHookingLibrary.Helpers;
namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class RainWorldGameILHooks : IOwnHooks
    {

        public void PreApply()
        {
			IL.RainWorldGame.Update += ILAction(RainWorldGame_Update);
            IL.RainWorldGame.RawUpdate += ILAction(RainWorldGame_RawUpdate);
        }

		private static void RainWorldGame_Update(ILCursor c)
		{
            // Get Karma From Scavengers: Main implementation
            static bool GetsKarmaFromScavs(bool isArtificer, RainWorldGame self)
            {
                return isArtificer || (self.TryGetFeature(GameFeaturesExt.getKarmaFromScavs, out bool getKarma) && getKarma);
            }

            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer).GetSlugcatFieldInfo()
                ); // AFTER: if (ModManager.MSC && this.Players.Count > 0 && this.IsStorySession && this.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
            c.EmitLdarg0Delegate(GetsKarmaFromScavs);
        }


        private void RainWorldGame_RawUpdate(ILCursor c)
        {
            static float FramesPerSecondMushroom(float orig, RainWorldGame self)
            {
                if (ObjectInteractions.lastAteMushroomFPS is float fps)
                {
                    return fps;
                }
                return orig;
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchConvR4(),
                x => x.MatchLdcR4(15)
                ); // float num2 = flag ? Mathf.Lerp((float)this.framesPerSecond, 8f, num) : Mathf.Lerp((float)this.framesPerSecond, 15f, num);
            c.EmitLdarg0Delegate(FramesPerSecondMushroom);
        }

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
