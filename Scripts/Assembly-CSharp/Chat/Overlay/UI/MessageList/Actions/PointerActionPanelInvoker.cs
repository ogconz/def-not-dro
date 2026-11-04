using Binding;

namespace Chat.Overlay.UI.MessageList.Actions
{
	public class PointerActionPanelInvoker
	{
		private MessageActionBinder m_actionBinder;

		private ChatMessageListProperty m_messageList;

		public void Setup(MessageActionBinder actionBinder, ChatMessageListProperty messageList)
		{
			m_actionBinder = actionBinder;
			m_messageList = messageList;
			m_messageList.OnClick += OnMessageClickHandler;
		}

		public void Release()
		{
			m_messageList.OnClick -= OnMessageClickHandler;
			m_actionBinder = null;
			m_messageList = null;
		}

		private void OnMessageClickHandler(BaseListElementData item)
		{
			if (item is ChatMessageElementData chatMessageElementData)
			{
				m_messageList.Selection(item, selected: true);
				m_actionBinder.BindMessage(chatMessageElementData.GetMessage());
				m_actionBinder.InvokeActionPanel(chatMessageElementData.GetContentPosition());
				m_actionBinder.ReleaseMessage();
			}
		}
	}
}
