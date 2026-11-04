using System.Collections.Generic;
using Chat.Protocol;

namespace Chat
{
	public class ChannelLocalizer
	{
		private ChannelProvider m_channelProvider;

		private Dictionary<TargetType, LocalizationKeys.Key> m_localizationKeys = new Dictionary<TargetType, LocalizationKeys.Key>
		{
			[TargetType.Mp_Room] = LocalizationKeys.chat_room_channel_caption,
			[TargetType.Direct] = LocalizationKeys.chat_direct_message_channel_caption
		};

		public ChannelLocalizer(ChannelProvider channelProvider)
		{
			m_channelProvider = channelProvider;
			m_channelProvider.onChannelAdded += TryLocalizeChannelCaption;
		}

		public void Release()
		{
			m_channelProvider.onChannelAdded -= TryLocalizeChannelCaption;
			m_channelProvider = null;
		}

		private void TryLocalizeChannelCaption(MessageChannel channel)
		{
			TargetType type = channel.GetTargetInfo().type;
			if (m_localizationKeys.TryGetValue(type, out var value))
			{
				channel.SetCaption(value.Localized());
			}
		}
	}
}
