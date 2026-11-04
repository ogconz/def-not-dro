using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.ActionList
{
	public class ActionPanelStateHandler
	{
		private UIChatActionPanelContext m_actionPanel;

		private StateContext m_state;

		public void Setup(UIChatActionPanelContext actionPanel, StateContext state)
		{
			m_actionPanel = actionPanel;
			m_state = state;
			m_actionPanel.onActivityChanged += OnActionPanelActivityChangedHandler;
		}

		public void Release()
		{
			m_actionPanel.onActivityChanged -= OnActionPanelActivityChangedHandler;
			m_actionPanel = null;
			m_state = null;
		}

		private void OnActionPanelActivityChangedHandler()
		{
			m_state.actionPanelActive.Set(m_actionPanel.IsActive());
		}
	}
}
