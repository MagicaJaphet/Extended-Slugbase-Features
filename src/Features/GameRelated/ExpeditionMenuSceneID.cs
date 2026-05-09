using Expedition;
using ExtendedSlugbase.Extensions;
using Menu;
using SlugBase;
using SlugBase.Features;

namespace ExtendedSlugbase.Features.GameRelated;

public class ExpeditionMenuSceneID() : GameFeature<MenuScene.SceneID>("expedition_menu_art", JsonUtils.ToExtEnum<MenuScene.SceneID>)
{
	internal static class Implementation
	{
		internal static void CharacterSelectPage_UpdateSelectedSlugcat(On.Menu.CharacterSelectPage.orig_UpdateSelectedSlugcat orig, CharacterSelectPage self, int num)
		{
			orig(self, num);
			if (num >= 0
				&& num < ExpeditionGame.playableCharacters.Count
				&& ExpeditionGame.playableCharacters[num].TryGetFeature(ExtGameFeatures.ExpeditionMenuSceneID, out var sceneID))
			{
				self.slugcatScene = sceneID;
			}
		}
	}
}