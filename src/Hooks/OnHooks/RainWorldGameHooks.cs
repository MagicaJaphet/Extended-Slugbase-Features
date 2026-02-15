using System;
using System.Linq;
using ExtendedSlugbase.Features;
using ExtendedSlugbase.Helpers;
using MagicaHookingLibrary.Interfaces;
using RWCustom;
using SlugBase.Features;
using System.Collections.Generic;
using SlugBase.SaveData;
using SlugBase.Assets;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class RainWorldGameHooks : IOwnHooks
    {

        public void PreApply()
        {
            On.RainWorldGame.GoToRedsGameOver += RainWorldGame_GoToRedsGameOver;
            On.RainWorldGame.TryGetPlayerStartPos += TryGetSlugbaseCharacterStartPos;
        }

        private void RainWorldGame_GoToRedsGameOver(On.RainWorldGame.orig_GoToRedsGameOver orig, RainWorldGame self)
        {
            if (self.manager.upcomingProcess != null)
            {
                return;
            }
            self.manager.musicPlayer?.FadeOutAllSongs(20f);
            if (self.Players[0].realizedCreature is Player red)
            {
                if (red.redsIllness != null)
                {
                    red.redsIllness.fadeOutSlow = true;
                }
            }
            if (self.TryGetFeature(GameFeaturesExt.cycleLimit, out var hardMode))
            {
				CustomScene.SetSelectMenuScene(self.GetStorySession.saveState, hardMode.DeathSceneID);
				if (ModManager.CoopAvailable)
                {
					int num = 0;
					using IEnumerator<Player> enumerator = (from x in self.Players select x.realizedCreature as Player).GetEnumerator();
					while (enumerator.MoveNext())
					{
						Player player = enumerator.Current;
						self.GetStorySession.saveState.AppendCycleToStatistics(player, self.GetStorySession, true, num);
						num++;
					}
				}
				else 
					self.GetStorySession.saveState.AppendCycleToStatistics(self.Players[0].realizedCreature as Player, self.GetStorySession, true, 0);
            }
            orig(self);
        }

        private bool TryGetSlugbaseCharacterStartPos(On.RainWorldGame.orig_TryGetPlayerStartPos orig, string room, out IntVector2 pos)
        {
            var result = orig(room, out pos);

            if (Custom.rainWorld.inGameSlugCat is SlugcatStats.Name name
            && name.TryGetFeature(GameFeatures.StartRoom, out string[] rooms)
            && name.TryGetFeature(GameFeaturesExt.possibleSpawnPositons, out IntVector2[] positions))
            {
                // Finds the room name in the rooms array by weakly comparing all of the names in the rooms array :|
                var roomIndex = rooms.IndexOf(rooms.FirstOrDefault(r => string.Equals(r, room, StringComparison.InvariantCultureIgnoreCase)));
                if (roomIndex > -1 && roomIndex < positions.Length)
                {
                    pos = positions[roomIndex];
                    return true;
                }
            }

            return result;
        }

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
