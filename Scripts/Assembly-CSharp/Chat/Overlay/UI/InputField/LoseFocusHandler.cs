using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.InputField
{
	public class LoseFocusHandler
	{
		private StateContext m_state;

		public void Setup(StateContext state)
		{
			m_state = state;
			m_state.inputFieldFocused.onChanged += OnControlLayerActiveChanged;
		}

		public void Release()
		{
			TrySetIgnoreLoseFocus(value: false);
			m_state.inputFieldFocused.onChanged -= OnControlLayerActiveChanged;
			m_state = null;
		}

		private void OnControlLayerActiveChanged(bool isActive)
		{
			TrySetIgnoreLoseFocus(isActive);
		}

		private void TrySetIgnoreLoseFocus(bool value)
		{
			if (States.current is BaseRaceState baseRaceState)
			{
				baseRaceState.forceIgnoreLoseFocus = value;
			}
		}
	}
}
