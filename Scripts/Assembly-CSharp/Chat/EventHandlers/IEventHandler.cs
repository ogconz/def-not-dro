namespace Chat.EventHandlers
{
	public interface IEventHandler
	{
		void AddListeners(EventDispatcher dispatcher);

		void RemoveListeners(EventDispatcher dispatcher);
	}
}
