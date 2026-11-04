using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.BlackList
{
	public class AutoSelectHandler
	{
		private StateContext m_state;

		private ChatIgnoreListProperty m_users;

		public void Setup(StateContext state, ChatIgnoreListProperty users)
		{
			m_state = state;
			m_users = users;
			m_state.ignorePanelActive.onChanged += OnIgnorePanelActiveChangedHandler;
			m_state.navigationLayerActive.onChanged += OnNavigationLayerActiveChangedHandler;
		}

		public void Release()
		{
			m_state.ignorePanelActive.onChanged -= OnIgnorePanelActiveChangedHandler;
			m_state.navigationLayerActive.onChanged -= OnNavigationLayerActiveChangedHandler;
			m_state = null;
			m_users = null;
		}

		private void OnIgnorePanelActiveChangedHandler(bool isActive)
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
			bool num = m_state.ignorePanelActive.Get();
			bool flag2 = m_users.GetSelectedIndex() != -1;
			if (num)
			{
				if (flag && !flag2)
				{
					m_users.Selection(0, selected: true);
				}
				else if (!flag && flag2)
				{
					m_users.SelectionAll(selected: false);
				}
			}
		}
	}
}
