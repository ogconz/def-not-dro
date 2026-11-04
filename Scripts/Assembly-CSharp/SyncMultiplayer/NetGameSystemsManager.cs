using System;
using System.Collections;
using System.Collections.Generic;

namespace SyncMultiplayer
{
	public class NetGameSystemsManager : IEnumerable
	{
		private Dictionary<Type, BaseNetGameSystem> m_SystemsIndecies;

		private List<BaseNetGameSystem> m_Systems;

		private bool m_isMaster;

		public NetworkGame host { get; private set; }

		public NetGameSystemsManager(NetworkGame host)
		{
			m_SystemsIndecies = new Dictionary<Type, BaseNetGameSystem>();
			m_Systems = new List<BaseNetGameSystem>();
			this.host = host;
			m_isMaster = false;
		}

		public void Add(BaseNetGameSystem system)
		{
			m_SystemsIndecies.Add(system.GetType(), system);
			m_Systems.Add(system);
		}

		public NetGameSystemsManager AttachSystems()
		{
			foreach (BaseNetGameSystem system in m_Systems)
			{
				if (system.owner != this)
				{
					system.Attach(this);
				}
			}
			return this;
		}

		public void Remove<T>() where T : BaseNetGameSystem
		{
			BaseNetGameSystem value = null;
			if (m_SystemsIndecies.TryGetValue(typeof(T), out value))
			{
				m_Systems.Remove(value);
			}
			m_SystemsIndecies.Remove(typeof(T));
		}

		public T Get<T>() where T : BaseNetGameSystem
		{
			return m_SystemsIndecies[typeof(T)] as T;
		}

		public void OnGameConnect()
		{
			foreach (BaseNetGameSystem system in m_Systems)
			{
				system.OnGameConnect();
			}
		}

		public void OnReconnect()
		{
			foreach (BaseNetGameSystem system in m_Systems)
			{
				system.OnReconnect();
			}
		}

		public void GameLost()
		{
			foreach (BaseNetGameSystem system in m_Systems)
			{
				system.OnDisconnect();
			}
		}

		public void LateUpdate()
		{
			foreach (BaseNetGameSystem system in m_Systems)
			{
				system.LateUpdate();
			}
		}

		public void Update()
		{
			if (host.LocalPlayer.IsMasterClient != m_isMaster)
			{
				m_isMaster = host.LocalPlayer.IsMasterClient;
				foreach (BaseNetGameSystem system in m_Systems)
				{
					system.ChangeMasterState(m_isMaster);
				}
			}
			foreach (BaseNetGameSystem system2 in m_Systems)
			{
				system2.Update();
			}
		}

		public void FixedUpdate()
		{
			if (host.LocalPlayer.IsMasterClient != m_isMaster)
			{
				m_isMaster = host.LocalPlayer.IsMasterClient;
				foreach (BaseNetGameSystem system in m_Systems)
				{
					system.ChangeMasterState(m_isMaster);
				}
			}
			foreach (BaseNetGameSystem system2 in m_Systems)
			{
				system2.FixedUpdate();
			}
		}

		public void DoPlayerEnter(NetworkPlayer player)
		{
			if (player.NetworkID == -1)
			{
				return;
			}
			foreach (BaseNetGameSystem system in m_Systems)
			{
				system.DoPlayerEnter(player);
			}
		}

		public void DoPlayerLeave(NetworkPlayer player)
		{
			if (player.NetworkID == -1)
			{
				return;
			}
			foreach (BaseNetGameSystem system in m_Systems)
			{
				system.DoPlayerLeave(player);
			}
		}

		public void PostPlayerLeave(NetworkPlayer player)
		{
			if (player.NetworkID == -1)
			{
				return;
			}
			foreach (BaseNetGameSystem system in m_Systems)
			{
				system.PostPlayerLeave(player);
			}
		}

		public IEnumerator GetEnumerator()
		{
			return m_Systems.GetEnumerator();
		}

		public static implicit operator bool(NetGameSystemsManager x)
		{
			return x != null;
		}

		public void OnRaceLoaded()
		{
			foreach (BaseNetGameSystem system in m_Systems)
			{
				system.OnRaceLoaded();
			}
		}

		public void OnDrawGizmos()
		{
			if (m_Systems == null)
			{
				return;
			}
			foreach (BaseNetGameSystem system in m_Systems)
			{
				system.OnDrawGizmos();
			}
		}
	}
}
