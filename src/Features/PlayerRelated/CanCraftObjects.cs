using ExtendedSlugbase.Extensions;
using MagicaHookingLibrary.Helpers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using SlugBase;
using SlugBase.Features;
using System.Collections.Generic;
using System.Linq;
using static ExtendedSlugbase.DataTypes.AbstractSpawners;

namespace ExtendedSlugbase.Features.PlayerRelated;
public class CanCraftObjects() : PlayerFeature<CanCraftObjects.Craftability>("crafting", Factory)
{
	/// <summary>
	/// Non-anonymous method for stack tracing.
	/// </summary>
	internal static Craftability Factory(JsonAny json) => new(json);

	/// <summary>
	/// JSON Object to handle crafting and regurgitation logic.
	/// </summary>
	public class Craftability
	{
		public bool EatsMeals { get; } = false;
		public int MealBonus { get; } = 1;

		public Dictionary<Ingredient, Dictionary<Ingredient, AbstractObject>> TwoHandedRecipeTable { get; } = [];
		public Dictionary<Ingredient, (int cost, AbstractObject result)> OneHandedRecipeList { get; } = [];
		public Dictionary<Ingredient, (int cost, AbstractObject result)> SwallowRecipeList { get; } = [];
		public Regurgitatable RegurgitateList { get; } = new();
		public SoundID CraftSound { get; } = SoundID.Slugcat_Swallow_Item;


		public class Regurgitatable
		{
			public int cost = 1;
			public List<(AbstractObject obj, float rarity)> objects = [];
		}

		public class Ingredient
		{
			public AbstractPhysicalObject.AbstractObjectType objType;
			public CreatureTemplate.Type critType;

			public Ingredient(PhysicalObject obj)
			{
				if (obj is Creature crit)
				{
					critType = crit.Template.type;
				}
				objType = obj?.abstractPhysicalObject.type;
			}

			public Ingredient(string str, JsonAny any)
			{
				if (!ExtEnumHelpers.TryGetExtEnum(str, out objType) && !ExtEnumHelpers.TryGetExtEnum(str, out critType))
				{
					throw new JsonException($"{str} is not a valid AbstractObjectType or CreatureTemplate.Type!", any);
				}
			}

			public override bool Equals(object obj)
			{
				if (obj is Ingredient b)
				{
					return critType != null && b.critType != null && critType == b.critType || objType != null && b.objType != null && objType == b.objType;
				}
				return base.Equals(obj);
			}

			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
		}

		public bool TryGetOneHandedRecipe(Player player, AbstractPhysicalObject testObj, Ingredient test, out AbstractPhysicalObject resultObj, bool swallowRecipe = false)
		{
			resultObj = null;
			if (test == null)
			{
				return false;
			}

			foreach ((Ingredient key, (int cost, AbstractObject result)) in (swallowRecipe ? SwallowRecipeList : OneHandedRecipeList).Select(x => (x.Key, x.Value)))
			{
				if (key.Equals(test))
				{
					if (player.FoodInStomach >= cost)
					{
						//BUG: Abstract object will stop spawning after a while
						if (result.TryGetObject(player.room.abstractRoom, player.room.GetWorldCoordinate(player.firstChunk.pos), out resultObj))
						{
							if (resultObj is AbstractCreature crit && crit.creatureTemplate.type == key.critType || resultObj.type == key.objType && AbstractObjectsAreEqual(resultObj, testObj))
							{
								return false;
							}
							player.SubtractFood(cost);
							return true;
						}
					}
				}
			}
			return false;
		}

		private bool AbstractObjectsAreEqual(AbstractPhysicalObject a, AbstractPhysicalObject b)
		{
			if (a is AbstractSpear spearA && b is AbstractSpear spearB)
			{
				return spearA.explosive == spearB.explosive
				&& spearA.electric == spearB.electric
				&& spearA.hue == spearB.hue
				&& spearA.poison == spearB.poison;
			}
			return true;
		}


