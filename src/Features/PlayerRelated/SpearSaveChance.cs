using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features.PlayerRelated;
public class SpearSaveChance() : PlayerFeature<float>("spear_save_chance", JsonUtils.ToFloat)
{
	internal static class Implementation
	{
		//FEATURE: Spear damage random save chance (like gourmand)
		//	Spear.HitSomething
	}
}
