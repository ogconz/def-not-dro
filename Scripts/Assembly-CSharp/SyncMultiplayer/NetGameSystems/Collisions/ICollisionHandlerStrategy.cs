namespace SyncMultiplayer.NetGameSystems.Collisions
{
	public interface ICollisionHandlerStrategy
	{
		void Initialize(NetGameSystemsManager owner);

		void FixedUpdate();

		void Update();

		void LateUpdate();

		void PlayerSpawned(NetworkPlayer player);

		void UpdateCollideState(NetworkPlayer player);

		void MovePlayerToColliderGroup(string groupName, NetworkPlayer playerToMove);

		void OnReconnect();

		void DoPlayerEnter(NetworkPlayer player);

		void DoPlayerLeave(NetworkPlayer player);

		NetGameCollisionManager.CollideState GetColliderState(NetworkPlayer player, out float time);
	}
}
