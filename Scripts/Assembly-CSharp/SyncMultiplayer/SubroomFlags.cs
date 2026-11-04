using System;

namespace SyncMultiplayer
{
	[Flags]
	public enum SubroomFlags
	{
		None = 0,
		XDSCollide = 1,
		XDSRecord = 2,
		LeaderRandom = 4,
		LeaderCreator = 8,
		LeaderClient = 0x10
	}
}