		public bool TryGetRecipeResult(Player player, out bool isOneHanded, out AbstractPhysicalObject obj, out Creature.Grasp chosen)
		{
			chosen = null;
			isOneHanded = true;
			obj = null;
			Creature.Grasp a = player.grasps[0];
			Creature.Grasp b = player.grasps[1];

			if (a == null && b == null)
			{
				return false;
			}
			Ingredient ingredientA = a != null ? new(a.grabbed) : null;
			Ingredient ingredientB = b != null ? new(b.grabbed) : null;

			if (OneHandedRecipeList.Count > 0)
			{
				if (a != null && TryGetOneHandedRecipe(player, a.grabbed.abstractPhysicalObject, ingredientA, out obj))
				{
					chosen = a;
					return true;
				}
				if (b != null && TryGetOneHandedRecipe(player, b.grabbed.abstractPhysicalObject, ingredientB, out obj))
				{
					chosen = b;
					return true;
				}
			}

			// Two handed recipe table
			if (ingredientA == null || ingredientB == null)
			{
				// Don't calculate two handed recipe if one is null
				return false;
			}

			isOneHanded = false;

			foreach ((Ingredient key, var dict) in TwoHandedRecipeTable.Select(x => (x.Key, x.Value)))
			{
				if (key.Equals(ingredientA))
				{
					foreach ((Ingredient secondKey, AbstractObject result) in TwoHandedRecipeTable[key].Select(x => (x.Key, x.Value)))
					{
						if (secondKey.Equals(ingredientB) && result.TryGetObject(player.room.abstractRoom, player.room.GetWorldCoordinate(player.firstChunk.pos), out obj))
						{
							return true;
						}
					}
				}
			}

			if (EatsMeals && GourmandCombos.CraftingResults_ObjectData(a, b, true) == AbstractPhysicalObject.AbstractObjectType.DangleFruit
			&& a?.grabbed is IPlayerEdible edible && SlugcatStats.NourishmentOfObjectEaten(player.SlugCatClass, edible) != -1
			&& b?.grabbed is IPlayerEdible edible2 && SlugcatStats.NourishmentOfObjectEaten(player.SlugCatClass, edible2) != -1)
			{
				return true;
			}

			return false;
		}

		public bool TryGetRegurgitate(Player player, Regurgitatable r, out AbstractPhysicalObject obj)
		{
			obj = null;
			if (r.objects.Count > 0)
			{
				var random = UnityEngine.Random.value;
				var validItemsByRarity = r.objects.Where(x => x.rarity > random).ToList();
				if (validItemsByRarity[UnityEngine.Random.Range(0, validItemsByRarity.Count - 1)].obj.TryGetObject(player.room.abstractRoom, player.room.GetWorldCoordinate(player.firstChunk.pos), out obj))
				{
					return true;
				}
			}
			return false;
		}
		internal Craftability(JsonAny json)
		{
			if (json.TryParse(out JsonObject obj))
			{
				if (obj.TryGet("can_eat_meals", out bool eatsMeals))
				{
					EatsMeals = eatsMeals;
				}
				if (obj.TryGet("meal_bonus", out int bonus))
				{
					MealBonus = bonus;
				}
				if (obj.TryGet("two_handed_recipes", out JsonObject twoHandedRecipeList))
				{
					foreach ((string mainItem, JsonObject ingredientList) in twoHandedRecipeList.GetKeyPairEnumerator().Select(x => (x.key, x.value.AsObject())))
					{
						Ingredient mainIngredient = new(mainItem, twoHandedRecipeList);
						TwoHandedRecipeTable[mainIngredient] = [];

						foreach ((string secondItem, JsonAny any) in ingredientList.GetKeyPairEnumerator())
						{
							Ingredient secondIngredient = new(secondItem, any);
							AbstractObject result = new(any);
							TwoHandedRecipeTable[mainIngredient][secondIngredient] = result;

							// Also add the opposite
							TwoHandedRecipeTable[secondIngredient] = [];
							TwoHandedRecipeTable[secondIngredient][mainIngredient] = result;
						}
					}
				}
				if (obj.TryGet("craft_sound_id", out SoundID craftSound))
				{
					CraftSound = craftSound;
				}
				if (obj.TryGet("one_handed_recipes", out JsonObject oneHandedRecipeList))
				{
					foreach ((string mainItem, JsonObject ingredientList) in oneHandedRecipeList.GetKeyPairEnumerator().Select(x => (x.key, x.value.AsObject())))
					{
						Ingredient mainIngredient = new(mainItem, oneHandedRecipeList);
						OneHandedRecipeList[mainIngredient] = (ingredientList.Get("cost").AsInt(), new(ingredientList.Get("result")));
					}
				}
				if (obj.TryGet("swallow_recipes", out JsonObject swallowRecipeList))
				{
					foreach ((string mainItem, JsonObject ingredientList) in swallowRecipeList.GetKeyPairEnumerator().Select(x => (x.key, x.value.AsObject())))
					{
						Ingredient mainIngredient = new(mainItem, swallowRecipeList);
						SwallowRecipeList[mainIngredient] = (ingredientList.Get("cost").AsInt(), new(ingredientList.Get("result")));
					}
				}
				if (obj.TryGet("regurgitate_list", out JsonObject regObj))
				{
					RegurgitateList.cost = regObj.GetInt("cost");
					RegurgitateList.objects = [.. from i in regObj.GetList("item_pool") let iObj = i.AsObject() select (new AbstractObject(iObj.Get("object")), iObj.Get("rarity").AsFloat())];
				}
			}
		}

	}
	internal static class Implementation
	{
		internal static Craftability CraftingFeature(Player self) => ExtPlayerFeatures.CanCraftObjects.Get(self);

