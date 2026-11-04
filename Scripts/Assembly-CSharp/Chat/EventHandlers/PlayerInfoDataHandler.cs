using Chat.Protocol;

namespace Chat.EventHandlers
{
	public class PlayerInfoDataHandler : IEventHandler
	{
		private ClientInfo m_clientInfo;

		public PlayerInfoDataHandler(ClientInfo clientInfo)
		{
			m_clientInfo = clientInfo;
		}

		public void AddListeners(EventDispatcher dispatcher)
		{
			dispatcher.AddListener<PlayerInfoPayload>(PlayerInfoHandler);
		}

		public void RemoveListeners(EventDispatcher dispatcher)
		{
			dispatcher.RemoveListener<PlayerInfoPayload>(PlayerInfoHandler);
		}

		private void PlayerInfoHandler(PlayerInfoPayload payload)
		{
			m_clientInfo.internalId = payload.internalId;
			m_clientInfo.temporalToken = payload.temporalToken;
		}
	}
}
