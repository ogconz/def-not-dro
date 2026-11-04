namespace SharedConfigs_DR2
{
	public abstract class BasePartItem : BaseBlockableItem
	{
		public int installationCost;

		public bool defaultLocked;

		public BasePartItem()
		{
			base.currencyType = CurrencyType.Coins;
		}

		public BasePartItem(int installationCost, bool defaultLocked)
		{
			this.installationCost = installationCost;
			this.defaultLocked = defaultLocked;
		}
	}
}