		private static bool CanRegurgitate_Player(bool isGourm, Craftability craft)
		{
			return isGourm || (craft != null && craft.RegurgitateList.objects.Count > 0);
		}

		internal static void Player_GrabUpdate_1(ILCursor c, VariableDefinition craftFeature)
		{
			static bool CanOneHandedCraft(bool isArti, Craftability craft)
			{
				return isArti || craft != null && craft.OneHandedRecipeList.Count > 0;
			}
			c.TryMoveToNextSlugcatBool(
				nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer).GetSlugcatFieldInfo()
				);
			c.EmitFeatureDelegate(craftFeature, CanOneHandedCraft);

			static bool CanRegurgitate(bool isGourmand, Craftability craft, Player self)
			{
				return isGourmand || craft != null && craft.RegurgitateList.objects.Count > 0 && !self.GraspsCanBeCrafted();
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchCallOrCallvirt(typeof(Player).GetProperty(nameof(Player.isGourmand)).GetGetMethod())
				);
			c.EmitFeatureDelegate(craftFeature, CanRegurgitate, true);
		}
		internal static void Player_GrabUpdate_2(ILCursor c, VariableDefinition craftFeature)
		{
			c.GotoNext(
				MoveType.After,
				x => x.MatchCallOrCallvirt(typeof(Player).GetProperty(nameof(Player.isGourmand)).GetGetMethod())
				);
			c.EmitFeatureDelegate(craftFeature, CanRegurgitate_Player);
			c.GotoNext(
				MoveType.After,
				x => x.MatchCallOrCallvirt(typeof(Player).GetProperty(nameof(Player.isGourmand)).GetGetMethod())
				);
			c.EmitFeatureDelegate(craftFeature, CanRegurgitate_Player);

			static int RegurgitateCost(int one, Craftability craft)
			{
				return craft?.RegurgitateList.cost ?? one;
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchLdcI4(1)
				);
			c.EmitFeatureDelegate(craftFeature, RegurgitateCost);
			c.GotoNext(
				MoveType.After,
				x => x.MatchLdcI4(1)
				);
			c.EmitFeatureDelegate(craftFeature, RegurgitateCost);
		}

		internal static void PlayerGraphics_Update(ILContext il)
		{
			ILCursor c = new(il);

			static bool CanRegurgitate_PlayerGraphics(bool isGourm, PlayerGraphics self)
			{
				return isGourm || self.player.TryGetFeature(ExtPlayerFeatures.CanCraftObjects, out var craft) && craft.RegurgitateList.objects.Count > 0;
			}

			c.TryMoveToNextSlugcatBool(
					nameof(MoreSlugcatsEnums.SlugcatStatsName.Gourmand).GetSlugcatFieldInfo()
					);
			c.EmitLdarg0Delegate(CanRegurgitate_PlayerGraphics);
		}

		internal static void Player_Regurgitate(ILContext il)
		{
			ILCursor c = new(il);

			var craftFeature = ExtPlayerFeatures.CanCraftObjects.ImplementFeatureVariable<Craftability, Player>(il, c);
			static bool CanRegurgitate(bool isGourmand, Craftability craft)
			{
				return isGourmand || craft != null && craft.RegurgitateList.objects.Count > 0;
			}

			c.GotoNext(
				MoveType.After,
				x => x.MatchCallOrCallvirt(typeof(Player).GetProperty(nameof(Player.isGourmand)).GetGetMethod())
				);
			c.EmitFeatureDelegate(craftFeature, CanRegurgitate);

			static AbstractPhysicalObject StomachItem(AbstractPhysicalObject gourmItem, Craftability craft, Player self)
			{
				if (craft != null)
				{
					if (craft.TryGetRegurgitate(self, craft.RegurgitateList, out var result))
					{
						return result;
					}
					return null;
				}
				return gourmItem;
			}
			c.GotoNext(
				x => x.MatchStfld(typeof(Player).GetField(nameof(Player.objectInStomach)))
				);
			c.EmitFeatureDelegate(craftFeature, StomachItem, true);
		}

