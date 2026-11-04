using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.Window
{
	public class WindowVisibilityHolder
	{
		private StateContext m_state;

		public void Setup(StateContext state)
		{
			m_state = state;
			States.onStateActivate += OnStateChangedHandler;
			OnStateChangedHandler(States.current);
		}

		public void Release()
		{
			States.onStateActivate -= OnStateChangedHandler;
			m_state = null;
		}

		private void OnStateChangedHandler(BaseState baseState)
		{
			m_state.windowForceShow.Set(baseState is GarageGUIState);
		}
	}
}
