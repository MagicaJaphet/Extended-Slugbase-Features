using SlugBase;
using System;
using System.Collections.Generic;

namespace ExtendedSlugbaseFeatures
{
	internal class ExtensionResources
	{
		/// <summary>
		/// Extension method to add support for translating custom spear object properties. <paramref name="abstractSpear"/> is the owner object which will be realized, <paramref name="spearProperties"/> is a string Dictionary which uses the name of the spear property.
		/// <para>The string key of <paramref name="spearProperties"/> contains the name of the field, and the object stores any values which would need to be parsed.</para>
		/// </summary>
		/// <param name="abstractSpear"></param>
		/// <param name="spearProperties"></param>
		/// <exception cref="NotImplementedException"></exception>
		public static void HandleCustomAbstractSpearProperties(AbstractSpear abstractSpear, Dictionary<string, object> spearProperties)
		{
			
		}

		/// <summary>
		/// Extension method to add support for adding custom spear object properties from the slugbase JSON file.
		/// <para>The string key of <paramref name="spearProperties"/> contains the name of the field, and the object stores any values which would need to be parsed.</para>
		/// </summary>
		/// <param name="spearProperties"></param>
		/// <param name="spearJSON"></param>
		/// <exception cref="NotImplementedException"></exception>
		public static void HandleCustomSpearJSON(JsonObject spearJSON, Dictionary<string, object> spearProperties)
		{

		}

		/// <summary>
		/// Extension method to handle unrecognized <see cref="AbstractPhysicalObject.AbstractObjectType"/> in the slugbase JSON which have special properties.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsUnrecognizedType(AbstractPhysicalObject.AbstractObjectType type)
		{
			return false;
		}

		/// <summary>
		/// Extension method to add unrecognized <see cref="AbstractPhysicalObject.AbstractObjectType"/> properties from the slugbase JSON.
		/// <para><paramref name="objectProperties"/> stores the string name of the object property, while the object holds the information stored in the property.</para>
		/// </summary>
		/// <param name="objectJSON"></param>
		/// <param name="objectProperties"></param>
		public static void HandleUnrecognizedTypes(JsonObject objectJSON, out Dictionary<string, object> objectProperties)
		{
			objectProperties = [];

			// Add properties here, example below
			/* if (objectJSON.TryGet(nameof(WaterNut.AbstractWaterNut.swollen)) is JsonAny waterType && waterType.TryBool() is bool swollen)
				{
					objectProperties.Add(nameof(WaterNut.AbstractWaterNut.swollen), swollen);
				}
			 */
		}

		internal static AbstractPhysicalObject ParseUnrecognizedTypes(AbstractPhysicalObject.AbstractObjectType type, Dictionary<string, object> objectProperties)
		{
			// Add properties here, example below
			/* if (type == myType) 
			 	{
			 		if (objectProperties != null)
						{
							if (objectProperties.ContainsKey(nameof(AbstractSpear.explosive)) && objectProperties[nameof(AbstractSpear.explosive)] is bool isExplosive)
								{
									spear.explosive = isExplosive;
								}
						}
			 	}
			 */
			return null;
		}
	}
}