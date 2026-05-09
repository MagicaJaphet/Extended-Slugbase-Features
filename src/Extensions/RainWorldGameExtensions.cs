using RWCustom;
using System.IO;
using UnityEngine;

namespace ExtendedSlugbase.Extensions
{
    public static class RainWorldGameExtensions
    {
		private static Texture2D palette0Texture;

		public static bool TryGetDefaultPalette(out Texture2D texture)
		{
			if (palette0Texture == null)
			{
				palette0Texture = new Texture2D(32, 16, TextureFormat.ARGB32, false);
				try
				{
					AssetManager.SafeWWWLoadTexture(ref palette0Texture, "file:///" + AssetManager.ResolveFilePath(Path.Combine("Palettes", "palette0.png")), false, true);
				}
				catch (FileLoadException) { }
			}
			texture = palette0Texture;
			return texture != null;
		}

		public static Color ReturnPaletteOrOffBlack(this Color color)
        {
            if (color == Color.black)
            {
                if (Custom.rainWorld.processManager?.currentMainLoop is RainWorldGame game && game.cameras?[0].paletteTexture is Texture2D currentPalette)
                {
                    return currentPalette.GetPixel(2, 0);
                }
				else if (TryGetDefaultPalette(out var palette))
				{
					return palette.GetPixel(2, 0);
				}
            }
            return color;
        }
    }
}
