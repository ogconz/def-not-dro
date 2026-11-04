using System;
using System.Collections.Generic;

namespace SyncMultiplayer
{
	public class DelayedPacketManager
	{
		public class DelayedData
		{
			public NetworkPlayer sender;

			public SmartfoxDataPackage data;
		}

		private List<DelayedData> m_delayedPackets = new List<DelayedData>();

		public int count => m_delayedPackets.Count;

		public void RegisterPacket(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			DelayedData delayedData = new DelayedData();
			delayedData.sender = sender;
			delayedData.data = data;
			m_delayedPackets.Add(delayedData);
		}

		public void PlayerLost(NetworkPlayer player)
		{
			m_delayedPackets.RemoveAll((DelayedData itr) => itr.sender == player);
		}

		public void GameLost()
		{
			m_delayedPackets.Clear();
		}

		public void Handle(Action<NetworkPlayer, SmartfoxDataPackage> handler)
		{
			int i = 0;
			try
			{
				for (; i < m_delayedPackets.Count; i++)
				{
					DelayedData delayedData = m_delayedPackets[i];
					handler(delayedData.sender, delayedData.data);
				}
				m_delayedPackets.Clear();
			}
			catch
			{
				m_delayedPackets.RemoveRange(0, i + 1);
			}
		}
	}
}
