using Binding.Base;
using Chat.Overlay.UI.ChannelList;
using Chat.Overlay.UI.State;
using Chat.Overlay.UI.Utils;

namespace Chat.Overlay.UI.InputField
{
	public class InputFieldProcessor : BaseBindingTarget
	{
		private ChatService m_service;

		private StateContext m_state;

		private HotkeyProvider m_hotkeyProvider;

		private ChatInputFieldElement m_inputField;

		private ChatChannelListProperty m_roomChannels;

		private InputMessageHandler m_inputMessageHandler = new InputMessageHandler();

		private InputFieldTextHolder m_textHolder = new InputFieldTextHolder();

		private InputHintReplacer m_inputHintReplacer = new InputHintReplacer();

		private PlaceholderHandler m_placeholderHandler = new PlaceholderHandler();

		private InputFieldStateHandler m_stateHandler = new InputFieldStateHandler();

		private InputFieldVisibilityHandler m_visibilityHandler = new InputFieldVisibilityHandler();

		private AutoSelectHandler m_autoSelectHandler = new AutoSelectHandler();

		private InputHandler m_inputHandler = new InputHandler();

		private LoseFocusHandler m_loseFocusHandler = new LoseFocusHandler();

		public void Setup(ChatService service, StateContext state, HotkeyProvider hotkeyProvider, ChatInputFieldElement inputField, ChatChannelListProperty roomChannels)
		{
			m_service = service;
			m_state = state;
			m_hotkeyProvider = hotkeyProvider;
			m_inputField = inputField;
			m_roomChannels = roomChannels;
			m_inputMessageHandler.Setup(m_service, m_roomChannels, m_inputField);
			m_textHolder.Setup(m_service.channelProvider, m_roomChannels, m_inputField);
			m_inputHintReplacer.Setup(m_state);
			m_placeholderHandler.Setup(m_state, m_inputField);
			m_stateHandler.Setup(m_inputField, m_state);
			m_visibilityHandler.Setup(m_state);
			m_autoSelectHandler.Setup(m_state, m_inputField);
			m_inputHandler.Setup(m_state, m_hotkeyProvider, m_inputField);
			m_loseFocusHandler.Setup(m_state);
		}

		public void Release()
		{
			m_loseFocusHandler.Release();
			m_inputHandler.Release();
			m_autoSelectHandler.Release();
			m_visibilityHandler.Release();
			m_stateHandler.Release();
			m_placeholderHandler.Release();
			m_inputHintReplacer.Release();
			m_textHolder.Release();
			m_inputMessageHandler.Release();
			m_service = null;
			m_state = null;
			m_hotkeyProvider = null;
			m_inputField = null;
			m_roomChannels = null;
		}
	}
}
