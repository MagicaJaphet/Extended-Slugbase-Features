using System;
using ExtendedSlugbase.Features;
using ExtendedSlugbase.Helpers;
using MagicaHookingLibrary.Helpers;
using MagicaHookingLibrary.Interfaces;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using static ExtendedSlugbase.Objects.PlayerObjects;
using System.Collections.Generic;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class ConsumableHooks : IOwnHooks
    {
        public void PreApply()
        {
            On.KarmaFlower.CanSpawnKarmaFlower += KarmaFlower_CanSpawnKarmaFlower;
        }
        
        private static bool KarmaFlower_CanSpawnKarmaFlower(On.KarmaFlower.orig_CanSpawnKarmaFlower orig, Room room)
		{
			return orig(room) && (!room.game.TryGetFeature(GameFeaturesExt.spawnKarmaFlowers, out bool shouldSpawn) || shouldSpawn);
		}

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
