using Sfs2X.Entities.Data;
using SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Batching.Interfaces;

namespace SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Batching
{
	public class SmartfoxPackageHolder : IValueHolder<SmartfoxDataPackage>
	{
		protected SmartfoxDataPackage? m_package;

		public SmartfoxDataPackage value => m_package.GetValueOrDefault();

		public bool Add(SmartfoxDataPackage value)
		{
			if (!CanAdd(value))
			{
				return false;
			}
			UnionWith(value);
			return true;
		}

		public virtual bool CanAdd(SmartfoxDataPackage value)
		{
			return true;
		}

		private void UnionWith(SmartfoxDataPackage value)
		{
			if (!m_package.HasValue)
			{
				m_package = value;
				return;
			}
			ISFSObject data = value.Data;
			string[] keys = data.GetKeys();
			foreach (string key in keys)
			{
				SFSDataWrapper data2 = data.GetData(key);
				m_package.Value.Data.Put(key, data2);
			}
		}
	}
}
