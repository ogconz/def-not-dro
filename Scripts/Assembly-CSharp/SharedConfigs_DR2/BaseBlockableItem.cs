namespace SharedConfigs_DR2
{
	public abstract class BaseBlockableItem : BaseSellableItem, IBlockable
	{
		public string blockerLevel;

		private Blocker m_blocker;

		public int id { get; set; }

		public Blocker blocker
		{
			get
			{
				if (m_blocker == null && !string.IsNullOrEmpty(blockerLevel))
				{
					m_blocker = new Blocker();
					m_blocker.blockType = BlockType.Level;
					m_blocker.data = blockerLevel;
				}
				return m_blocker;
			}
			set
			{
				if (value.blockType == BlockType.Level)
				{
					if (m_blocker != null)
					{
						m_blocker.data = value.data;
					}
					blockerLevel = value.data;
				}
				else
				{
					m_blocker = value;
				}
			}
		}
	}
}
