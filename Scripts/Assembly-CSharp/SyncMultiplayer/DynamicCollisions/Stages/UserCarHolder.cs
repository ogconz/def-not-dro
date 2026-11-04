using System;
using System.Collections.Generic;

namespace SyncMultiplayer.DynamicCollisions.Stages
{
	public class UserCarHolder
	{
		public Action<RaceCar, NetworkPlayer> onCarCreated;

		public Action<RaceCar, NetworkPlayer> onCarRemoved;

		private UserRegedit m_userRegedit;

		private RaceCar[] m_carsCache;

		private bool[] m_carValidState;

		private Queue<NetworkPlayer> m_releaseQueue;

		public UserCarHolder(DynamicCollisionsConfig config, UserRegedit userRegedit)
		{
			m_userRegedit = userRegedit;
			m_carsCache = new RaceCar[config.maxCars];
			m_carValidState = new bool[config.maxCars];
			m_releaseQueue = new Queue<NetworkPlayer>(config.maxCars);
			UserRegedit userRegedit2 = m_userRegedit;
			userRegedit2.onUserRemoved = (Action<NetworkPlayer>)Delegate.Combine(userRegedit2.onUserRemoved, new Action<NetworkPlayer>(OnUserRemovedHandler));
		}

		~UserCarHolder()
		{
			UserRegedit userRegedit = m_userRegedit;
			userRegedit.onUserRemoved = (Action<NetworkPlayer>)Delegate.Remove(userRegedit.onUserRemoved, new Action<NetworkPlayer>(OnUserRemovedHandler));
			m_userRegedit = null;
			m_carsCache = null;
			m_carValidState = null;
			m_releaseQueue = null;
		}

		public void Process()
		{
			NetworkPlayer result;
			while (m_releaseQueue.TryDequeue(out result))
			{
				if (m_carValidState[result.SlotID])
				{
					onCarRemoved.SafeCall(m_carsCache[result.SlotID], result);
				}
				m_carsCache[result.SlotID] = null;
				m_carValidState[result.SlotID] = false;
			}
			for (int i = 0; i < m_userRegedit.GetUsersCount(); i++)
			{
				NetworkPlayer userByIndex = m_userRegedit.GetUserByIndex(i);
				if (HasDifferentCar(userByIndex))
				{
					if (m_carValidState[userByIndex.SlotID])
					{
						onCarRemoved.SafeCall(m_carsCache[userByIndex.SlotID], userByIndex);
					}
					RaceCar userCar = userByIndex.userCar;
					m_carValidState[userByIndex.SlotID] = !BaseCar.IsNull(userCar);
					if (m_carValidState[userByIndex.SlotID])
					{
						onCarCreated.SafeCall(userCar, userByIndex);
					}
					m_carsCache[userByIndex.SlotID] = userCar;
				}
			}
		}

		public RaceCar GetCar(int slotID)
		{
			return m_carsCache[slotID];
		}

		private void OnUserRemovedHandler(NetworkPlayer player)
		{
			if (player != null)
			{
				m_releaseQueue.Enqueue(player);
			}
		}

		private bool HasDifferentCar(NetworkPlayer player)
		{
			return player.userCar != m_carsCache[player.SlotID];
		}
	}
}
