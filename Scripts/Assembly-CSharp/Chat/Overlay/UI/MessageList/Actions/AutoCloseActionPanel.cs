using Binding;
using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.MessageList.Actions
{
	public class AutoCloseActionPanel
	{
		private StateContext m_state;

		private MessageActionBinder m_actionBinder;

		private ChatMessageListProperty m_messageList;

		public void Setup(StateContext state, MessageActionBinder actionBinder, ChatMessageListProperty messageList)
		{
			m_state = state;
			m_actionBinder = actionBinder;
			m_messageList = messageList;
			m_state.actionPanelActive.onChanged += OnActionPanelActiveChangedHandler;
		}

		public void Release()
		{
			m_state.actionPanelActive.onChanged -= OnActionPanelActiveChangedHandler;
			m_messageList.OnSelectionChanged -= OnMessageSelectionChangedHandler;
			m_state = null;
			m_actionBinder = null;
			m_messageList = null;
		}

		private void OnActionPanelActiveChangedHandler(bool isActive)
		{
			if (isActive)
			{
				m_messageList.OnSelectionChanged += OnMessageSelectionChangedHandler;
			}
		}

		private void OnMessageSelectionChangedHandler(BaseListElementData item)
		{
			if (!item.selected)
			{
				m_messageList.OnSelectionChanged -= OnMessageSelectionChangedHandler;
				m_actionBinder.CloseActionPanel();
			}
		}
	}
}
