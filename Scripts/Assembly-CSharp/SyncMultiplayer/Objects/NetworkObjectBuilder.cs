using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace SyncMultiplayer.Objects
{
	public class NetworkObjectBuilder
	{
		private Dictionary<Type, BaseObjectSyncModel> m_syncModels = new Dictionary<Type, BaseObjectSyncModel>();

		private NetworkObject m_networkObject;

		private NetGameSystemsManager m_systems;

		public NetworkObjectBuilder(GameObject gameObject, NetGameSystemsManager systems)
		{
			m_networkObject = new NetworkObject(gameObject);
			m_systems = systems;
		}

		public NetworkObjectBuilder AddSync<T>() where T : BaseObjectSyncModel, new()
		{
			Type typeFromHandle = typeof(T);
			if (!m_syncModels.ContainsKey(typeFromHandle))
			{
				T val = new T();
				if (val.Init(m_networkObject))
				{
					val.Attach(m_systems);
					m_syncModels[typeFromHandle] = val;
				}
			}
			return this;
		}

		public NetworkObject Build()
		{
			m_networkObject.syncModels = m_syncModels.Values.ToList();
			m_networkObject.id = GenerateId();
			return m_networkObject;
		}

		private int GenerateId()
		{
			byte[] buffer = m_networkObject.SerializeNetworkObject(SyncPacketType.Id);
			using SHA256 sHA = SHA256.Create();
			return BitConverter.ToInt32(sHA.ComputeHash(buffer), 0);
		}
	}
}
