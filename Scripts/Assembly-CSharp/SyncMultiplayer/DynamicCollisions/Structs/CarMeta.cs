using Unity.Mathematics;

namespace SyncMultiplayer.DynamicCollisions.Structs
{
	public struct CarMeta
	{
		public bool isCurrentUser;

		public bool isCarActive;

		public float3 velocity;

		public float3 angularVelocity;

		public int collideGroupHash;
	}
}
