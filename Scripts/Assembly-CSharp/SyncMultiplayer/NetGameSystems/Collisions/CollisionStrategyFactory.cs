namespace SyncMultiplayer.NetGameSystems.Collisions
{
	public static class CollisionStrategyFactory
	{
		public static ICollisionHandlerStrategy Create(RoomData.CollisionType collisionType)
		{
			return collisionType switch
			{
				RoomData.CollisionType.Disabled => new DisabledCollisionStrategy(), 
				RoomData.CollisionType.Classic => new ClassicCollisionStrategy(), 
				RoomData.CollisionType.Dynamic => new DynamicCollisionStrategy(), 
				_ => null, 
			};
		}
	}
}
