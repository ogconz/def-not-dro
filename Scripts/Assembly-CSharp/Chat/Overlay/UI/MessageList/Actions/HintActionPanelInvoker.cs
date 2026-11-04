using Binding;
using Binding.Base;
using Chat.Overlay.UI.State;
using Chat.Overlay.UI.Utils;
using GameInput;
using UnityEngine;

namespace Chat.Overlay.UI.MessageList.Actions
{
	public class HintActionPanelInvoker : BaseBindingTarget
	{
		private StateContext m_state;

		private HotkeyProvider m_hotkeyProvider;

		private MessageActionBinder m_messageActionBinder;

		private UIChatActionPanelContext m_actionPanel;

		private ChatMessageListProperty m_messages;

		private ChatMessageElementData m_lastElement;

		public void Setup(StateContext state, HotkeyProvider hotkeyProvider, MessageActionBinder messageActionBinder, UIChatActionPanelContext actionPanel, ChatMessageListProperty messages)
		{
			m_state = state;
			m_hotkeyProvider = hotkeyProvider;
			m_messageActionBinder = messageActionBinder;
			m_actionPanel = actionPanel;
			m_messages = messages;
			m_state.navigationLayerActive.onChanged += OnNavigationLayerChangedHandler;
			m_state.actionPanelActive.onChanged += OnStateFlagChangedHandler;
			m_state.ignorePanelActive.onChanged += OnStateFlagChangedHandler;
			m_messages.OnSelectionChanged += OnMessageSelectionChangedHandler;
		}

		public void Release()
		{
			ReleaseElement();
			m_state.navigationLayerActive.onChanged -= OnNavigationLayerChangedHandler;
			m_state.actionPanelActive.onChanged -= OnStateFlagChangedHandler;
			m_state.ignorePanelActive.onChanged -= OnStateFlagChangedHandler;
			m_messages.OnSelectionChanged -= OnMessageSelectionChangedHandler;
			m_state = null;
			m_hotkeyProvider = null;
			m_messageActionBinder = null;
			m_messages = null;
			m_actionPanel = null;
		}

		private void OnNavigationLayerChangedHandler(bool isActive)
		{
			if (isActive)
			{
				m_hotkeyProvider.Add(ActionType.NavSelectBtnA, TryInvokeActionPanel, repeat: false);
			}
			else
			{
				m_hotkeyProvider.Remove(ActionType.NavSelectBtnA, TryInvokeActionPanel);
			}
			m_hotkeyProvider.UpdateHotkeys();
			UpdateVisibilityForLastMessage();
		}

		private void OnStateFlagChangedHandler(bool isActive)
		{
			UpdateVisibilityForLastMessage();
		}

		private void OnMessageSelectionChangedHandler(BaseListElementData item)
		{
			ReleaseElement();
			if (item != null && item.selected)
			{
				SetupElement();
			}
		}

		private void SetupElement()
		{
			m_lastElement = m_messages.GetSelected() as ChatMessageElementData;
			if (m_lastElement != null)
			{
				SetupLayout(m_lastElement.GetHintRectTransform(), m_lastElement.GetContentPosition());
				m_messageActionBinder.BindMessage(m_lastElement.GetMessage());
				UpdateVisibilityForLastMessage();
			}
		}

		private void ReleaseElement()
		{
			m_messageActionBinder.ReleaseMessage();
			TrySetHintVisibility(value: false);
			m_lastElement = null;
		}

		private void UpdateVisibilityForLastMessage()
		{
			TrySetHintVisibility(CanShowHint());
		}

		private bool CanShowHint()
		{
			if (m_state.navigationLayerActive.Get() && !m_state.actionPanelActive.Get() && !m_state.ignorePanelActive.Get())
			{
				return m_messageActionBinder.CanInvokeActionPanel();
			}
			return false;
		}

		private void TrySetHintVisibility(bool value)
		{
			if (m_lastElement != null)
			{
				m_lastElement.SetHintVisible(value);
			}
		}

		private void TryInvokeActionPanel()
		{
			if (m_lastElement != null && CanShowHint())
			{
				m_messageActionBinder.InvokeActionPanel(m_lastElement.GetContentPosition());
			}
		}

		private void SetupLayout(RectTransform layoutRect, Vector2 contentPosition)
		{
			bool isLeftSide = GetActionPanelPosition().x < contentPosition.x;
			GetAnchorsOfSide(isLeftSide, out var anchorMin, out var anchorMax, out var pivot);
			layoutRect.anchorMin = anchorMin;
			layoutRect.anchorMax = anchorMax;
			layoutRect.pivot = pivot;
		}

		private Vector2 GetActionPanelPosition()
		{
			RectTransform rectTransform = (RectTransform)m_actionPanel.gameObject.transform;
			Vector2 vector = rectTransform.GetSize() * rectTransform.pivot;
			return (Vector2)rectTransform.position - vector;
		}

		private void GetAnchorsOfSide(bool isLeftSide, out Vector2 anchorMin, out Vector2 anchorMax, out Vector2 pivot)
		{
			float num = (isLeftSide ? 0f : 1f);
			float x = 1f - num;
			anchorMin = new Vector2(num, 1f);
			anchorMax = new Vector2(num, 1f);
			pivot = new Vector2(x, 1f);
		}
	}
}
