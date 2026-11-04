using Unity.Mathematics;

namespace SyncMultiplayer.DynamicCollisions.Structs
{
	public struct Obb
	{
		public float3 center;

		public float3 extents;

		public quaternion rotation;

		public float3 offset;
	}
}
