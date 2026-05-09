using Menu;
using SlugBase;
using SlugBase.Assets;
using SlugBase.Features;

namespace ExtendedSlugbase.Features.GameRelated;

public class ExpeditionMenuSceneID() : GameFeature<MenuScene.SceneID>("expedition_menu_art", JsonUtils.ToExtEnum<MenuScene.SceneID>)
{
	internal static class Implementation
	{
		//FEATURE: Expedition menu scene art
	}
}