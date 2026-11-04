using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inventory
{
	public class ItemsAsset : ScriptableObject
	{
		[Serializable]
		public class ItemMeta
		{
			public string id;

			public Sprite icon;

			public bool permanent;
		}

		[SerializeField]
		private List<ItemMeta> m_items = new List<ItemMeta>();

		private readonly Dictionary<string, ItemMeta> m_itemsDictionary = new Dictionary<string, ItemMeta>();

		public IReadOnlyCollection<ItemMeta> items => m_items;

		public static ItemsAsset LoadDB()
		{
			ItemsAsset itemsAsset = ResourcesWrapper.Load<ItemsAsset>("DB/ItemsAsset");
			if (itemsAsset == null)
			{
				return itemsAsset;
			}
			foreach (ItemMeta item in itemsAsset.m_items)
			{
				itemsAsset.m_itemsDictionary[item.id] = item;
			}
			return itemsAsset;
		}

		public ItemMeta Find(string id)
		{
			if (!m_itemsDictionary.TryGetValue(id, out var value))
			{
				value = m_items.Find((ItemMeta x) => x.id == id);
				m_itemsDictionary.Add(id, value);
			}
			return value;
		}
	}
}
