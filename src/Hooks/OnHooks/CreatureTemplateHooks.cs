using MagicaHookingLibrary.Interfaces;
using ExtendedSlugbase.Features.GameRelated;
using ExtendedSlugbase.Features.PlayerRelated;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class CreatureHooks : IOwnHooks
    {
        public void PreApply()
        {
            On.Creature.InjectPoison += ObjectInteractions.Implementation.Creature_InjectPoison;
            On.CreatureTemplate.CreatureRelationship_CreatureTemplate += CreaturePlayerRelationOverrides.Implementation.CreatureTemplate_CreatureRelationship_CreatureTemplate;
            On.CreatureTemplate.CreatureRelationship_Creature += CreaturePlayerRelationOverrides.Implementation.CreatureTemplate_CreatureRelationship_Creature;
        }

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }

    }
}
