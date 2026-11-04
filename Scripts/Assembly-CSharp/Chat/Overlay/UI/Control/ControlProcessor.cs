using Binding.Base;
using Chat.Overlay.UI.State;
using Chat.Overlay.UI.Utils;

namespace Chat.Overlay.UI.Control
{
	public class ControlProcessor : BaseBindingTarget
	{
		private StateContext m_state;

		private HotkeyProvider m_hotkeyProvider;

		private NavigationStateHandler m_navigationStateHandler = new NavigationStateHandler();

		private ControlStateBinder m_controlStateBinder = new ControlStateBinder();

		private CarControlBlocker m_carControlBlocker = new CarControlBlocker();

		private CameraSwitchBlocker m_cameraSwitchBlocker = new CameraSwitchBlocker();

		public void Setup(StateContext state, HotkeyProvider hotkeyProvider)
		{
			m_state = state;
			m_hotkeyProvider = hotkeyProvider;
			m_navigationStateHandler.Setup(m_state);
			m_controlStateBinder.Setup(m_state, m_hotkeyProvider);
			m_carControlBlocker.Setup(m_state);
			m_cameraSwitchBlocker.Setup(m_state);
		}

		public void Release()
		{
			m_cameraSwitchBlocker.Release();
			m_carControlBlocker.Release();
			m_controlStateBinder.Release();
			m_navigationStateHandler.Release();
			m_state = null;
			m_hotkeyProvider = null;
		}
	}
}
