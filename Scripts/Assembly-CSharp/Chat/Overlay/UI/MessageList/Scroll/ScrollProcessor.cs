using Binding;
using Binding.Base;
using Chat.Overlay.UI.ChannelList;
using Chat.Overlay.UI.State;
using Chat.Overlay.UI.Utils;
using DG.Tweening;
using UnityEngine;

namespace Chat.Overlay.UI.MessageList.Scroll
{
	public class ScrollProcessor : BaseBindingTarget
	{
		private const float FocusDelay = 0.1f;

		private const float ScrollDuration = 0.2f;

		private ScrollRectProperty m_scrollRect = new ScrollRectProperty();

		private StateContext m_state;

		private HotkeyProvider m_hotkeyProvider;

		private ChatChannelListProperty m_roomChannels;

		private ChatMessageListProperty m_messages;

		private AutoScrollHandler m_autoScrollHandler = new AutoScrollHandler();

		private ScrollStateHandler m_stateHandler = new ScrollStateHandler();

		private ScrollMoveReader m_moveReader = new ScrollMoveReader();

		private ScrollMoveHandler m_moveHandler = new ScrollMoveHandler();

		private ChannelScrollHolder m_channelScrollHolder = new ChannelScrollHolder();

		public ScrollMoveReader scrollMoveReader => m_moveReader;

		public void Setup(StateContext state, HotkeyProvider hotkeyProvider, ChatChannelListProperty roomChannels, ChatMessageListProperty messages)
		{
			m_state = state;
			m_hotkeyProvider = hotkeyProvider;
			m_roomChannels = roomChannels;
			m_messages = messages;
			m_autoScrollHandler.Setup(m_state, this, m_messages);
			m_stateHandler.Setup(m_state, m_scrollRect, m_messages);
			m_moveReader.Setup(m_state, m_hotkeyProvider);
			m_moveHandler.Setup(m_moveReader, m_scrollRect, m_messages);
			m_channelScrollHolder.Setup(m_state, m_roomChannels, m_messages, this);
		}

		public void Release()
		{
			m_channelScrollHolder.Release();
			m_moveHandler.Release();
			m_moveReader.Release();
			m_stateHandler.Release();
			m_autoScrollHandler.Release();
			m_state = null;
			m_hotkeyProvider = null;
			m_roomChannels = null;
			m_messages = null;
		}

		public void FocusToBottom()
		{
			if (m_scrollRect.value.enabled)
			{
				HotkeyManager.Sleep(0.3f);
				TimeController.AddCallback(0.1f, FocusCallback);
			}
		}

		private void FocusCallback()
		{
			if (m_state.navigationLayerActive.Get() && !m_state.inputFieldFocused.Get())
			{
				m_moveHandler.LastSelection();
			}
			else
			{
				m_moveHandler.ResetSelection();
			}
			m_scrollRect.value.DONormalizedPos(Vector2.zero, 0.2f);
		}
	}
}
