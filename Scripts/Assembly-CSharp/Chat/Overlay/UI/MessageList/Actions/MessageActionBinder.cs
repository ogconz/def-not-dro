using UnityEngine;

namespace Chat.Overlay.UI.MessageList.Actions
{
	public class MessageActionBinder
	{
		private ChatService m_service;

		private UIChatActionPanelContext m_actionPanel;

		private MessageActionContainer m_container;

		public void Setup(ChatService service, UIChatActionPanelContext actionPanel)
		{
			m_service = service;
			m_actionPanel = actionPanel;
			m_container = new MessageActionContainer();
		}

		public void Release()
		{
			m_container.Release();
			m_service = null;
			m_actionPanel = null;
			m_container = null;
		}

		public void BindMessage(Message message)
		{
			m_container.Setup(m_service, message);
		}

		public void ReleaseMessage()
		{
			m_container.Release();
		}

		public bool CanInvokeActionPanel()
		{
			return m_container.HasActions();
		}

		public void InvokeActionPanel(Vector2 position)
		{
			m_actionPanel.Close();
			if (CanInvokeActionPanel())
			{
				m_actionPanel.Open(m_container.GetActions());
			}
			m_actionPanel.SetPosition(position);
		}

		public void CloseActionPanel()
		{
			m_actionPanel.Close();
		}
	}
}
