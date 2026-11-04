namespace SyncMultiplayer.DynamicCollisions.Tools
{
	public static class PairMath
	{
		public static int GetPairsCount(int totalCount)
		{
			return totalCount * (totalCount - 1) / 2;
		}

		public static int GetPairIndex(int first, int second, int totalCount)
		{
			if (first > second)
			{
				int num = second;
				int num2 = first;
				first = num;
				second = num2;
			}
			return first * (2 * totalCount - first - 1) / 2 + (second - first - 1);
		}
	}
}
