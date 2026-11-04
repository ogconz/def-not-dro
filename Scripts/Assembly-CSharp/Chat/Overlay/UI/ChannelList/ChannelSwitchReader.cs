using System;
using Chat.Overlay.UI.State;
using Chat.Overlay.UI.Utils;
using GameInput;

namespace Chat.Overlay.UI.ChannelList
{
	public class ChannelSwitchReader
	{
		private StateContext m_state;

		private HotkeyProvider m_hotkeyProvider;

		public event Action<int> onMoveEvent;

		public void Setup(StateContext state, HotkeyProvider hotkeyProvider)
		{
			m_state = state;
			m_hotkeyProvider = hotkeyProvider;
			m_state.navigationLayerActive.onChanged += OnNavigationLayerActiveChanged;
		}

		public void Release()
		{
			m_state.navigationLayerActive.onChanged -= OnNavigationLayerActiveChanged;
			m_state = null;
			m_hotkeyProvider = null;
		}

		private void OnNavigationLayerActiveChanged(bool isActive)
		{
			if (isActive)
			{
				m_hotkeyProvider.Add(ActionType.NavLeft, MoveLeft, repeat: false);
				m_hotkeyProvider.Add(ActionType.NavRight, MoveRight, repeat: false);
				m_hotkeyProvider.Add(ActionType.NavPrevPage, MoveLeft, repeat: false);
				m_hotkeyProvider.Add(ActionType.NavNextPage, MoveRight, repeat: false);
			}
			else
			{
				m_hotkeyProvider.Remove(ActionType.NavLeft, MoveLeft);
				m_hotkeyProvider.Remove(ActionType.NavRight, MoveRight);
				m_hotkeyProvider.Remove(ActionType.NavPrevPage, MoveLeft);
				m_hotkeyProvider.Remove(ActionType.NavNextPage, MoveRight);
			}
			m_hotkeyProvider.UpdateHotkeys();
		}

		private void MoveLeft()
		{
			this.onMoveEvent.SafeCall(-1);
		}

		private void MoveRight()
		{
			this.onMoveEvent.SafeCall(1);
		}
	}
}
