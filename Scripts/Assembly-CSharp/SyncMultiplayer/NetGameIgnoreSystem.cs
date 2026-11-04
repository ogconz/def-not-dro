using System;
using SyncMultiplayer.NetGameSystems.Collisions;

namespace SyncMultiplayer
{
	public class NetGameIgnoreSystem : BaseNetGameSystem
	{
		private const string IGNORING_ME_NAME_GROUP = "ignoring-me-group";

		private NetworkGame m_game;

		private NetGameCollisionManager m_collisionManager;

		public override void Attach(NetGameSystemsManager systemOwner)
		{
			base.Attach(systemOwner);
			m_game = systemOwner.host;
			m_collisionManager = systemOwner.Get<NetGameCollisionManager>();
			m_game.packetHandler.Subscribe(PacketId.Ignored, OnPlayerIgnoreMe);
		}

		public override void OnGameConnect()
		{
			UIReportContext.ClearBanPlayersList();
		}

		public override void OnDisconnect()
		{
			UIReportContext.ClearBanPlayersList();
			foreach (NetworkPlayer player in m_game.Players)
			{
				player.OnCarAttached = (Action<NetworkPlayer>)Delegate.Remove(player.OnCarAttached, new Action<NetworkPlayer>(OnPlayerCarLoaded));
			}
		}

		public override void DoPlayerEnter(NetworkPlayer player)
		{
			if (player != null)
			{
				player.OnCarAttached = (Action<NetworkPlayer>)Delegate.Remove(player.OnCarAttached, new Action<NetworkPlayer>(OnPlayerCarLoaded));
				player.OnCarAttached = (Action<NetworkPlayer>)Delegate.Combine(player.OnCarAttached, new Action<NetworkPlayer>(OnPlayerCarLoaded));
			}
		}

		public override void DoPlayerLeave(NetworkPlayer player)
		{
			player.OnCarAttached = (Action<NetworkPlayer>)Delegate.Remove(player.OnCarAttached, new Action<NetworkPlayer>(OnPlayerCarLoaded));
		}

		public void IgnorePlayer(NetworkPlayer player)
		{
			if (player != null)
			{
				if (CameraSwitch.instance.targetRaceCar == player.userCar)
				{
					CameraSwitch.instance.SetCar(base.owner.host.LocalPlayer.userCar);
				}
				AddPlayerToIgnoredList(player);
			}
		}

		public void UpdateVisibilityPlayers()
		{
			NetworkPlayer localPlayer = base.owner.host.LocalPlayer;
			bool flag = localPlayer.attachedRoom != null;
			foreach (NetworkPlayer player in base.owner.host.Players)
			{
				if (!player.IsLocal)
				{
					SetCarVisible(active: (!flag) ? (!IsPlayerIgnored(player)) : localPlayer.attachedRoom.runtimePlayers.Contains(player), car: player.userCar);
				}
			}
		}

		public bool IgnorePlayerIfNeed(NetworkPlayer player)
		{
			bool flag = IsPlayerIgnoringMe(player) || IsPlayerIgnored(player);
			bool num = PlayerHasIgnoredGroup(player);
			bool flag2 = PlayerHasCollideGroup(player);
			if (num)
			{
				return false;
			}
			if (flag && !flag2)
			{
				DisableCarCollision(player);
				return true;
			}
			return false;
		}

		public void DisableCarCollision(NetworkPlayer player)
		{
			if (player != null)
			{
				m_collisionManager.MovePlayerToColliderGroup("ignoring-me-group", player);
			}
		}

		public bool IsPlayerIgnored(NetworkPlayer player)
		{
			if (player != null)
			{
				if (!base.owner.host.LocalPlayer.PlayerProperties.IgnoredPlayers.Contains(player.PlayerId))
				{
					return UIReportContext.alreadyBannedInThisSessionPlayers.Contains(player.PlayerId.ToString());
				}
				return true;
			}
			return false;
		}

		public bool IsPlayerIgnoringMe(NetworkPlayer player)
		{
			return player?.PlayerProperties.IgnoredPlayers.Contains(base.owner.host.LocalPlayer.PlayerId) ?? false;
		}

		public bool PlayerHasIgnoredGroup(NetworkPlayer player)
		{
			return player.collideGroup == "ignoring-me-group";
		}

		public bool PlayerHasCollideGroup(NetworkPlayer player)
		{
			return !string.IsNullOrEmpty(player.collideGroup);
		}

		public bool EqualLocalCollideGroup(NetworkPlayer player)
		{
			return base.owner.host.LocalPlayer.collideGroup == player.collideGroup;
		}

		private void AddPlayerToIgnoredList(NetworkPlayer player)
		{
			base.owner.host.LocalPlayer.PlayerProperties.IgnoredPlayers.Add(player.PlayerId);
			base.owner.host.Client?.Send(new SmartfoxDataPackage(PacketId.Ignored), reliable: true, new int[1] { player.NetworkID });
			SetCarVisible(player.userCar, active: false);
		}

		private void SetCarVisible(RaceCar car, bool active)
		{
			if (!BaseCar.IsNull(car))
			{
				car.gameObject.SetActive(active);
			}
		}

		private void OnPlayerIgnoreMe(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			sender.PlayerProperties.IgnoredPlayers.Add(base.owner.host.LocalPlayer.PlayerId);
			DisableCarCollision(sender);
		}

		private void OnPlayerCarLoaded(NetworkPlayer player)
		{
			if (IsPlayerIgnored(player))
			{
				AddPlayerToIgnoredList(player);
			}
		}
	}
}
