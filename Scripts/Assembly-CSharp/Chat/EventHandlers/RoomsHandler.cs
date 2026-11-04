using Chat.Protocol;

namespace Chat.EventHandlers
{
	public class RoomsHandler : IEventHandler
	{
		private ChannelProvider m_channelProvider;

		public RoomsHandler(ChannelProvider channelProvider)
		{
			m_channelProvider = channelProvider;
		}

		public void AddListeners(EventDispatcher dispatcher)
		{
			dispatcher.AddListener<RoomsInfoPayload>(RoomsInfoHandler);
			dispatcher.AddListener<RoomRemovedPayload>(RoomRemovedHandler);
		}

		public void RemoveListeners(EventDispatcher dispatcher)
		{
			dispatcher.RemoveListener<RoomsInfoPayload>(RoomsInfoHandler);
			dispatcher.RemoveListener<RoomRemovedPayload>(RoomRemovedHandler);
		}

		private void RoomsInfoHandler(RoomsInfoPayload payload)
		{
			RoomInfo[] joinedRooms = payload.joinedRooms;
			foreach (RoomInfo roomInfo in joinedRooms)
			{
				m_channelProvider.AddOrUpdate(roomInfo.type, roomInfo.id, roomInfo.name);
			}
		}

		private void RoomRemovedHandler(RoomRemovedPayload payload)
		{
			m_channelProvider.Remove(payload.roomId);
		}
	}
}
