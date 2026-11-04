using System;
using ArrayExtension;
using Binding;
using Chat.Protocol;

namespace Chat.Overlay.UI.ChannelList
{
	public class ChatChannelListProperty : ListProperty
	{
		private ChannelProvider m_channelProvider;

		private TargetType[] m_targetChannelsType = new TargetType[2]
		{
			TargetType.Global,
			TargetType.Mp_Room
		};

		public event Action<MessageChannel> onChannelSelected;

		public void Setup(ChannelProvider channelProvider)
		{
			m_channelProvider = channelProvider;
			BindHandlers();
			FillChannels();
		}

		public void Release()
		{
			ReleaseHandlers();
			Clear();
		}

		private void BindHandlers()
		{
			m_channelProvider.onChannelAdded += OnChannelAddedHandler;
			m_channelProvider.onChannelRemoved += OnChannelRemovedHandler;
			base.OnSelectionChanged += OnSelectionChangedHandler;
		}

		private void ReleaseHandlers()
		{
			m_channelProvider.onChannelAdded -= OnChannelAddedHandler;
			m_channelProvider.onChannelRemoved -= OnChannelRemovedHandler;
			base.OnSelectionChanged -= OnSelectionChangedHandler;
		}

		private void OnChannelAddedHandler(MessageChannel channel)
		{
			if (m_targetChannelsType.Contains(channel.GetTargetInfo().type))
			{
				Add(new ChatChannelElementData(channel));
			}
		}

		private void OnChannelRemovedHandler(MessageChannel channel)
		{
			if (m_targetChannelsType.Contains(channel.GetTargetInfo().type))
			{
				RemoveAt(IndexOfChannel(channel));
			}
		}

		private void OnSelectionChangedHandler(BaseListElementData item)
		{
			if (item is ChatChannelElementData { selected: not false } chatChannelElementData)
			{
				this.onChannelSelected.SafeCall(chatChannelElementData.GetChannel());
			}
		}

		private int IndexOfChannel(MessageChannel channel)
		{
			return IndexOf((BaseListElementData item) => item is ChatChannelElementData chatChannelElementData && object.Equals(chatChannelElementData.GetChannel(), channel));
		}

		private void FillChannels()
		{
			TargetType[] targetChannelsType = m_targetChannelsType;
			foreach (TargetType type in targetChannelsType)
			{
				foreach (MessageChannel item in m_channelProvider.GetAllByType(type))
				{
					Add(new ChatChannelElementData(item));
				}
			}
		}
	}
}
