public interface IGameEvent
{
	uint GetID();

	void CleanUp(GameEventFlag Flags);

	uint Subscribe(GameEventHandler<GameEvents.EventInfo> Callback);

	uint Subscribe(GameEventFlag Flags, GameEventHandler<GameEvents.EventInfo> Callback);

	void Unsubscribe(uint ID);
}
