using Chat.Overlay.UI.State;
using Chat.Overlay.UI.Utils;
using GameInput;

namespace Chat.Overlay.UI.InputField.InputStrategy
{
	public class InputStrategyProvider
	{
		private StateContext m_state;

		private HotkeyProvider m_hotkeyProvider;

		private ChatInputFieldElement m_inputField;

		public void Setup(StateContext state, HotkeyProvider hotkeyProvider, ChatInputFieldElement inputField)
		{
			m_state = state;
			m_hotkeyProvider = hotkeyProvider;
			m_inputField = inputField;
		}

		public void Release()
		{
			m_state = null;
			m_hotkeyProvider = null;
			m_inputField = null;
		}

		public IInputStrategy GetInputStrategy(DeviceType deviceType)
		{
			return GetStandaloneInputStrategy();
		}

		private IInputStrategy GetStandaloneInputStrategy()
		{
			if (GameManager.IsLinuxOsPlatform())
			{
				return CreateVirtualInputStrategy();
			}
			return CreateStandaloneInputStrategy();
		}

		private IInputStrategy GetConsoleInputStrategy(DeviceType deviceType)
		{
			return CreateVirtualInputStrategy();
		}

		private IInputStrategy CreateStandaloneInputStrategy()
		{
			return new StandaloneInputStrategy(m_state, m_hotkeyProvider, m_inputField);
		}

		private IInputStrategy CreateVirtualInputStrategy()
		{
			return new VirtualInputStrategy(m_inputField);
		}
	}
}
