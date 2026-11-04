using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

namespace DB.XML
{
	public static class XmlTools
	{
		public const char SEPARATOR = ',';

		public const string SEPARATOR_STRING = ",";

		public const string TWO_TEMPLATE = "{0},{1}";

		private const string COLOR_TEMPLATE = "{0},{1},{2},{3}";

		private const string GOLD_SILVER_BRONZE_TEMPLATE = "{0},{1},{2}";

		public static string ListIntToString(List<int> src)
		{
			if (src.Count == 0)
			{
				return "";
			}
			string text = src[0].ToString();
			for (int i = 1; i < src.Count; i++)
			{
				text = text + " " + src[i];
			}
			return text;
		}

		public static List<int> StringToListInt(string src)
		{
			List<int> list = new List<int>();
			if (!string.IsNullOrEmpty(src))
			{
				string[] array = src.Split(' ');
				foreach (string s in array)
				{
					list.Add(int.Parse(s));
				}
			}
			return list;
		}

		public static string CostToString(int cost, bool isPremium)
		{
			return $"{cost},{isPremium}";
		}

		public static void ParseCost(string str, out int cost, out bool isPremium)
		{
			string[] array = str.Split(',');
			cost = int.Parse(array[0]);
			isPremium = bool.Parse(array[1]);
		}

		public static void ParseCost(string str, out ObscuredInt cost, out ObscuredBool isPremium)
		{
			string[] array = str.Split(',');
			cost = int.Parse(array[0]);
			isPremium = bool.Parse(array[1]);
		}

		public static string ColorToString(Color32? color)
		{
			if (color.HasValue)
			{
				return $"{color.Value.r},{color.Value.g},{color.Value.b},{color.Value.a}";
			}
			return null;
		}

		public static Color32? ParseColor(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				return null;
			}
			string[] array = str.Split(',');
			return new Color32(byte.Parse(array[0]), byte.Parse(array[1]), byte.Parse(array[2]), byte.Parse(array[3]));
		}

		public static string GoldSilverBronzeToString(int gold, int silver, int bronze)
		{
			return $"{gold},{silver},{bronze}";
		}

		public static void ParseGoldSilverBronze(string str, out int gold, out int silver, out int bronze)
		{
			string[] array = str.Split(',');
			gold = int.Parse(array[0]);
			silver = int.Parse(array[1]);
			bronze = int.Parse(array[2]);
		}
	}
}
