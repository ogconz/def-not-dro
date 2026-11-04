using System.Collections.Generic;
using Chat.Overlay.UI.ChannelList;

namespace Chat.Overlay.UI.InputField
{
	public class InputFieldTextHolder
	{
		private ChannelProvider m_channelProvider;

		private ChatChannelListProperty m_roomChannels;

		private ChatInputFieldElement m_inputField;

		private Dictionary<MessageChannel, string> m_channels = new Dictionary<MessageChannel, string>();

		private MessageChannel m_selectedChannel;

		public void Setup(ChannelProvider channelProvider, ChatChannelListProperty roomChannels, ChatInputFieldElement inputField)
		{
			m_channelProvider = channelProvider;
			m_roomChannels = roomChannels;
			m_inputField = inputField;
			m_channelProvider.onChannelAdded += AddChannelIfNotExist;
			m_channelProvider.onChannelRemoved += RemoveChannel;
			m_roomChannels.onChannelSelected += OnChannelSelectedHandler;
			m_inputField.onValueChanged.AddListener(OnInputFieldTextChanged);
		}

		public void Release()
		{
			m_channelProvider.onChannelAdded -= AddChannelIfNotExist;
			m_channelProvider.onChannelRemoved -= RemoveChannel;
			m_roomChannels.onChannelSelected -= OnChannelSelectedHandler;
			m_inputField.onValueChanged.RemoveListener(OnInputFieldTextChanged);
			m_channelProvider = null;
			m_roomChannels = null;
			m_inputField = null;
			m_selectedChannel = null;
			m_channels.Clear();
		}

		private void OnChannelSelectedHandler(MessageChannel channel)
		{
			m_selectedChannel = channel;
			if (m_selectedChannel != null)
			{
				AddChannelIfNotExist(m_selectedChannel);
				m_inputField.text = m_channels[m_selectedChannel];
				m_inputField.caretPosition = m_inputField.text.Length;
			}
		}

		private void OnInputFieldTextChanged(string text)
		{
			if (m_selectedChannel != null && m_channels.ContainsKey(m_selectedChannel))
			{
				m_channels[m_selectedChannel] = text;
			}
		}

		private void AddChannelIfNotExist(MessageChannel channel)
		{
			if (!m_channels.ContainsKey(channel))
			{
				m_channels[channel] = string.Empty;
			}
		}

		private void RemoveChannel(MessageChannel channel)
		{
			m_channels.Remove(channel);
		}
	}
}
