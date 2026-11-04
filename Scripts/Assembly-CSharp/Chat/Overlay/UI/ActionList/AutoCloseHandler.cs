using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.ActionList
{
	public class AutoCloseHandler
	{
		private UIChatActionPanelContext m_actionPanel;

		private ChatActionListProperty m_actions;

		private StateContext m_state;

		public void Setup(UIChatActionPanelContext actionPanel, ChatActionListProperty actions, StateContext state)
		{
			m_actionPanel = actionPanel;
			m_actions = actions;
			m_state = state;
			m_actionPanel.onActivityChanged += OnActivityChangedHandler;
		}

		public void Release()
		{
			m_actionPanel.onActivityChanged -= OnActivityChangedHandler;
			m_actionPanel = null;
			m_actions = null;
			m_state = null;
		}

		private void OnActivityChangedHandler()
		{
			if (m_actionPanel.IsActive())
			{
				BindCloseCallback();
			}
			else
			{
				ReleaseCloseCallback();
			}
		}

		private void BindCloseCallback()
		{
			m_actions.onCalledAction += m_actionPanel.Close;
			m_state.actionPanelFocused.onChanged += OnActionPanelFocusedHandler;
			m_state.messageSelecting.onChanged += OnActionPanelFocusedHandler;
		}

		private void ReleaseCloseCallback()
		{
			m_actions.onCalledAction -= m_actionPanel.Close;
			m_state.actionPanelFocused.onChanged -= OnActionPanelFocusedHandler;
			m_state.messageSelecting.onChanged -= OnActionPanelFocusedHandler;
		}

		private void OnActionPanelFocusedHandler(bool isFocused)
		{
			if (!isFocused)
			{
				m_actionPanel.Close();
			}
		}
	}
}
