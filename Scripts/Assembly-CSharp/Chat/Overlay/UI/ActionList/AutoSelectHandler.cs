using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.ActionList
{
	public class AutoSelectHandler
	{
		private StateContext m_state;

		private ChatActionListProperty m_actions;

		public void Setup(StateContext state, ChatActionListProperty actions)
		{
			m_state = state;
			m_actions = actions;
			m_state.actionPanelActive.onChanged += OnActionPanelActiveChangedHandler;
			m_state.navigationLayerActive.onChanged += OnNavigationLayerActiveChangedHandler;
		}

		public void Release()
		{
			m_state.actionPanelActive.onChanged -= OnActionPanelActiveChangedHandler;
			m_state.navigationLayerActive.onChanged -= OnNavigationLayerActiveChangedHandler;
			m_state = null;
			m_actions = null;
		}

		private void OnActionPanelActiveChangedHandler(bool isActive)
		{
			UpdateSelectionState();
		}

		private void OnNavigationLayerActiveChangedHandler(bool isActive)
		{
			UpdateSelectionState();
		}

		private void UpdateSelectionState()
		{
			bool flag = m_state.navigationLayerActive.Get();
			bool num = m_state.actionPanelActive.Get();
			bool flag2 = m_state.actionPanelHovered.Get();
			bool flag3 = m_actions.GetSelectedIndex() != -1;
			if (num)
			{
				if (flag && !flag3)
				{
					m_actions.Selection(0, selected: true);
				}
				else if (!flag && flag3 && !flag2)
				{
					m_actions.SelectionAll(selected: false);
				}
			}
		}
	}
}
