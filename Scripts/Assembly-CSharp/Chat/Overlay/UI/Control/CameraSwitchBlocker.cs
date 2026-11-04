using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.Control
{
	public class CameraSwitchBlocker
	{
		private StateContext m_state;

		public void Setup(StateContext state)
		{
			m_state = state;
			m_state.controlLayerActive.onChanged += OnControlStateChangedHandler;
		}

		public void Release()
		{
			m_state.controlLayerActive.onChanged -= OnControlStateChangedHandler;
			m_state = null;
		}

		private void OnControlStateChangedHandler(bool isActive)
		{
			if ((bool)CameraSwitch.instance)
			{
				CameraSwitch.instance.SetActiveCameraSwitch(!isActive);
			}
		}
	}
}
