using Chat.Overlay.UI.Utils;
using GameInput;

namespace Chat.Overlay.UI.ActionList
{
	public class NavigationBinder
	{
		private HotkeyProvider m_hotkeyProvider;

		private UIChatActionPanelContext m_actionPanel;

		private ChatActionListProperty m_actions;

		public void Setup(HotkeyProvider hotkeyProvider, UIChatActionPanelContext actionPanel, ChatActionListProperty actions)
		{
			m_hotkeyProvider = hotkeyProvider;
			m_actionPanel = actionPanel;
			m_actions = actions;
			BindInput();
		}

		public void Release()
		{
			ReleaseInput();
			m_hotkeyProvider = null;
			m_actionPanel = null;
			m_actions = null;
		}

		private void BindInput()
		{
			m_hotkeyProvider.Add(ActionType.NavSelectBtnA, m_actions.ClickOnCurrentAction, repeat: false);
			m_hotkeyProvider.Add(ActionType.NavCancelBtnB, m_actionPanel.Close, repeat: false);
			m_hotkeyProvider.Add(ActionType.NavUp, PrevItem, repeat: true);
			m_hotkeyProvider.Add(ActionType.NavDown, NextItem, repeat: true);
			m_hotkeyProvider.UpdateHotkeys();
		}

		private void ReleaseInput()
		{
			m_hotkeyProvider.Remove(ActionType.NavSelectBtnA, m_actions.ClickOnCurrentAction);
			m_hotkeyProvider.Remove(ActionType.NavCancelBtnB, m_actionPanel.Close);
			m_hotkeyProvider.Remove(ActionType.NavUp, PrevItem);
			m_hotkeyProvider.Remove(ActionType.NavDown, NextItem);
			m_hotkeyProvider.UpdateHotkeys();
		}

		private void NextItem()
		{
			m_actions.SelectAction(1);
		}

		private void PrevItem()
		{
			m_actions.SelectAction(-1);
		}
	}
}
