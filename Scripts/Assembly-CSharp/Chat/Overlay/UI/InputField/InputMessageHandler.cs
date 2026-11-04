using Chat.Overlay.UI.ChannelList;

namespace Chat.Overlay.UI.InputField
{
	public class InputMessageHandler
	{
		private ChatService m_service;

		private ChatChannelListProperty m_roomChannels;

		private ChatInputFieldElement m_inputField;

		private MessageChannel m_channel;

		public void Setup(ChatService service, ChatChannelListProperty roomChannels, ChatInputFieldElement inputField)
		{
			m_service = service;
			m_roomChannels = roomChannels;
			m_inputField = inputField;
			m_roomChannels.onChannelSelected += SetChannel;
			m_inputField.onTextSubmit += SendMessage;
		}

		public void Release()
		{
			m_roomChannels.onChannelSelected -= SetChannel;
			m_inputField.onTextSubmit -= SendMessage;
			m_service = null;
			m_roomChannels = null;
			m_inputField = null;
			m_channel = null;
		}

		private void SetChannel(MessageChannel channel)
		{
			m_channel = channel;
		}

		private void SendMessage(string message)
		{
			if (m_service != null && m_channel != null)
			{
				m_service.SendMessage(m_channel, message);
			}
		}
	}
}
