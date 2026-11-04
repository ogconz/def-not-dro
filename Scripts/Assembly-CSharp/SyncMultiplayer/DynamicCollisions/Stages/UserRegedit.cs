using System;
using System.Collections.Generic;

namespace SyncMultiplayer.DynamicCollisions.Stages
{
	public class UserRegedit
	{
		public Action<NetworkPlayer> onUserAdded;

		public Action<NetworkPlayer> onUserRemoved;

		private NetGameSystemsManager m_netSystems;

		private List<NetworkPlayer> m_users;

		private NetworkPlayer[] m_userSlotMapping;

		public UserRegedit(DynamicCollisionsConfig config, NetGameSystemsManager netSystems)
		{
			m_users = new List<NetworkPlayer>(config.maxCars);
			m_userSlotMapping = new NetworkPlayer[config.maxCars];
			m_netSystems = netSystems;
		}

		public void BindPlayer(NetworkPlayer player)
		{
			if (!m_users.Contains(player))
			{
				m_users.Add(player);
				m_userSlotMapping[player.SlotID] = player;
				onUserAdded.SafeCall(player);
			}
		}

		public void ReleasePlayer(NetworkPlayer player)
		{
			if (m_users.Contains(player))
			{
				onUserRemoved.SafeCall(player);
				m_users.Remove(player);
				m_userSlotMapping[player.SlotID] = null;
			}
		}

		public int GetUsersCount()
		{
			return m_users.Count;
		}

		public NetworkPlayer GetUserByIndex(int index)
		{
			return m_users[index];
		}

		public NetworkPlayer GetUserBySlotId(int slotId)
		{
			return m_userSlotMapping[slotId];
		}

		public NetworkPlayer GetLocalUser()
		{
			NetworkPlayer result = m_netSystems?.host?.LocalPlayer;
			if (CameraSwitch.instance == null)
			{
				return result;
			}
			RaceCar car = CameraSwitch.instance.GetCar();
			if (car != null && car.networkPlayer != null)
			{
				result = car.networkPlayer;
			}
			return result;
		}
	}
}
