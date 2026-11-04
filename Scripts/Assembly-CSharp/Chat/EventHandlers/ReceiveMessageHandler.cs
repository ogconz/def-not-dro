using Chat.Protocol;

namespace Chat.EventHandlers
{
	public class ReceiveMessageHandler : IEventHandler
	{
		private ChannelProvider m_channelProvider;

		private ClientInfo m_clientInfo;

		public ReceiveMessageHandler(ChannelProvider channelProvider, ClientInfo clientInfo)
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
			if (!(payload.id == m_clientInfo.internalId))
			{
				if (payload.targetType == TargetType.Direct)
				{
					m_channelProvider.AddOrUpdate(TargetType.Direct, payload.id, payload.username);
				}
				m_channelProvider.Get(payload.targetId)?.AddMessage(new Message(payload));
			}
		}
	}
}
