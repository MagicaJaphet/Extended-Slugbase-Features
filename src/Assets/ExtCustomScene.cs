using SlugBase;
using SlugBase.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static SlugBase.Assets.CustomScene;

namespace ExtendedSlugbase.Assets;
public class ExtCustomScene
{
	public static Dictionary<CustomScene, ExtCustomScene> ExtCustomScenes { get; } = [];

	public List<ExtImage> ExtImages = [];

	public ExtCustomScene(CustomScene scene, JsonObject json)
	{
		var images = json.GetList("images");
		foreach (var image in from i in images select i.AsObject())
		{
			Image imgObj = scene.Images.FirstOrDefault(x => x.Name == image.GetString("name"));
			ExtImages.Add(new(image));
		}
	}

	public class ExtImage
	{
		public Color? ImageColor { get; }
		public string SlotName { get; }
		public int? SlotIndex { get; }
		public float Opacity { get; } = 1f;
		public string BackupImage { get; }

		public ExtImage(JsonObject json)
		{
			if (json.TryGet("image_color", out JsonAny any))
			{
				if (any.TryParse(out string slot, throwIfParseError: false))
				{
					SlotName = slot;
				}
				else if (any.TryParse(out int slotIndex, throwIfParseError: false))
				{
					SlotIndex = slotIndex;
				}
				else if (any.TryParse(out Color color))
				{
					ImageColor = color;
				}
			}
			if (json.TryGet("color_opacity", out float opacity))
			{
				Opacity = opacity;
			}
			if (json.TryGet("backup_image", out string backUp))
			{
				BackupImage = backUp;
			}
		}
	}
}
