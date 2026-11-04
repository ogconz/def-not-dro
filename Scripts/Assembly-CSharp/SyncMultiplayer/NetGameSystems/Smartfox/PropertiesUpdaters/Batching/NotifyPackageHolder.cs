namespace SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Batching
{
	public class NotifyPackageHolder : SmartfoxPackageHolder
	{
		public override bool CanAdd(SmartfoxDataPackage value)
		{
			if (!m_package.HasValue)
			{
				return true;
			}
			if (m_package.GetValueOrDefault().Data.GetData("key") != value.Data.GetData("key"))
			{
				return false;
			}
			return true;
		}
	}
}
