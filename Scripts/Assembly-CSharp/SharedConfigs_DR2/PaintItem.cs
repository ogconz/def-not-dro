namespace SharedConfigs_DR2
{
	public class PaintItem : BasePaintItem
	{
		public HSBColor hsb;

		public PaintItem()
		{
		}

		public PaintItem(int id, int cost, CurrencyType currencyType, int sort, HSBColor hsb)
		{
			base.id = id;
			base.cost = cost;
			base.currencyType = currencyType;
			base.sort = sort;
			this.hsb = hsb;
		}
	}
}
