using Unity.Collections;

namespace SyncMultiplayer.DynamicCollisions.Structs
{
	public struct DataState
	{
		public NativeArray<ResolveFlag> resolveMap;

		public NativeArray<bool> desiredCollideStates;

		public NativeArray<bool> commitedCollideStates;

		public NativeArray<bool> lockCollideStates;

		public NativeArray<bool> submitCollideStates;

		public NativeArray<int> initiators;

		public NativeArray<bool> collideRestrictions;

		public NativeArray<ushort> adjacency;

		public NativeList<int> activeSlots;

		public NativeArray<CarMeta> carsMeta;

		public NativeArray<Obb> carsRawTransforms;

		public NativeArray<Obb> carsObb;

		public NativeArray<Obb> velocitySweptObb;

		public int activeCarsCount;

		public int maxCarsCount;
	}
}
