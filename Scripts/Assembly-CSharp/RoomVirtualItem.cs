using System;

public class RoomVirtualItem
{
	[Flags]
	public enum RoomSettingFlags : byte
	{
		NONE = 0,
		DRIFT_ALLOWED = 1,
		TIME_ALLOWED = 2,
		VOTE_ALLOWED = 4,
		CLIPPING_DUEL_ALLOWED = 8,
		COLLISIONS_ACTIVE = 0x10,
		CHAMPIONSHIP_MODE = 0x20,
		EXTERNAL_TRACK = 0x40,
		ALL = 0xF
	}
}
