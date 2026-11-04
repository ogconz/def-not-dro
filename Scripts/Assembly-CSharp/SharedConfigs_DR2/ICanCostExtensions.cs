namespace SharedConfigs_DR2
{
	public static class ICanCostExtensions
	{
		public static bool HasCost(this ICanCost item)
		{
			if (item.cost > 0)
			{
				return item.currencyType != CurrencyType.None;
			}
			return false;
		}
	}
}
