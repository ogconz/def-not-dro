using SyncMultiplayer.NetGameSystems.Collisions;

namespace SyncMultiplayer
{
	public class NetGamePlayerStateSystem : BaseNetGameSystem
	{
		private const string PauseCollisionGroup = "in_game_pause_group";

		private NetGameNotifySystem m_notify;

		private NetGameCollisionManager m_collisions;

		private NetworkGame m_game;

		public override void Attach(NetGameSystemsManager owner)
		{
			base.Attach(owner);
			m_game = owner.host;
			m_notify = owner.Get<NetGameNotifySystem>();
			m_collisions = owner.Get<NetGameCollisionManager>();
			m_notify.Subscribe("pst", UpdatePlayerState);
			if (m_collisions.strategy is DynamicCollisionStrategy dynamicCollisionStrategy)
			{
				dynamicCollisionStrategy.SetCollidingForGroup("in_game_pause_group", state: false);
			}
		}

		public override void OnGameConnect()
		{
			base.OnGameConnect();
			ClearStatesForLocalPlayer();
		}

		public bool ProcessPlayerIfNeed(NetworkPlayer player)
		{
			bool flag = string.IsNullOrEmpty(player.collideGroup);
			if (player.collideGroup.Equals("in_game_pause_group"))
			{
				return false;
			}
			if (!flag)
			{
				return false;
			}
			if (!PlayerHaveDisabledCollision(player))
			{
				return false;
			}
			m_collisions.MovePlayerToColliderGroup("in_game_pause_group", player);
			return true;
		}

		private void UpdatePlayerState(NetworkPlayer player)
		{
			bool num = string.IsNullOrEmpty(player.collideGroup);
			bool flag = player.collideGroup.Equals("in_game_pause_group");
			if (num || flag)
			{
				m_collisions.MovePlayerToColliderGroup(PlayerHaveDisabledCollision(player) ? "in_game_pause_group" : string.Empty, player);
			}
		}

		private bool PlayerHaveDisabledCollision(NetworkPlayer player)
		{
			if (!PlayerInPause(player))
			{
				return PlayerInDyno(player);
			}
			return true;
		}

		public bool PlayerInPause(NetworkPlayer player)
		{
			return PlayerHasState(player, PlayerCarState.Pause);
		}

		public bool PlayerInDyno(NetworkPlayer player)
		{
			return PlayerHasState(player, PlayerCarState.Dyno);
		}

		public bool PlayerHasState(NetworkPlayer player, PlayerCarState state)
		{
			return GetCurrent(player).HasFlag(state);
		}

		public PlayerCarState GetCurrent(NetworkPlayer player)
		{
			return player.PlayerProperties.PlayerState;
		}

		public UIPlayerStateType GetUIState(NetworkPlayer player, bool isFriend, out float suspensionTime)
		{
			PlayerCarState current = GetCurrent(player);
			suspensionTime = 0f;
			return current switch
			{
				PlayerCarState.Idle => GetIdleUIPlayerState(player, isFriend, out suspensionTime), 
				PlayerCarState.Pause => UIPlayerStateType.Pause, 
				PlayerCarState.LoadLivery => UIPlayerStateType.LoadLivery, 
				PlayerCarState.Dyno => UIPlayerStateType.Dyno, 
				_ => UIPlayerStateType.CollisionTimer, 
			};
		}

		private UIPlayerStateType GetIdleUIPlayerState(NetworkPlayer player, bool isFriend, out float suspensionTime)
		{
			bool num = m_collisions.GetPlayerCollideState(player.userCar, out suspensionTime) == NetGameCollisionManager.CollideState.On;
			UIPlayerStateType result = UIPlayerStateType.CollisionOff;
			bool flag = player.attachedRoom != null;
			bool flag2 = player.attachedRoom == m_game.LocalPlayer.attachedRoom;
			if (num)
			{
				result = UIPlayerStateType.CollisionOn;
			}
			if (flag)
			{
				result = UIPlayerStateType.InSubroom;
			}
			if (num && isFriend)
			{
				result = UIPlayerStateType.IsFriend;
			}
			if (suspensionTime > 0f)
			{
				result = UIPlayerStateType.CollisionTimer;
			}
			if (flag && !flag2)
			{
				suspensionTime = 0f;
				result = UIPlayerStateType.InSubroom;
			}
			return result;
		}

		private void SetCurrent(NetworkPlayer player, PlayerCarState state)
		{
			player.PlayerProperties.PlayerState = state;
			if (player.IsLocal)
			{
				m_game.FillLocalPlayerData();
			}
		}

		public void AddStateForPlayer(NetworkPlayer player, PlayerCarState state)
		{
			PlayerCarState current = GetCurrent(player);
			if (!current.HasFlag(state))
			{
				SetCurrent(player, current | state);
			}
		}

		public void RemoveStateForPlayer(NetworkPlayer player, PlayerCarState state)
		{
			PlayerCarState current = GetCurrent(player);
			if (current.HasFlag(state))
			{
				SetCurrent(player, current & ~state);
			}
		}

		public void AddStateForLocalPlayer(PlayerCarState state)
		{
			AddStateForPlayer(m_game.LocalPlayer, state);
		}

		public void RemoveStateForLocalPlayer(PlayerCarState state)
		{
			RemoveStateForPlayer(m_game.LocalPlayer, state);
		}

		private void ClearStatesForLocalPlayer()
		{
			SetCurrent(m_game.LocalPlayer, PlayerCarState.Idle);
		}
	}
}
