using System;
using SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Interfaces;

namespace SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Dispatchers
{
	public class PackageUpdateDispatcher<T, V> : IDisposable
	{
		protected IPackageApplier<T, V> m_updater;

		public virtual void Process(T subject, V changes)
		{
			m_updater.Apply(subject, changes);
		}

		public virtual void Init()
		{
		}

		public virtual void Dispose()
		{
			m_updater = null;
		}

		public void Register(IPackageApplier<T, V> updater)
		{
			m_updater = updater;
		}
	}
}
