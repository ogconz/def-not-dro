using System;

namespace TrackLayout
{
	[Flags]
	public enum IgnoreFlags
	{
		None = 0,
		Background = 1,
		PerfectLine = 2,
		Checkpoints = 4,
		Sectors = 8,
		ClipZones = 0x10,
		Rules = 0x20
	}
}
