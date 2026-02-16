using SlugBase;
using ExtendedSlugbase.Helpers;
using Menu;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using SlugBase.Assets;
using static SlugBase.Assets.CustomScene;
using UnityEngine;
namespace ExtendedSlugbase.Objects
{
    public class GameObjects
    {
		public class ExtCustomScene
		{
			public static Dictionary<CustomScene, ExtCustomScene> ExtCustomScenes { get; } = [];

			public Dictionary<Image, ExtImage> ExtImages = [];

			public ExtCustomScene(CustomScene scene, JsonObject json)
			{
				var images = json.GetList("images");
				foreach (var image in from i in images select i.AsObject())
				{
					Image imgObj = scene.Images.FirstOrDefault(x => x.Name == image.GetString("name"));
					ExtImages.Add(imgObj, new(image));
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

        public class HardMode
        {
            public int Cycles { get; } = 20;
            public MenuScene.SceneID DeathSceneID { get; } = MenuScene.SceneID.Slugcat_Dead_Red;
            public bool HardLimit { get; } = false;

            public HardMode(JsonAny json)
            {
                if (json.TryParse(out JsonObject obj))
                {
                    if (obj.TryGet("cycles", out int cycles))
                    {
                        Cycles = cycles;
                    }
                    if (obj.TryGet("death_menu_scene", out MenuScene.SceneID deathSceneID))
                    {
                        DeathSceneID = deathSceneID;
                    }
                    if (obj.TryGet("hard_cycle_limit", out bool hardLimit))
                    {
                        //LATER: Implement
                        HardLimit = hardLimit;
                    }
                }
            }
        }
    }
}
