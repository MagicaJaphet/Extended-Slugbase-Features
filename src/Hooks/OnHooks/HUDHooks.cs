using ExtendedSlugbase.Assets;
using ExtendedSlugbase.Features.GameRelated;
using ExtendedSlugbase.Features.TimelineRelated;
using MagicaHookingLibrary.Interfaces;
using SlugBase;
using UnityEngine;

namespace ExtendedSlugbase.Hooks.OnHooks
{
    public class HUDHooks : IOwnHooks
    {
        public void PreApply()
        {
            On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.HasUniqueSprite += JollyMenu_SymbolButtonTogglePupButton_HasUniqueSprite;
            On.JollyCoop.JollyMenu.JollyPlayerSelector.GetPupButtonOffName += JollyMenu_JollyPlayerSelector_GetPupButtonOffName;
            On.JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyDeathBump.ctor += JollyHUD_JollyPlayerSpecificHud_JollyDeathBump_ctor;
            On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.Update += JollyHUD_JollyMeter_PlayerIcon_Update;
            On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.ctor += JollyHUD_JollyMeter_PlayerIcon_ctor;
            On.HUD.Map.CycleLabel.UpdateCycleText += CycleLabel_UpdateCycleText;
            On.HUD.RainMeter.ctor += RainMeter_ctor;
			On.HUD.RainMeter.Update += RainMeter_Update;
			On.HUD.RainMeter.Draw += RainMeter_Draw;
        }

        private bool JollyMenu_SymbolButtonTogglePupButton_HasUniqueSprite(On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.orig_HasUniqueSprite orig, JollyCoop.JollyMenu.SymbolButtonTogglePupButton self)
        {
			if (self.owner is JollyCoop.JollyMenu.JollyPlayerSelector selector && SlugBaseCharacter.TryGet(selector.JollyOptions(selector.index).PlayerClass, out var slugBase))
			{
				return AtlasManager.TryGetElement(slugBase, AtlasManager.FixedSpriteElements.JollyPlayerUniqueIcon, out _);
			}
            return orig(self);
        }


        private string JollyMenu_JollyPlayerSelector_GetPupButtonOffName(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_GetPupButtonOffName orig, JollyCoop.JollyMenu.JollyPlayerSelector self)
        {
			SlugcatStats.Name playerClass = self.JollyOptions(self.index).PlayerClass;
			if (SlugBaseCharacter.TryGet(playerClass, out var slugBase)
				&& AtlasManager.TryGetElement(slugBase, AtlasManager.FixedSpriteElements.JollyPlayerIcon, out _) || (AtlasManager.TryGetElement(slugBase, AtlasManager.FixedSpriteElements.JollyPlayerUniqueIcon, out _))) 
			{
				return AtlasManager.GetNameKey(AtlasManager.FixedSpriteElements.JollyPlayerIcon, slugBase);
			}

            return orig(self);
        }


        private void JollyHUD_JollyPlayerSpecificHud_JollyDeathBump_ctor(On.JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyDeathBump.orig_ctor orig, JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyDeathBump self, JollyCoop.JollyHUD.JollyPlayerSpecificHud jollyHud)
        {
           	orig(self, jollyHud);
			
			if (jollyHud.abstractPlayer?.realizedCreature is Player player && SlugBaseCharacter.Registry.TryGet(player.SlugCatClass, out var slugBase) 
			&& AtlasManager.TryGetElement(slugBase, AtlasManager.FixedSpriteElements.JollyIconDead, out var element))
			{
				self.symbolSprite.SetElementByName(element);
			}
        }


        private void JollyHUD_JollyMeter_PlayerIcon_Update(On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.orig_Update orig, JollyCoop.JollyHUD.JollyMeter.PlayerIcon self)
        {
			orig(self);

			if (self.iconSprite.element.name == "Multiplayer_Death" && self.player?.realizedCreature is Player player && SlugBaseCharacter.Registry.TryGet(player.SlugCatClass, out var slugBase) 
			&& AtlasManager.TryGetElement(slugBase, AtlasManager.FixedSpriteElements.JollyIconDead, out var element))
			{
				self.iconSprite.SetElementByName(element);
			}
		}
        


        private void JollyHUD_JollyMeter_PlayerIcon_ctor(On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.orig_ctor orig, JollyCoop.JollyHUD.JollyMeter.PlayerIcon self, JollyCoop.JollyHUD.JollyMeter meter, AbstractCreature associatedPlayer, Color color)
        {
            orig(self, meter, associatedPlayer, color);

			if (associatedPlayer?.realizedCreature is Player player && SlugBaseCharacter.Registry.TryGet(player.SlugCatClass, out var slugBase)
				&& AtlasManager.TryGetElement(slugBase, AtlasManager.FixedSpriteElements.JollyIcon, out var element))
			{
				self.iconSprite.SetElementByName(element);
			}
        }


        private void CycleLabel_UpdateCycleText(On.HUD.Map.CycleLabel.orig_UpdateCycleText orig, HUD.Map.CycleLabel self)
        {
            orig(self);

			CycleLimit.Implementation.HUD_Map_CycleLabel_UpdateCycleText(self);
        }

		
		private static void RainMeter_ctor(On.HUD.RainMeter.orig_ctor orig, HUD.RainMeter self, HUD.HUD hud, FContainer fContainer)
		{
			orig(self, hud, fContainer);
			if (ShowRainTimer.Implementation.NoRainTimer(hud))
			{
				self.halfTimeShown = true;
			}
		}

        private static void RainMeter_Update(On.HUD.RainMeter.orig_Update orig, HUD.RainMeter self)
		{
			if (ShowRainTimer.Implementation.NoRainTimer(self.hud))
			{
				self.halfTimeShown = true;
			}
			orig(self);
		}

		
		private static void RainMeter_Draw(On.HUD.RainMeter.orig_Draw orig, HUD.RainMeter self, float timeStacker)
		{
			if (ShowRainTimer.Implementation.NoRainTimer(self.hud))
			{
				return;
			}
			orig(self, timeStacker);
		}

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
