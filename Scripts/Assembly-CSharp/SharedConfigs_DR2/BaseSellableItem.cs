namespace SharedConfigs_DR2
{
	public abstract class BaseSellableItem : ICanCost
	{
		public int cost { get; set; }

		public CurrencyType currencyType { get; set; }
	}
}