		internal static bool Player_GraspsCanBeCrafted(Player self) => self.TryGetFeature(ExtPlayerFeatures.CanCraftObjects, out var craft) && craft.TryGetRecipeResult(self, out _, out _, out _);

		internal static void Player_SpitUpCraftedObject(On.Player.orig_SpitUpCraftedObject orig, Player self)
		{
			if (self.TryGetFeature(ExtPlayerFeatures.CanCraftObjects, out var craft) && craft.TryGetRecipeResult(self, out bool isOneHanded, out var result, out var chosenGrasp))
			{
				self.room.PlaySound(craft.CraftSound, self.mainBodyChunk);
				// One handed recipes
				if (isOneHanded)
				{
					for (int i = 0; i < self.grasps.Length; i++)
					{
						AbstractPhysicalObject grabbed = self.grasps[i]?.grabbed.abstractPhysicalObject;
						if (grabbed != null && self.grasps[i] == chosenGrasp)
						{
							self.ReleaseGrasp(i);
							grabbed.realizedObject.RemoveFromRoom();
							self.room.abstractRoom.RemoveEntity(grabbed);

							self.room.abstractRoom.AddEntity(result);
							result.RealizeInRoom();
							self.SlugcatGrab(result.realizedObject, self.FreeHand());
							return;
						}
					}
				}
				else if (craft.EatsMeals && GourmandCombos.CraftingResults_ObjectData(self.grasps[0], self.grasps[1], true) == AbstractPhysicalObject.AbstractObjectType.DangleFruit)
				{
					while (self.grasps[0] != null && self.grasps[0].grabbed is IPlayerEdible || self.grasps[1] != null && self.grasps[1].grabbed is IPlayerEdible)
					{
						self.BiteEdibleObject(true);
					}
					self.AddFood(craft.MealBonus);
				}
				else
				{
					self.room.abstractRoom.AddEntity(result);
					result.RealizeInRoom();
					for (int j = 0; j < self.grasps.Length; j++)
					{
						AbstractPhysicalObject toDelete = self.grasps[j].grabbed.abstractPhysicalObject;
						if (self.room.game.session is StoryGameSession game)
						{
							game.RemovePersistentTracker(toDelete);
						}
						self.ReleaseGrasp(j);
						for (int k = toDelete.stuckObjects.Count - 1; k >= 0; k--)
						{
							if (toDelete.stuckObjects[k] is AbstractPhysicalObject.AbstractSpearStick && toDelete.stuckObjects[k].A.type == AbstractPhysicalObject.AbstractObjectType.Spear && toDelete.stuckObjects[k].A.realizedObject != null)
							{
								(toDelete.stuckObjects[k].A.realizedObject as Spear).ChangeMode(Weapon.Mode.Free);
							}
						}
						toDelete.LoseAllStuckObjects();
						toDelete.realizedObject.RemoveFromRoom();
						self.room.abstractRoom.RemoveEntity(toDelete);
					}

					if (self.FreeHand() != -1)
					{
						self.SlugcatGrab(result.realizedObject, self.FreeHand());
					}
				}
				return;
			}
			orig(self);
		}

		internal static void Player_SwallowObject(On.Player.orig_SwallowObject orig, Player self, int grasp)
		{
			var obj = self.grasps[grasp]?.grabbed;
			AbstractPhysicalObject abstractObj = null;

			if (obj != null && self.TryGetFeature(ExtPlayerFeatures.CanCraftObjects, out var craft) && craft.SwallowRecipeList.Count > 0)
			{
				Craftability.Ingredient test = new(obj);
				craft.TryGetOneHandedRecipe(self, obj.abstractPhysicalObject, test, out abstractObj, true);
			}
			orig(self, grasp);

			if (abstractObj != null)
			{
				self.objectInStomach = abstractObj;
			}
		}
	}
}
