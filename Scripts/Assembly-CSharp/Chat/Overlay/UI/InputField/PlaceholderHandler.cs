using Binding;
using Binding.Base;
using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.InputField
{
	public class PlaceholderHandler : BaseBindingTarget
	{
		private BoolProperty m_inputFieldTextPlaceholder = new BoolProperty();

		private BoolProperty m_inputFieldHint = new BoolProperty();

		private StateContext m_state;

		private ChatInputFieldElement m_inputField;

		public void Setup(StateContext state, ChatInputFieldElement inputField)
		{
			m_state = state;
			m_inputField = inputField;
			m_state.navigationLayerActive.onChanged += OnStateFlagChangedHandler;
			m_state.inputFieldFocused.onChanged += OnStateFlagChangedHandler;
			m_inputField.onValueChanged.AddListener(OnValueChangedHandler);
			States.onStateActivate += OnStateActivateHandler;
			UpdatePlaceholderState();
		}

		public void Release()
		{
			m_state.navigationLayerActive.onChanged -= OnStateFlagChangedHandler;
			m_state.inputFieldFocused.onChanged -= OnStateFlagChangedHandler;
			m_inputField.onValueChanged.RemoveListener(OnValueChangedHandler);
			States.onStateActivate -= OnStateActivateHandler;
			m_state = null;
			m_inputField = null;
		}

		private void OnStateFlagChangedHandler(bool isActive)
		{
			UpdatePlaceholderState();
		}

		private void OnValueChangedHandler(string text)
		{
			UpdatePlaceholderState();
		}

		private void OnStateActivateHandler(BaseState baseState)
		{
			UpdatePlaceholderState();
		}

		private void UpdatePlaceholderState()
		{
			bool num = m_state.inputFieldFocused.Get();
			bool flag = m_state.navigationLayerActive.Get() || States.current is GarageGUIState;
			bool flag2 = string.IsNullOrEmpty(m_inputField.text);
			bool value = !num && flag;
			bool value2 = !num && flag2;
			m_inputFieldHint.value = value;
			m_inputFieldTextPlaceholder.value = value2;
		}
	}
}
