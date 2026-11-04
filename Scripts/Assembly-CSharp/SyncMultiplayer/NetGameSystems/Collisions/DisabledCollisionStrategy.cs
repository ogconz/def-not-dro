namespace SyncMultiplayer.NetGameSystems.Collisions
{
	public class DisabledCollisionStrategy : ICollisionHandlerStrategy
	{
		private NetGameIgnoreSystem m_ignoreSystem;

		private NetworkGame m_networkGame;

		private NetworkPlayer[] m_slots = new NetworkPlayer[16];

		public void Initialize(NetGameSystemsManager owner)
		{
			m_ignoreSystem = owner.Get<NetGameIgnoreSystem>();
			m_networkGame = owner.host;
		}

		public void DoPlayerEnter(NetworkPlayer player)
		{
			for (int i = 0; i < m_slots.Length; i++)
			{
				if (m_slots[i] == null)
				{
					m_slots[i] = player;
					player.SlotID = (byte)i;
					break;
				}
			}
			m_ignoreSystem.UpdateVisibilityPlayers();
		}

		public void DoPlayerLeave(NetworkPlayer player)
		{
			if (m_slots[player.SlotID] == player)
			{
				m_slots[player.SlotID] = null;
			}
		}

		public void FixedUpdate()
		{
		}

		public void Update()
		{
		}

		public void LateUpdate()
		{
		}

		public NetGameCollisionManager.CollideState GetColliderState(NetworkPlayer player, out float time)
		{
			time = 0f;
			return NetGameCollisionManager.CollideState.On;
		}

		public void MovePlayerToColliderGroup(string groupName, NetworkPlayer playerToMove)
		{
			m_ignoreSystem.UpdateVisibilityPlayers();
		}

		public void OnReconnect()
		{
			for (int i = 0; i < m_slots.Length; i++)
			{
				m_slots[i] = null;
			}
			int num = 0;
			foreach (NetworkPlayer player in m_networkGame.Players)
			{
				m_slots[num] = player;
				player.SlotID = (byte)num;
				num++;
			}
			m_ignoreSystem.UpdateVisibilityPlayers();
		}

		public void PlayerSpawned(NetworkPlayer player)
		{
		}

		public void SetCollidingForGroup(string groupName, bool state)
		{
		}

		public void UpdateCollideState(NetworkPlayer player)
		{
		}
	}
}
