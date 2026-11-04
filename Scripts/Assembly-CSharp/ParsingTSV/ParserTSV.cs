using System.Collections.Generic;

namespace ParsingTSV
{
	public static class ParserTSV
	{
		private const char NEW_LINE_CHAR = '\n';

		private const char TAB_CHAR = '\t';

		public static void ParseTextRaw(string text, out IReadOnlyList<string> cells, out int rowCount)
		{
			List<string> list = new List<string>();
			string[] array = text.Split('\n');
			rowCount = array.Length;
			for (int i = 0; i < rowCount; i++)
			{
				string[] array2 = array[i].Split('\t');
				list.Capacity += array2.Length;
				for (int j = 0; j < array2.Length; j++)
				{
					list.Add(array2[j].Replace("\r", string.Empty));
				}
			}
			cells = list;
		}

		public static void ParseText(string text, out CellFeed feed)
		{
			List<CellData> list = new List<CellData>();
			string[] array = text.Split('\n');
			uint num = (uint)array.Length;
			for (uint num2 = 0u; num2 < num; num2++)
			{
				ParseRow(array[num2], num2 + 1, out var rowCells);
				list.AddRange(rowCells);
			}
			feed = new CellFeed(list);
		}

		private static void ParseRow(string rowText, uint rowNumber, out CellData[] rowCells)
		{
			string[] array = rowText.Split('\t');
			uint num = (uint)array.Length;
			rowCells = new CellData[num];
			for (uint num2 = 0u; num2 < num; num2++)
			{
				rowCells[num2] = new CellData(rowNumber, num2 + 1, array[num2]);
			}
		}
	}
}
