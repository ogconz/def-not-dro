using Binding.Base;
using Chat.Overlay.UI.BlackList;
using Chat.Overlay.UI.ChannelList;
using Chat.Overlay.UI.Control;
using Chat.Overlay.UI.InputField;
using Chat.Overlay.UI.MessageList;
using Chat.Overlay.UI.State;
using Chat.Overlay.UI.Utils;
using Chat.Overlay.UI.Window;
using UnityEngine;

namespace Chat.Overlay.UI
{
	public class UIChatWindowContext : BaseBindingBehaviourTarget
	{
		[SerializeField]
		private ChatInputFieldElement m_inputField;

		private ChatChannelListProperty m_roomChannels = new ChatChannelListProperty();

		private MessageWindowElement m_messageWindow = new MessageWindowElement();

		private HotkeyProvider m_windowHotkeyProvider = new HotkeyProvider();

		private WindowEventHandler m_windowEventHandler = new WindowEventHandler();

		private ControlProcessor m_controlProcessor = new ControlProcessor();

		private InputFieldProcessor m_inputFieldProcessor = new InputFieldProcessor();

		private WindowVisibilityHandler m_visibilityHandler = new WindowVisibilityHandler();

		private WindowVisibilityHolder m_visibilityHolder = new WindowVisibilityHolder();

		private BackgroundVisibilityHandler m_backgroundVisibilityHandler = new BackgroundVisibilityHandler();

		private IgnorePanelButtonHandler m_ignorePanelButtonHandler = new IgnorePanelButtonHandler();

		private IgnorePanelHintVisibilityHandler m_ignorePanelHintVisibilityHandler = new IgnorePanelHintVisibilityHandler();

		private ChatHintPanelVisibilityHandler m_hintPanelVisibilityHandler = new ChatHintPanelVisibilityHandler();

		private ChannelSwitchReader m_channelSwitchReader = new ChannelSwitchReader();

		private ChannelSwitchHandler m_channelSwitchHandler = new ChannelSwitchHandler();

		private ChannelSelectionHolder m_channelSelectionHolder = new ChannelSelectionHolder();

		private ConnectionPlaceholder m_connectionPlaceholder = new ConnectionPlaceholder();

		private XRModeHandler m_xrModeHandler = new XRModeHandler();

		private ChatService m_service;

		private StateContext m_state;

		private UIChatActionPanelContext m_actionPanel;

		private UIChatIgnoreListPanelContext m_ignorePanel;

		public void Setup(ChatService service, StateContext stateContext, UIChatActionPanelContext actionPanel, UIChatIgnoreListPanelContext ignorePanel)
		{
			m_service = service;
			m_state = stateContext;
			m_actionPanel = actionPanel;
			m_ignorePanel = ignorePanel;
			m_windowEventHandler.Setup(m_state);
			m_messageWindow.Setup(m_service, m_state, m_windowHotkeyProvider, m_roomChannels, m_actionPanel);
			m_controlProcessor.Setup(m_state, m_windowHotkeyProvider);
			m_inputFieldProcessor.Setup(m_service, m_state, m_windowHotkeyProvider, m_inputField, m_roomChannels);
			m_channelSelectionHolder.Setup(m_service.channelProvider, m_service.channelHolder, m_roomChannels);
			m_roomChannels.Setup(m_service.channelProvider);
			m_visibilityHandler.Setup(m_state);
			m_visibilityHolder.Setup(m_state);
			m_backgroundVisibilityHandler.Setup(m_state);
			m_ignorePanelButtonHandler.Setup(m_ignorePanel);
			m_ignorePanelHintVisibilityHandler.Setup(m_state);
			m_hintPanelVisibilityHandler.Setup(m_state);
			m_channelSwitchReader.Setup(m_state, m_windowHotkeyProvider);
			m_channelSwitchHandler.Setup(m_state, m_channelSwitchReader, m_roomChannels);
			m_connectionPlaceholder.Setup(m_service.client);
			m_xrModeHandler.Setup(m_state);
		}

		public void Release()
		{
			m_xrModeHandler.Release();
			m_connectionPlaceholder.Release();
			m_channelSwitchHandler.Release();
			m_channelSwitchReader.Release();
			m_hintPanelVisibilityHandler.Release();
			m_ignorePanelHintVisibilityHandler.Release();
			m_ignorePanelButtonHandler.Release();
			m_backgroundVisibilityHandler.Release();
			m_visibilityHolder.Release();
			m_visibilityHandler.Release();
			m_roomChannels.Release();
			m_channelSelectionHolder.Release();
			m_inputFieldProcessor.Release();
			m_controlProcessor.Release();
			m_messageWindow.Release();
			m_windowEventHandler.Release();
			m_ignorePanel = null;
			m_actionPanel = null;
			m_state = null;
			m_service = null;
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			m_windowHotkeyProvider.Setup(this);
			m_state?.windowActive.Set(value: true);
		}

		protected override void OnDeactivate()
		{
			m_state?.windowActive.Set(value: false);
			m_windowHotkeyProvider.Release();
			base.OnDeactivate();
		}

		public override bool IsTopOrBottomContext(BaseContext context)
		{
			if (m_state != null && m_state.controlLayerActive.Get())
			{
				return false;
			}
			return base.IsTopOrBottomContext(context);
		}
	}
}
