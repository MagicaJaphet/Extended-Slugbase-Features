using ExtendedMenuscenes;
using ExtendedSlugbase.Objects;
using Menu;
using SlugBase.Assets;
using UnityEngine;

namespace ExtendedSlugbase.ExternalWrappers;
internal class ExtendedMenuscenes
{
	internal static void TryApplyColoredMenuIllustration(MenuScene self, CustomScene.Image image, Color? color, float opacity, int? slotIndex, SlugcatStats.Name name)
	{
		if (!self.flatMode)
		{
			int index = self.depthIllustrations.IndexOf(self.depthIllustrations.Find(x => x.fileName.Contains(image.Name)));
			if (index != -1)
			{
				self.depthIllustrations[index].ColorImage(color ?? Color.white, opacity, slotIndex, name);
			}
		}
	}
}
