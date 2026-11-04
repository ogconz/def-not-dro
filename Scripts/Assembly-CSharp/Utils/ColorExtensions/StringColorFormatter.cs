using UnityEngine;

namespace Utils.ColorExtensions
{
	public static class StringColorFormatter
	{
		private const string COLORED_TEXT_FORMAT_WITH_GRID = "<color=#{1}>{0}</color>";

		private const string COLORED_TEXT_FORMAT = "<color={1}>{0}</color>";

		public static string SetColor(this string text, string color)
		{
			return GetColored(text, color);
		}

		public static string GetColored(string text, string color)
		{
			return string.Format(color.Contains("#") ? "<color={1}>{0}</color>" : "<color=#{1}>{0}</color>", text, color);
		}

		public static string SetColor(this string text, Color color)
		{
			return GetColored(text, color);
		}

		public static string GetColored(string text, Color color)
		{
			string arg = color.ToHexString();
			return string.Format("<color={1}>{0}</color>", text, arg);
		}
	}
}
