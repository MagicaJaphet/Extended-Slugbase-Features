using SlugBase.DataTypes;
using UnityEngine;
using static ExtendedSlugbase.Helpers.CWTHelpers;

namespace ExtendedSlugbase.Objects
{
    public class CWTs
    {
        public class SpearCWT : ExtraDataClass<AbstractSpear, SpearCWT>
        {
            public ColorSlot generatedSpearColor;
            public ColorSlot generatedSpearFadeColor;

			public int? playerNumber;
		}

        public class PlayerCWT : ExtraDataClass<Player, PlayerCWT>
        {
            public int saintTongueSprite;
        }
    }
}
