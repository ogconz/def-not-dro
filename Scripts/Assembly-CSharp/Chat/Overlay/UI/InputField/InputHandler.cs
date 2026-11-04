using System;
using Chat.Overlay.UI.InputField.InputStrategy;
using Chat.Overlay.UI.State;
using Chat.Overlay.UI.Utils;
using GameInput;
using Rewired;

namespace Chat.Overlay.UI.InputField
{
	public class InputHandler
	{
		private StateContext m_state;

		private HotkeyProvider m_hotkeyProvider;

		private ChatInputFieldElement m_inputField;

		private InputStrategyProvider m_inputStrategyProvider = new InputStrategyProvider();

		private IInputStrategy m_inputStrategy;

		public void Setup(StateContext state, HotkeyProvider hotkeyProvider, ChatInputFieldElement inputField)
		{
			m_state = state;
			m_hotkeyProvider = hotkeyProvider;
			m_inputField = inputField;
			m_inputStrategyProvider.Setup(m_state, m_hotkeyProvider, m_inputField);
			GameInput.InputManager instance = GameInput.InputManager.instance;
			instance.onChangeActiveDevice = (Action<Controller>)Delegate.Combine(instance.onChangeActiveDevice, new Action<Controller>(SetupStrategy));
			SetupStrategy(GameInput.InputManager.instance.lastController);
		}

		public void Release()
		{
			GameInput.InputManager instance = GameInput.InputManager.instance;
			instance.onChangeActiveDevice = (Action<Controller>)Delegate.Remove(instance.onChangeActiveDevice, new Action<Controller>(SetupStrategy));
			ReleaseStrategy();
			m_inputStrategyProvider.Release();
			m_state = null;
			m_hotkeyProvider = null;
			m_inputField = null;
		}

		private void SetupStrategy(Controller controller)
		{
			if (CanChangeStrategy())
			{
				ReleaseStrategy();
				DeviceType deviceType = controller.GetMetaInfo().deviceType;
				m_inputStrategy = m_inputStrategyProvider.GetInputStrategy(deviceType);
				m_inputStrategy.Setup();
			}
		}

		private void ReleaseStrategy()
		{
			if (m_inputStrategy != null)
			{
				m_inputStrategy.Release();
				m_inputStrategy = null;
			}
		}

		private bool CanChangeStrategy()
		{
			if (m_inputStrategy != null)
			{
				return !m_inputStrategy.IsLocked();
			}
			return true;
		}
	}
}
