namespace SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Batching.Interfaces
{
	public interface IValueHolder<T>
	{
		T value { get; }

		bool Add(T value);

		bool CanAdd(T value);
	}
}
