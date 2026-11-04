using System;

namespace SyncMultiplayer
{
	[Flags]
	public enum DataFrameFlags : byte
	{
		None = 0,
		Finished = 1,
		Inactive = 2
	}
}
