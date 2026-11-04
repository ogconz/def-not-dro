using System.Collections.Generic;
using PrefsModels;
using SimpleJSON;

namespace Quests.Data.Models
{
	public class QuestsInventoryModel : BasePrefsModel
	{
		public enum InventoryType
		{
			None = -1,
			Vinyls = 0,
			VinylCategories = 1,
			RewardAvailableCars = 2
		}

		[JsonSerialize]
		private readonly List<int> m_vinyls = new List<int>();

		[JsonSerialize]
		private readonly List<int> m_vinylCategories = new List<int>();

		[JsonSerialize]
		private readonly List<int> m_rewardAvailableCars = new List<int>();

		[JsonIgnore]
		public List<int> vinyls => m_vinyls;

		[JsonIgnore]
		public List<int> vinylCategories => m_vinylCategories;

		[JsonIgnore]
		public List<int> rewardAvailableCarItems => m_rewardAvailableCars;

		public void AddInventory(int id, InventoryType inventoryType)
		{
			List<int> list = null;
			switch (inventoryType)
			{
			case InventoryType.Vinyls:
				list = m_vinylCategories;
				break;
			case InventoryType.VinylCategories:
				list = m_vinylCategories;
				break;
			case InventoryType.RewardAvailableCars:
				list = m_rewardAvailableCars;
				break;
			}
			if (list != null && !list.Contains(id))
			{
				list.Add(id);
			}
		}

		public void ClearAll()
		{
			m_vinyls.Clear();
			m_vinylCategories.Clear();
			m_rewardAvailableCars.Clear();
			Save();
		}
	}
}
