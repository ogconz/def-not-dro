using Chat.Overlay.UI.State;
using Chat.Overlay.UI.Utils;
using GameInput;

namespace Chat.Overlay.UI.InputField.InputStrategy
{
	public class StandaloneInputStrategy : IInputStrategy
	{
		private StateContext m_state;

		private HotkeyProvider m_hotkeyProvider;

		private ChatInputFieldElement m_inputField;

		private bool m_hotkeyRegistered;

		public StandaloneInputStrategy(StateContext state, HotkeyProvider hotkeyProvider, ChatInputFieldElement inputField)
		{
			m_state = state;
			m_hotkeyProvider = hotkeyProvider;
			m_inputField = inputField;
		}

		~StandaloneInputStrategy()
		{
			m_state = null;
			m_hotkeyProvider = null;
			m_inputField = null;
		}

		public void Setup()
		{
			m_inputField.onSubmit.AddListener(OnSubmitHandler);
			m_inputField.onActivityChanged += OnActivityChangedHandler;
			m_inputField.onActivityChanged += UpdateHotkeyState;
			m_state.navigationLayerActive.onChanged += OnNavigationLayerActiveHandler;
			m_state.messageSelecting.onChanged += OnMessageSelectingHandler;
			UpdateHotkeyState();
		}

		public void Release()
		{
			m_inputField.onSubmit.RemoveListener(OnSubmitHandler);
			m_inputField.onActivityChanged -= OnActivityChangedHandler;
			m_inputField.onActivityChanged -= UpdateHotkeyState;
			m_state.navigationLayerActive.onChanged -= OnNavigationLayerActiveHandler;
			m_state.messageSelecting.onChanged -= OnMessageSelectingHandler;
			ReleaseHotkey();
		}

		public bool IsLocked()
		{
			return false;
		}

		private void OnSubmitHandler(string rawText)
		{
			m_inputField.InvokeTextSubmitEvent();
			if (States.mainCurrent is GarageGUIState)
			{
				m_inputField.ActivateInputField();
			}
			else
			{
				m_inputField.SetSelection(selected: false);
			}
		}

		private void OnActivityChangedHandler()
		{
			if (m_inputField.IsSelected())
			{
				m_inputField.ActivateInputField();
			}
			else
			{
				m_inputField.DeactivateInputField();
			}
		}

		private void OnNavigationLayerActiveHandler(bool isActive)
		{
			UpdateHotkeyState();
		}

		private void OnMessageSelectingHandler(bool isActive)
		{
			if (isActive)
			{
				m_inputField.SetSelection(selected: false);
			}
		}

		private void UpdateHotkeyState()
		{
			if (m_inputField.IsSelected() && m_state.navigationLayerActive.Get())
			{
				BindHotkey();
			}
			else
			{
				ReleaseHotkey();
			}
		}

		private void BindHotkey()
		{
			if (!m_hotkeyRegistered)
			{
				m_hotkeyProvider.Add(ActionType.NavSelectBtnA, InvokeSubmitInput, repeat: false);
				m_hotkeyProvider.Add(ActionType.NavCancelBtnB, InvokeCancelInput, repeat: false);
				m_hotkeyProvider.UpdateHotkeys();
				m_hotkeyRegistered = true;
			}
		}

		private void ReleaseHotkey()
		{
			if (m_hotkeyRegistered)
			{
				m_hotkeyProvider.Remove(ActionType.NavSelectBtnA, InvokeSubmitInput);
				m_hotkeyProvider.Remove(ActionType.NavCancelBtnB, InvokeCancelInput);
				m_hotkeyProvider.UpdateHotkeys();
				m_hotkeyRegistered = false;
			}
		}

		private void InvokeSubmitInput()
		{
			m_inputField.OnSubmit(null);
		}

		private void InvokeCancelInput()
		{
			if (m_inputField.IsSelected())
			{
				m_inputField.SetSelection(selected: false);
			}
		}
	}
}
