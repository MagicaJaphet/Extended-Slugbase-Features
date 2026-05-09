using MagicaHookingLibrary.Interfaces;
using ExtendedSlugbase.Features.GameRelated;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class RainWorldGameHooks : IOwnHooks
    {

        public void PreApply()
        {
            On.RainWorldGame.GoToRedsGameOver += RainWorldGame_GoToRedsGameOver;
            On.RainWorldGame.TryGetPlayerStartPos += StartingSpawnPositions.Implementation.RainWorldGame_TryGetPlayerStartPos;
        }

        private void RainWorldGame_GoToRedsGameOver(On.RainWorldGame.orig_GoToRedsGameOver orig, RainWorldGame self)
        {
            if (self.manager.upcomingProcess != null)
            {
                return;
			}
			self.manager.musicPlayer?.FadeOutAllSongs(20f);

			CycleLimit.Implementation.RainWorldGame_GoToRedsGameOver(self);

			orig(self);
        }

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
