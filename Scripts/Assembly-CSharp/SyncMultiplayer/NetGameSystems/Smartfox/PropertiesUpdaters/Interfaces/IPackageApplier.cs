namespace SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Interfaces
{
	public interface IPackageApplier<T, V>
	{
		void Apply(T val1, V val2);
	}
}
