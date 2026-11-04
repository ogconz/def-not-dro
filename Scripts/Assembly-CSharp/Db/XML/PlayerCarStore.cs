using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using DB.Meta;

namespace DB.XML
{
	public class PlayerCarStore : CachedWebStore<PlayerCar, PlayerCarStore.CarRemoteMeta>
	{
		public class CarRemoteMeta : IDBMeta
		{
			private ObscuredInt m_cost;

			private ObscuredBool m_sellForPremium;

			public int id { get; set; }

			public bool enabled { get; set; }

			public int cost
			{
				get
				{
					return m_cost;
				}
				set
				{
					m_cost = value;
				}
			}

			public bool sellForPremium
			{
				get
				{
					return m_sellForPremium;
				}
				set
				{
					m_sellForPremium = value;
				}
			}
		}

		public PlayerCarStore(List<PlayerCar> baseList)
			: base(baseList)
		{
		}

		protected override void ApplyFilter(PlayerCar item, CarRemoteMeta overridedItem)
		{
			item.enabled = overridedItem.enabled;
			item.cost = overridedItem.cost;
		}

		protected override ObjectLoader<List<CarRemoteMeta>> MakeLoader()
		{
			return new ObjectLoader<List<CarRemoteMeta>>("CarsRemoteCashKey", Constants.GetDayInterval());
		}
	}
}
