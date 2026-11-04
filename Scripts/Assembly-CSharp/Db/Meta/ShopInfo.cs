using System;
using System.Collections.Generic;
using UnityEngine;

namespace DB.Meta
{
	[Serializable]
	public class ShopInfo : IDBMeta
	{
		[SerializeField]
		private int m_id;

		[SerializeField]
		private List<int> m_wheelPrices = new List<int>();

		[SerializeField]
		private List<int> m_rimColorPrice = new List<int>();

		[SerializeField]
		private List<int> m_spokeColorPrice = new List<int>();

		public int id
		{
			get
			{
				return m_id;
			}
			set
			{
				m_id = value;
			}
		}

		public string wheelPrice
		{
			get
			{
				return PriceToString(m_wheelPrices);
			}
			set
			{
				StringToPrices(value, m_wheelPrices);
			}
		}

		public string rimColorPrice
		{
			get
			{
				return PriceToString(m_rimColorPrice);
			}
			set
			{
				StringToPrices(value, m_rimColorPrice);
			}
		}

		public string spokeColorPrice
		{
			get
			{
				return PriceToString(m_spokeColorPrice);
			}
			set
			{
				StringToPrices(value, m_spokeColorPrice);
			}
		}

		public int GetWheelPrice(int rank)
		{
			return GetPrice(m_wheelPrices, rank);
		}

		public int GetRimColorPrice(int rank)
		{
			return GetPrice(m_rimColorPrice, rank);
		}

		public int GetSpokeColorPrice(int rank)
		{
			return GetPrice(m_spokeColorPrice, rank);
		}

		private int GetPrice(List<int> prices, int rank)
		{
			if (prices.Count == 0)
			{
				return 0;
			}
			if (rank < 0)
			{
				return 0;
			}
			if (rank >= prices.Count)
			{
				return prices.Last();
			}
			return prices[rank];
		}

		private string PriceToString(List<int> prices)
		{
			if (prices.Count == 0)
			{
				return "";
			}
			string text = prices[0].ToString();
			for (int i = 1; i < prices.Count; i++)
			{
				text = text + " " + prices[i];
			}
			return text;
		}

		private void StringToPrices(string source, List<int> prices)
		{
			prices.Clear();
			string[] array = source.Split(' ');
			int result = 0;
			string[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				if (int.TryParse(array2[i], out result))
				{
					prices.Add(result);
				}
				else if (prices.Count > 0)
				{
					prices.Add(prices.Last());
				}
				else
				{
					prices.Add(0);
				}
			}
		}
	}
}
