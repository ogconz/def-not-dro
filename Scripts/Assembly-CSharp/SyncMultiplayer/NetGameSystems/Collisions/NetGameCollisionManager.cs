namespace SyncMultiplayer.NetGameSystems.Collisions
{
	public class NetGameCollisionManager : BaseNetGameSystem
	{
		public enum CollideState
		{
			On = 0,
			Off = 1,
			Suspended = 2
		}

		public const float MainGroupNoCollisionTimerSec = 15f;

		public const float SubroomGroupNoCollisionTimerSec = 5f;

		public ICollisionHandlerStrategy strategy { get; private set; }

		public override void FixedUpdate()
		{
			strategy.FixedUpdate();
		}

		public override void Update()
		{
			strategy.Update();
		}

		public override void LateUpdate()
		{
			strategy.LateUpdate();
		}

		public override void OnGameConnect()
		{
			InitCollisionStrategy();
			OnReconnect();
		}

		public override void OnReconnect()
		{
			strategy.OnReconnect();
		}

		public override void DoPlayerEnter(NetworkPlayer player)
		{
			strategy.DoPlayerEnter(player);
		}

		public override void DoPlayerLeave(NetworkPlayer player)
		{
			strategy.DoPlayerLeave(player);
		}

		public CollideState GetPlayerCollideState(RaceCar car, out float time)
		{
			return strategy.GetColliderState((car != null) ? car.networkPlayer : null, out time);
		}

		public void PlayerSpawned(NetworkPlayer player)
		{
			strategy?.PlayerSpawned(player);
		}

		public void MovePlayerToColliderGroup(string groupName, NetworkPlayer playerToMove)
		{
			strategy.MovePlayerToColliderGroup(groupName, playerToMove);
		}

		public void UpdateCollideState(NetworkPlayer player)
		{
			strategy.UpdateCollideState(player);
		}

		private void InitCollisionStrategy()
		{
			RoomData.CollisionType collisionType = (RoomData.CollisionType)base.owner.host.lastRoomOptions.CollisionType;
			strategy = CollisionStrategyFactory.Create(collisionType);
			strategy.Initialize(base.owner);
		}
	}
}
