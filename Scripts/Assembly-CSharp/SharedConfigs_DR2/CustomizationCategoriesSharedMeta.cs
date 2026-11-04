using System.Collections.Generic;

namespace SharedConfigs_DR2
{
	public class CustomizationCategoriesSharedMeta : BaseMeta
	{
		public List<CustomizationCategory> partCategories;

		public List<CustomizationCategory> paintCategories;

		public List<CustomizationCategory> vinylCategories;

		public CustomizationCategoriesSharedMeta()
		{
			partCategories = new List<CustomizationCategory>();
			paintCategories = new List<CustomizationCategory>();
			vinylCategories = new List<CustomizationCategory>();
		}

		public CustomizationCategory GetPartCategory(string key)
		{
			return partCategories.Find((CustomizationCategory category) => category.key == key);
		}

		public CustomizationCategory GetPaintCategory(string key)
		{
			return paintCategories.Find((CustomizationCategory category) => category.key == key);
		}

		public CustomizationCategory GetVinylCategory(int id)
		{
			return vinylCategories.Find((CustomizationCategory category) => category.id == id);
		}
	}
}
