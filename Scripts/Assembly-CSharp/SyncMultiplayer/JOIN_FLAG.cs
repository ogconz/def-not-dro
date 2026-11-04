using System;

namespace SyncMultiplayer
{
	[Flags]
	public enum JOIN_FLAG
	{
		ALLOW = 0,
		EMPTY_V = 2,
		WRONG_V = 4,
		CROSS_DISABLE = 8,
		WRONG_TRACK = 0x10
	}
}
