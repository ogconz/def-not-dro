using SimpleJSON;
using UnityEngine;

namespace SharedConfigs_DR2
{
	public abstract class BasePaintItem : BaseBlockableItem
	{
		[JsonIgnore]
		public const float DefaultCostCoefficient = 1f;

		public int sort;

		public BasePaintItem()
		{
			base.currencyType = CurrencyType.Coins;
		}

		public int CalculateCost(float categoryCoefficient, float materialCoefficient)
		{
			return Mathf.RoundToInt((float)base.cost * categoryCoefficient * materialCoefficient);
		}
	}
}
