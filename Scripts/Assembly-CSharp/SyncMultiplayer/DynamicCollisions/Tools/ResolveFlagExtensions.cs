using SyncMultiplayer.DynamicCollisions.Structs;
using Unity.Burst;

namespace SyncMultiplayer.DynamicCollisions.Tools
{
	[BurstCompile]
	public static class ResolveFlagExtensions
	{
		public static bool Has(this ResolveFlag flag, in ResolveFlag mask)
		{
			return (flag & mask) != 0;
		}

		public static ResolveFlag Add(this ResolveFlag flag, in ResolveFlag mask)
		{
			return flag | mask;
		}

		public static ResolveFlag Remove(this ResolveFlag flag, in ResolveFlag mask)
		{
			return flag & ~mask;
		}
	}
}
