using System.Collections.Generic;

namespace ParsingTSV
{
	public struct CellFeed
	{
		private IList<CellData> m_cells;

		public IList<CellData> Cells => m_cells;

		public CellFeed(IList<CellData> cellDatas)
		{
			m_cells = cellDatas;
		}
	}
}
