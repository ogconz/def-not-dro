public class GameEventSubscriber<T>
{
	public uint ID;

	public GameEventFlag flags;

	public GameEventHandler<T> callback;
}
