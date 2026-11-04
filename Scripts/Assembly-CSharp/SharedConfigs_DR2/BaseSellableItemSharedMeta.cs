namespace SharedConfigs_DR2
{
	public abstract class BaseSellableItemSharedMeta : BaseMeta, ICanCost
	{
		public int cost { get; set; }

		public CurrencyType currencyType { get; set; }
	}
}
