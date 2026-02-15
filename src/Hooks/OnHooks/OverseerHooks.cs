using System;
using ExtendedSlugbase.Features;
using ExtendedSlugbase.Helpers;
using MagicaHookingLibrary.Interfaces;
using UnityEngine;
using MonoMod.RuntimeDetour;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class OverseerHooks : IOwnHooks
    {

        public void PreApply()
        {
            _ = new Hook(typeof(OverseerGraphics).GetProperty(nameof(OverseerGraphics.MainColor), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod(), OverseerColorOverride);
        }

		internal static Color OverseerColorOverride(Func<OverseerGraphics, Color> orig, OverseerGraphics self)
		{
			if (!self.overseer.SafariOverseer && !self.overseer.SandboxOverseer 
            && self.overseer.abstractCreature.world.game.TryGetFeature(GameFeaturesExt.overseerOverwrite, out var overrides) 
            && overrides.TryGetValue((self.overseer.abstractCreature.abstractAI as OverseerAbstractAI).ownerIterator, out var overrideColor))
			{
				return overrideColor;
			}

			return orig(self);
		}
        
        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
