using MagicaHookingLibrary.Interfaces;
using ExtendedSlugbase.Features.GameRelated;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class ConsumableHooks : IOwnHooks
    {
        public void PreApply()
        {
            On.KarmaFlower.CanSpawnKarmaFlower += SpawnKarmaFlowers.Implementation.KarmaFlower_CanSpawnKarmaFlower;
        }

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
