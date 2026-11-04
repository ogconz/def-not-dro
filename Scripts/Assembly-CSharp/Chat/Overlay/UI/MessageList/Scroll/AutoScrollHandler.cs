using Binding;
using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.MessageList.Scroll
{
	public class AutoScrollHandler
	{
		private StateContext m_state;

		private ScrollProcessor m_scrollProcessor;

		private ChatMessageListProperty m_messages;

		public void Setup(StateContext state, ScrollProcessor scrollProcessor, ChatMessageListProperty messages)
		{
			m_state = state;
			m_scrollProcessor = scrollProcessor;
			m_messages = messages;
			m_messages.OnElementInserted += OnElementInsertedHandler;
			m_state.navigationLayerActive.onChanged += OnNavigationLayerActiveChangedHandler;
		}

		public void Release()
		{
			m_messages.OnElementInserted -= OnElementInsertedHandler;
			m_state.navigationLayerActive.onChanged -= OnNavigationLayerActiveChangedHandler;
			m_state = null;
			m_scrollProcessor = null;
			m_messages = null;
		}

		private void OnElementInsertedHandler(int index, BaseListElementData item)
		{
			if (IsAutoScrollEnabled() || IsOwnedMessage(item))
			{
				m_scrollProcessor.FocusToBottom();
			}
		}

		private bool IsAutoScrollEnabled()
		{
			return m_state.messageAutoScrollMode.Get();
		}

		private void OnNavigationLayerActiveChangedHandler(bool isActive)
		{
			if (!isActive && !m_state.controlLayerActive.Get())
			{
				m_scrollProcessor.FocusToBottom();
			}
		}

		private bool IsOwnedMessage(BaseListElementData item)
		{
			if (item is ChatMessageElementData chatMessageElementData)
			{
				Message message = chatMessageElementData.GetMessage();
				if (message.isLocalOwner)
				{
					return !message.isMessageBlocked;
				}
				return false;
			}
			return false;
		}
	}
}
