using Binding;
using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.MessageList
{
	public class MessageListStateHandler
	{
		private ChatMessageListProperty m_messages;

		private StateContext m_state;

		public void Setup(ChatMessageListProperty messages, StateContext state)
		{
			m_messages = messages;
			m_state = state;
			m_messages.OnSelectionChanged += OnMessageSelectionChangedHandler;
			m_state.messageSelecting.onChanged += OnMessageSelectionChangedHandler;
			m_state.showWindow.onChanged += OnShowWindowChangedHandler;
			m_state.actionPanelActive.onChanged += OnActionPanelActiveHandler;
			m_state.navigationLayerActive.onChanged += OnControlLayerAutoActiveHandler;
			m_state.inputFieldFocused.onChanged += OnInputFieldFocusedHandler;
		}

		public void Release()
		{
			m_messages.OnSelectionChanged -= OnMessageSelectionChangedHandler;
			m_state.messageSelecting.onChanged -= OnMessageSelectionChangedHandler;
			m_state.showWindow.onChanged -= OnShowWindowChangedHandler;
			m_state.actionPanelActive.onChanged -= OnActionPanelActiveHandler;
			m_state.navigationLayerActive.onChanged -= OnControlLayerAutoActiveHandler;
			m_state.inputFieldFocused.onChanged -= OnInputFieldFocusedHandler;
			m_messages = null;
			m_state = null;
		}

		private void OnMessageSelectionChangedHandler(BaseListElementData item)
		{
			m_state.messageSelecting.Set(m_messages.GetSelectedIndex() != -1);
		}

		private void OnMessageSelectionChangedHandler(bool isActive)
		{
			if (!isActive)
			{
				m_messages.ClearSelection();
			}
			else if (m_messages.count == 0)
			{
				m_state.messageSelecting.Set(value: false);
			}
			else if (m_messages.GetSelectedIndex() == -1)
			{
				m_messages.Selection(m_messages.count - 1, selected: true);
			}
		}

		private void OnShowWindowChangedHandler(bool isActive)
		{
			if (!isActive)
			{
				m_state.messageSelecting.Set(value: false);
			}
		}

		private void OnActionPanelActiveHandler(bool isActive)
		{
			if (!isActive && !m_state.navigationLayerActive.Get())
			{
				m_state.messageSelecting.Set(value: false);
			}
		}

		private void OnControlLayerAutoActiveHandler(bool isActive)
		{
			if (!isActive && !m_state.actionPanelActive.Get())
			{
				m_state.messageSelecting.Set(value: false);
			}
		}

		private void OnInputFieldFocusedHandler(bool isActive)
		{
			m_state.messageSelecting.Set(!isActive && m_state.deviceWithNavigation.Get());
		}
	}
}
