using System;

namespace ArrayExtension
{
	public static class Tools
	{
		private const int PRECACHE_INIT_SIZE = 128;

		private const int ADD_SIZE = 64;

		public static bool[] PreCache = new bool[128];

		public static int[] PreCacheInt = new int[128];

		public static void MakeSureSize<T>(ref T[] array, int size)
		{
			while (array.Length < size)
			{
				int num = array.Length;
				do
				{
					num += 64;
				}
				while (num < size);
				GameLog.Print(AlertLevel.Warning, "Debug", $"ArrayExtension: PreCache/PreCacheInt depleted, re-allocate! ({array.Length} => {num})");
				Array.Resize(ref array, num);
			}
		}
	}
}
