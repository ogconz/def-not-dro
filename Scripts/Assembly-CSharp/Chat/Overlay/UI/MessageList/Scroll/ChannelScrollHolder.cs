using Binding;
using Chat.Overlay.UI.ChannelList;
using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.MessageList.Scroll
{
	public class ChannelScrollHolder
	{
		private StateContext m_state;

		private ChatChannelListProperty m_roomChannels;

		private ChatMessageListProperty m_messages;

		private ScrollProcessor m_scrollProcessor;

		private bool m_requireFocusToBottom;

		private bool m_requireSaveControl;

		private ActionHolder m_existingFocusAction;

		public void Setup(StateContext state, ChatChannelListProperty roomChannels, ChatMessageListProperty messages, ScrollProcessor scrollProcessor)
		{
			m_state = state;
			m_roomChannels = roomChannels;
			m_messages = messages;
			m_scrollProcessor = scrollProcessor;
			m_roomChannels.OnSelectionChanged += OnSelectionChangedHandler;
			m_messages.OnElementInserted += OnElementInsertedHandler;
			RequestFocusToBottom();
		}

		public void Release()
		{
			ReleaseCallback();
			m_roomChannels.OnSelectionChanged -= OnSelectionChangedHandler;
			m_messages.OnElementInserted -= OnElementInsertedHandler;
			m_state = null;
			m_roomChannels = null;
			m_messages = null;
			m_scrollProcessor = null;
		}

		private void OnSelectionChangedHandler(BaseListElementData item)
		{
			if (!item.selected && m_roomChannels.GetSelected() is ChatChannelElementData chatChannelElementData)
			{
				if (chatChannelElementData.GetChannel().GetMessages().Count > 0)
				{
					RequestFocusToBottom();
				}
				else
				{
					m_state.forceActivateControlLayer.Set(value: false);
				}
			}
		}

		private void OnElementInsertedHandler(int i, BaseListElementData item)
		{
			if (m_requireFocusToBottom)
			{
				CreateCallback();
			}
		}

		private void CreateCallback()
		{
			ReleaseCallback();
			m_existingFocusAction = TimeController.AddCallback(0.1f, FocusCallback);
		}

		private void ReleaseCallback()
		{
			if (m_existingFocusAction != null)
			{
				TimeController.RemoveCallback(m_existingFocusAction);
				m_existingFocusAction = null;
			}
		}

		private void RequestFocusToBottom()
		{
			if (!m_requireFocusToBottom)
			{
				m_requireFocusToBottom = true;
				m_requireSaveControl = m_state.controlLayerActive.Get();
				if (m_requireSaveControl)
				{
					m_state.forceActivateControlLayer.Set(value: true);
				}
			}
		}

		private void FocusCallback()
		{
			m_existingFocusAction = null;
			m_requireFocusToBottom = false;
			m_scrollProcessor.FocusToBottom();
			m_state.forceActivateControlLayer.Set(value: false);
		}
	}
}
