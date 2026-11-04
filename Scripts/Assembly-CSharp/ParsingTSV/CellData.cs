namespace ParsingTSV
{
	public struct CellData
	{
		private uint m_row;

		private uint m_column;

		private string m_value;

		public uint Row => m_row;

		public uint Column => m_column;

		public string Value => m_value;

		public CellData(uint row, uint column, string value)
		{
			m_row = row;
			m_column = column;
			m_value = (string.IsNullOrEmpty(value) ? null : value);
		}
	}
}
