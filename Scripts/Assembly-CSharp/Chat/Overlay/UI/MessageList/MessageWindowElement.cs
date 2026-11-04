using Binding.Base;
using Chat.Overlay.UI.ChannelList;
using Chat.Overlay.UI.MessageList.Actions;
using Chat.Overlay.UI.MessageList.Jump;
using Chat.Overlay.UI.MessageList.Scroll;
using Chat.Overlay.UI.State;
using Chat.Overlay.UI.Utils;

namespace Chat.Overlay.UI.MessageList
{
	public class MessageWindowElement : BaseBindingTarget
	{
		private ChatMessageListProperty m_messages = new ChatMessageListProperty();

		private ChatService m_service;

		private StateContext m_state;

		private HotkeyProvider m_hotkeyProvider;

		private ChatChannelListProperty m_roomChannels;

		private UIChatActionPanelContext m_actionPanel;

		private MessageActionBinder m_messageActionBinder = new MessageActionBinder();

		private PointerActionPanelInvoker m_pointerActionPanelInvoker = new PointerActionPanelInvoker();

		private HintActionPanelInvoker m_hintActionPanelInvoker = new HintActionPanelInvoker();

		private AutoCloseActionPanel m_autoCloseActionPanel = new AutoCloseActionPanel();

		private MessageListStateHandler m_messageListStateHandler = new MessageListStateHandler();

		private ScrollProcessor m_scrollProcessor = new ScrollProcessor();

		private JumpProcessor m_jumpProcessor = new JumpProcessor();

		public void Setup(ChatService service, StateContext state, HotkeyProvider hotkeyProvider, ChatChannelListProperty roomChannels, UIChatActionPanelContext actionPanel)
		{
			m_service = service;
			m_state = state;
			m_hotkeyProvider = hotkeyProvider;
			m_roomChannels = roomChannels;
			m_actionPanel = actionPanel;
			m_messages.Setup(m_service, m_roomChannels);
			m_messageActionBinder.Setup(m_service, m_actionPanel);
			m_pointerActionPanelInvoker.Setup(m_messageActionBinder, m_messages);
			m_hintActionPanelInvoker.Setup(m_state, m_hotkeyProvider, m_messageActionBinder, m_actionPanel, m_messages);
			m_autoCloseActionPanel.Setup(m_state, m_messageActionBinder, m_messages);
			m_messageListStateHandler.Setup(m_messages, m_state);
			m_scrollProcessor.Setup(m_state, m_hotkeyProvider, m_roomChannels, m_messages);
			m_jumpProcessor.Setup(m_state, m_scrollProcessor);
		}

		public void Release()
		{
			m_jumpProcessor.Release();
			m_scrollProcessor.Release();
			m_messageListStateHandler.Release();
			m_autoCloseActionPanel.Release();
			m_hintActionPanelInvoker.Release();
			m_pointerActionPanelInvoker.Release();
			m_messageActionBinder.Release();
			m_messages.Release();
			m_service = null;
			m_state = null;
			m_hotkeyProvider = null;
			m_roomChannels = null;
			m_actionPanel = null;
		}
	}
}
