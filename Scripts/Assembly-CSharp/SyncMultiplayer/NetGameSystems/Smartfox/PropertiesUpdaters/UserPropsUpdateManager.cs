using System;
using System.Collections.Generic;
using Sfs2X.Entities;
using SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Batching;
using SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Dispatchers;
using SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Dispatchers.Settings;
using SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Obtainers;

namespace SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters
{
	public class UserPropsUpdateManager : IDisposable
	{
		private MultiThreadPackageBatcherUpdateDispatcher<SFSUser, HashSetHolder<string>, HashSet<string>> m_updateDispatcher;

		private UserPropsScheduleSettings m_settings;

		private NetGameNotifySystem m_notifySystem;

		public void Init(NetGameNotifySystem notifySystem)
		{
			m_notifySystem = notifySystem;
			InitSettings();
			CreateDispatcher();
		}

		private void CreateDispatcher()
		{
			if (m_updateDispatcher == null)
			{
				m_updateDispatcher = new MultiThreadPackageBatcherUpdateDispatcher<SFSUser, HashSetHolder<string>, HashSet<string>>();
			}
			m_updateDispatcher.Register(new UserPropsPackageApplier(m_notifySystem));
			m_updateDispatcher.Init();
			m_updateDispatcher.SetDelay(m_settings.delay);
		}

		public void Process(SFSUser user, HashSet<string> changed)
		{
			m_updateDispatcher.Process(user, changed);
		}

		public void Dispose()
		{
			m_updateDispatcher?.Dispose();
			m_updateDispatcher = null;
			m_settings = null;
			m_notifySystem = null;
		}

		private void InitSettings()
		{
			m_settings = new UserPropsScheduleSettings();
		}
	}
}
