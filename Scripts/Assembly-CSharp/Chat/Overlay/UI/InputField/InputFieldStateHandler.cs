using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.InputField
{
	public class InputFieldStateHandler
	{
		private ChatInputFieldElement m_inputField;

		private StateContext m_state;

		public void Setup(ChatInputFieldElement inputField, StateContext state)
		{
			m_inputField = inputField;
			m_state = state;
			m_state.inputFieldFocused.onChanged += OnInputFieldFocusedHandler;
			m_inputField.onActivityChanged += OnInputFieldActivityChanged;
			UpdateInputFieldState();
		}

		public void Release()
		{
			m_state.inputFieldFocused.onChanged -= OnInputFieldFocusedHandler;
			m_inputField.onActivityChanged -= OnInputFieldActivityChanged;
			m_inputField = null;
			m_state = null;
		}

		private void OnInputFieldFocusedHandler(bool isActive)
		{
			if (m_inputField.IsSelected() != isActive)
			{
				m_inputField.SetSelection(isActive);
			}
		}

		private void OnInputFieldActivityChanged()
		{
			UpdateInputFieldState();
		}

		private void UpdateInputFieldState()
		{
			m_state.inputFieldFocused.Set(m_inputField.IsSelected());
			m_state.inputFieldHovered.Set(m_inputField.IsHovered());
		}
	}
}
