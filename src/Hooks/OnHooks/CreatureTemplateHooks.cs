using System;
using ExtendedSlugbase.Features;
using ExtendedSlugbase.Helpers;
using RWCustom;
using MagicaHookingLibrary.Interfaces;
using MagicaHookingLibrary.Helpers;
using UnityEngine;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class CreatureHooks : IOwnHooks
    {
        public void PreApply()
        {
            On.Creature.InjectPoison += Creature_InjectPoison;
            On.CreatureTemplate.CreatureRelationship_CreatureTemplate += CreatureTemplate_CreatureRelationship_CreatureTemplate;
            On.CreatureTemplate.CreatureRelationship_Creature += CreatureTemplate_CreatureRelationship_Creature;
        }

        private void Creature_InjectPoison(On.Creature.orig_InjectPoison orig, Creature self, float amount, Color poisonColor)
        {
            if (self is Player player && player.TryGetFeature(PlayerFeaturesExt.objectInteractions, out var objectInteractions) && objectInteractions.PoisonImmune)
            {
                return;
            }
            orig(self, amount, poisonColor);
        }


        private CreatureTemplate.Relationship CreatureTemplate_CreatureRelationship_CreatureTemplate(On.CreatureTemplate.orig_CreatureRelationship_CreatureTemplate orig, CreatureTemplate self, CreatureTemplate crit)
        {
            if (crit.type == CreatureTemplate.Type.Slugcat && MiscHelpers.TryGetCurrentGame(out var game) && game.TryGetFeature(GameFeaturesExt.creaturePlayerRelationOverrides, out var overrides) && overrides.TryGetRelationship(self.type, out var relationship))
            {
                return relationship;
            }
            return orig(self, crit);
        }


        private CreatureTemplate.Relationship CreatureTemplate_CreatureRelationship_Creature(On.CreatureTemplate.orig_CreatureRelationship_Creature orig, CreatureTemplate self, Creature crit)
        {
            if (crit is Player player && player.AI == null && crit.room?.game != null && crit.room.game.TryGetFeature(GameFeaturesExt.creaturePlayerRelationOverrides, out var overrides) && overrides.TryGetRelationship(self.type, out var relationship))
            {
                return relationship;
            }
            return orig(self, crit);
        }


        public void OnApply()
        {
        }

        public void PostApply()
        {
        }

    }
}
