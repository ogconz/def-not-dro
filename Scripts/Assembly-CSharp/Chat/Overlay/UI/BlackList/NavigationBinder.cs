using Chat.Overlay.UI.Utils;
using GameInput;

namespace Chat.Overlay.UI.BlackList
{
	public class NavigationBinder
	{
		private HotkeyProvider m_hotkeyProvider;

		private UIChatIgnoreListPanelContext m_ignorePanel;

		private ChatIgnoreListProperty m_users;

		public void Setup(HotkeyProvider hotkeyProvider, UIChatIgnoreListPanelContext ignorePanel, ChatIgnoreListProperty users)
		{
			m_hotkeyProvider = hotkeyProvider;
			m_ignorePanel = ignorePanel;
			m_users = users;
			BindInput();
		}

		public void Release()
		{
			ReleaseInput();
			m_hotkeyProvider = null;
			m_ignorePanel = null;
			m_users = null;
		}

		private void BindInput()
		{
			m_hotkeyProvider.Add(ActionType.NavSelectBtnA, m_users.ClickOnCurrentItem, repeat: false);
			m_hotkeyProvider.Add(ActionType.NavCancelBtnB, m_ignorePanel.Close, repeat: false);
			m_hotkeyProvider.Add(ActionType.NavUp, PrevItem, repeat: true);
			m_hotkeyProvider.Add(ActionType.NavDown, NextItem, repeat: true);
			m_hotkeyProvider.UpdateHotkeys();
		}

		private void ReleaseInput()
		{
			m_hotkeyProvider.Remove(ActionType.NavSelectBtnA, m_users.ClickOnCurrentItem);
			m_hotkeyProvider.Remove(ActionType.NavCancelBtnB, m_ignorePanel.Close);
			m_hotkeyProvider.Remove(ActionType.NavUp, PrevItem);
			m_hotkeyProvider.Remove(ActionType.NavDown, NextItem);
			m_hotkeyProvider.UpdateHotkeys();
		}

		private void NextItem()
		{
			m_users.SelectItemByOffset(1);
		}

		private void PrevItem()
		{
			m_users.SelectItemByOffset(-1);
		}
	}
}
