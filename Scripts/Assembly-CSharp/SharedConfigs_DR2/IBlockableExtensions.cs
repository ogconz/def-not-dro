namespace SharedConfigs_DR2
{
	public static class IBlockableExtensions
	{
		public static bool HasBlocker(this IBlockable item)
		{
			if (item.blocker != null)
			{
				return item.blocker.blockType != BlockType.None;
			}
			return false;
		}

		public static BlockType GetBlockType(this IBlockable item)
		{
			if (item.blocker == null)
			{
				return BlockType.None;
			}
			return item.blocker.blockType;
		}
	}
}
