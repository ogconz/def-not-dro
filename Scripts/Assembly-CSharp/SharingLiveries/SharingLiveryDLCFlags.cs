using System;

namespace SharingLiveries
{
	[Flags]
	public enum SharingLiveryDLCFlags : byte
	{
		Any = 0,
		None = 1,
		StreetTuners = 2,
		HitTheWall = 4,
		YoungTimers = 8,
		HalloweenVinyls = 0x10,
		ProDriftStickersVinyls = 0x20,
		StreetClassStickersVinyls = 0x40,
		NewYearStickers2020 = 0x80
	}
}
