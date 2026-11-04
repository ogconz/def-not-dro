using System.Collections.Generic;
using PrefsModels;
using SimpleJSON;

namespace Inventory
{
	public class InventoryModel : BasePrefsModel
	{
		[JsonSerialize]
		private Dictionary<string, InventoryItem> m_items = new Dictionary<string, InventoryItem>();

		[JsonIgnore]
		public IReadOnlyDictionary<string, InventoryItem> items => m_items;

		public InventoryItem AddItem(string id, int count, bool permanentItem, bool addCountIfExist = true)
		{
			if (m_items.ContainsKey(id))
			{
				if (addCountIfExist)
				{
					m_items[id].count += count;
				}
				return m_items[id];
			}
			InventoryItem inventoryItem = new InventoryItem
			{
				id = id,
				count = count,
				permanent = permanentItem
			};
			m_items.Add(id, inventoryItem);
			Save();
			return inventoryItem;
		}

		public void RemoveItem(string id, int count)
		{
			if (m_items.ContainsKey(id) && !m_items[id].permanent)
			{
				m_items[id].count -= count;
				if (m_items[id].count <= 0)
				{
					m_items.Remove(id);
				}
				Save();
			}
		}

		public InventoryItem GetItem(string id)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				return null;
			}
			if (!m_items.ContainsKey(id))
			{
				return null;
			}
			return m_items[id];
		}

		protected override bool CompareRecovePrefs(BasePrefsModel reserveModel)
		{
			if (reserveModel is InventoryModel inventoryModel)
			{
				return m_items == inventoryModel.m_items;
			}
			return true;
		}

		public void ClearAll()
		{
			m_items.Clear();
			Save();
		}
	}
}
