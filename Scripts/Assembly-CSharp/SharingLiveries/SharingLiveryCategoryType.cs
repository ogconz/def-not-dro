using System;

namespace SharingLiveries
{
	[Flags]
	public enum SharingLiveryCategoryType : short
	{
		Any = 0,
		Original = 1,
		Replica = 2,
		Itasha = 4,
		Racing = 8,
		Badges = 0x10,
		Abstract = 0x20,
		Colorful = 0x40,
		Minimalistic = 0x80,
		Artistic = 0x100,
		USDM = 0x200,
		EUDM = 0x400,
		AUDM = 0x800,
		ASDM = 0x1000,
		PRO = 0x2000,
		STREET = 0x4000
	}
}
