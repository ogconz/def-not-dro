using Binding;
using Chat.Overlay.UI.ChannelList;
using GameOverlay;
using UnityEngine;

namespace Chat.Overlay.UI.MessageList
{
	public class ChatMessageListProperty : ListProperty
	{
		private ChatService m_service;

		private ChatChannelListProperty m_roomChannels;

		private MessageChannel m_channel;

		private int m_tempSavedIndex;

		public void Setup(ChatService service, ChatChannelListProperty roomChannels)
		{
			m_service = service;
			m_roomChannels = roomChannels;
			m_service.ignoreUsersProvider.onListChanged += OnIgnoreListChanged;
			m_roomChannels.onChannelSelected += BindChannel;
		}

		public void Release()
		{
			m_service.ignoreUsersProvider.onListChanged -= OnIgnoreListChanged;
			m_roomChannels.onChannelSelected -= BindChannel;
			ReleaseChannel();
			m_service = null;
			m_roomChannels = null;
		}

		private void BindChannel(MessageChannel channel)
		{
			if ((m_channel == null || !m_channel.Equals(channel)) && channel != null)
			{
				ReleaseChannel();
				m_channel = channel;
				BindChannelHandlers();
				FillMessages();
			}
		}

		private void ReleaseChannel()
		{
			if (m_channel != null)
			{
				ReleaseChannelHandlers();
				ClearSelection();
				Clear();
				m_channel = null;
			}
		}

		private void BindChannelHandlers()
		{
			m_channel.onMessageAdded += OnMessageAddedHandler;
			m_channel.onMessageRemoved += OnMessageRemovedHandler;
		}

		private void ReleaseChannelHandlers()
		{
			m_channel.onMessageAdded -= OnMessageAddedHandler;
			m_channel.onMessageRemoved -= OnMessageRemovedHandler;
		}

		private void OnMessageAddedHandler(Message message)
		{
			AddMessageWithFilter(message);
		}

		private void OnMessageRemovedHandler(Message message)
		{
			int index = IndexOf((BaseListElementData x) => x is ChatMessageElementData chatMessageElementData && object.Equals(chatMessageElementData.GetMessage(), message));
			RemoveAt(index);
		}

		private void OnIgnoreListChanged()
		{
			SaveSelection();
			ClearSelection();
			Clear();
			FillMessages();
			ApplySelection();
		}

		public void ClearSelection()
		{
			SelectionAll(selected: false);
		}

		private void FillMessages()
		{
			foreach (Message message in m_channel.GetMessages())
			{
				AddMessageWithFilter(message);
			}
		}

		private void AddMessageWithFilter(Message message)
		{
			if (!IsMessageBlocked(message))
			{
				Add(new ChatMessageElementData(message));
			}
		}

		private bool IsMessageBlocked(Message message)
		{
			if (message.isMessageBlocked)
			{
				return true;
			}
			if (GameOverlay.Overlay.instance.IsBlockedUser(message.playerId))
			{
				return true;
			}
			UserInfo fromMessage = m_service.userInfoProvider.GetFromMessage(message);
			return m_service.ignoreUsersProvider.Exist(fromMessage);
		}

		private void SaveSelection()
		{
			m_tempSavedIndex = GetSelectedIndex();
		}

		private void ApplySelection()
		{
			if (m_tempSavedIndex != -1)
			{
				TimeController.AddCallback(0f, SelectionCallback);
			}
		}

		private void SelectionCallback()
		{
			m_tempSavedIndex = Mathf.Clamp(m_tempSavedIndex, 0, base.count - 1);
			Selection(m_tempSavedIndex, selected: true);
			m_tempSavedIndex = -1;
		}
	}
}
