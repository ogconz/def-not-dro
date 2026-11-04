using Chat.Protocol;

namespace Chat.EventHandlers
{
	public class HandshakeMessageHandler : IEventHandler
	{
		private ChannelProvider m_channelProvider;

		private ClientInfo m_clientInfo;

		public HandshakeMessageHandler(ChannelProvider channelProvider, ClientInfo clientInfo)
		{
			m_channelProvider = channelProvider;
			m_clientInfo = clientInfo;
		}

		public void AddListeners(EventDispatcher dispatcher)
		{
			dispatcher.AddListener<RecvMessagePayload>(MessageHandler);
		}

		public void RemoveListeners(EventDispatcher dispatcher)
		{
			dispatcher.RemoveListener<RecvMessagePayload>(MessageHandler);
		}

		private void MessageHandler(RecvMessagePayload payload)
		{
			if (payload.id != m_clientInfo.internalId)
			{
				return;
			}
			foreach (MessageChannel item in m_channelProvider)
			{
				foreach (Message message in item.GetMessages())
				{
					if (!message.synced && message.userId == m_clientInfo.internalId && message.messageId == payload.messageId)
					{
						message.ConfirmSync();
						return;
					}
				}
			}
		}
	}
}
