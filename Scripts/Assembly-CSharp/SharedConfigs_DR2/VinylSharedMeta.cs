using SimpleJSON;
using UnityEngine;

namespace SharedConfigs_DR2
{
	public class VinylSharedMeta : BaseBlockableItemSharedMeta
	{
		[JsonIgnore]
		public const int VinylPresetsCategoryId = -1;

		public string texturePath;

		public string flippedTexturePath;

		public float minSize;

		public float maxSize;

		public Color[] defaultColors;

		public int categoryId;

		public int initAngle;

		public int sort;

		public bool defaultLocked;

		public VinylSharedMeta()
		{
			base.currencyType = CurrencyType.Coins;
		}

		public VinylSharedMeta(int id, int cost, CurrencyType currencyType, bool defaultLocked, int sort, string texturePath, string flippedTexturePath, float minSize, float maxSize, Color[] defaultColors, int categoryId)
		{
			base.id = id;
			base.cost = cost;
			base.currencyType = currencyType;
			this.defaultLocked = defaultLocked;
			this.sort = sort;
			this.texturePath = texturePath;
			this.flippedTexturePath = flippedTexturePath;
			this.minSize = minSize;
			this.maxSize = maxSize;
			this.defaultColors = defaultColors;
			this.categoryId = categoryId;
		}
	}
}
