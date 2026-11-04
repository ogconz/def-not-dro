using SimpleJSON;

namespace SharedConfigs_DR2
{
	public class CustomizationCategory
	{
		public string key;

		public string name;

		public string icon;

		public bool visible;

		public int sort;

		[JsonIgnore]
		public int id
		{
			get
			{
				int result = 0;
				int.TryParse(key, out result);
				return result;
			}
		}

		public CustomizationCategory()
		{
		}

		public CustomizationCategory(string key, string name, string icon, bool visible, int sort)
		{
			this.key = key;
			this.name = name;
			this.icon = icon;
			this.visible = visible;
			this.sort = sort;
		}
	}
}
