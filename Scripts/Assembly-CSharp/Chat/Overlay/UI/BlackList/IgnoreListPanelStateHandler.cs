using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.BlackList
{
	public class IgnoreListPanelStateHandler
	{
		private UIChatIgnoreListPanelContext m_ignorePanel;

		private StateContext m_state;

		public void Setup(UIChatIgnoreListPanelContext ignorePanel, StateContext state)
		{
			m_ignorePanel = ignorePanel;
			m_state = state;
			m_ignorePanel.onActivityChanged += OnIgnorePanelActivityChangedHandler;
		}

		public void Release()
		{
			m_ignorePanel.onActivityChanged -= OnIgnorePanelActivityChangedHandler;
			m_ignorePanel = null;
			m_state = null;
		}

		private void OnIgnorePanelActivityChangedHandler()
		{
			m_state.ignorePanelActive.Set(m_ignorePanel.IsActive());
		}
	}
}
