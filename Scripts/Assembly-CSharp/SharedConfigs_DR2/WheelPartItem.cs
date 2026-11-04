using SimpleJSON;

namespace SharedConfigs_DR2
{
	public class WheelPartItem : BasePartItem
	{
		public int wheelRimId;

		private RimSharedMeta m_rimMeta;

		[JsonIgnore]
		public string name => meta.name;

		[JsonIgnore]
		public string icon => meta.icon;

		[JsonIgnore]
		public string prefab => meta.prefab;

		private RimSharedMeta meta
		{
			get
			{
				if (m_rimMeta == null)
				{
					m_rimMeta = new RimSharedMeta();
				}
				return m_rimMeta;
			}
		}

		public WheelPartItem()
		{
		}

		public WheelPartItem(int id, int cost, int installationCost, CurrencyType currencyType, bool defaultLocked, int wheelRimId)
			: base(installationCost, defaultLocked)
		{
			base.id = id;
			base.cost = cost;
			base.currencyType = currencyType;
			this.wheelRimId = wheelRimId;
		}
	}
}
