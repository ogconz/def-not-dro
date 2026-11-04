using System;
using Chat.Overlay.UI.State;
using Chat.Overlay.UI.Utils;
using GameInput;
using UnityEngine;

namespace Chat.Overlay.UI.MessageList.Scroll
{
	public class ScrollMoveReader
	{
		private StateContext m_state;

		private HotkeyProvider m_hotkeyProvider;

		private bool m_inputRegistered;

		private bool m_inputBlocked;

		public event Action<float> onMoveEvent;

		public event Action<float> onScrollEvent;

		public void Setup(StateContext state, HotkeyProvider hotkeyProvider)
		{
			m_state = state;
			m_hotkeyProvider = hotkeyProvider;
			HotkeyManager.onOwnerStackChangedEvent += OnOwnerStackChangedHandler;
			m_state.navigationLayerActive.onChanged += OnNavigationLayerActiveChanged;
		}

		public void Release()
		{
			HotkeyManager.onOwnerStackChangedEvent -= OnOwnerStackChangedHandler;
			m_state.navigationLayerActive.onChanged -= OnNavigationLayerActiveChanged;
			m_state = null;
			m_hotkeyProvider = null;
		}

		private void OnOwnerStackChangedHandler()
		{
			m_inputBlocked = m_hotkeyProvider.IsHotkeyBlocked();
		}

		private void OnNavigationLayerActiveChanged(bool isActive)
		{
			if (isActive)
			{
				BindInput();
			}
			else
			{
				ReleaseInput();
			}
		}

		private void BindInput()
		{
			TickManager.RegisterFixedUpdate(ReadAxisInput, 1, "readAxisInputForScroll");
			m_hotkeyProvider.Add(ActionType.NavDPadUp, MoveToUp, repeat: true);
			m_hotkeyProvider.Add(ActionType.NavDPadDown, MoveToDown, repeat: true);
			m_hotkeyProvider.UpdateHotkeys();
			m_inputRegistered = true;
		}

		private void ReleaseInput()
		{
			m_inputRegistered = false;
			TickManager.UnregisterFixedUpdate(ReadAxisInput);
			m_hotkeyProvider.Remove(ActionType.NavDPadUp, MoveToUp);
			m_hotkeyProvider.Remove(ActionType.NavDPadDown, MoveToDown);
			m_hotkeyProvider.UpdateHotkeys();
		}

		private void ReadAxisInput()
		{
			if (InputManager.instance != null)
			{
				ReadAxisInputFromAction(ActionType.NavUp);
				ReadAxisInputFromAction(ActionType.NavDown);
			}
		}

		private void ReadAxisInputFromAction(ActionType actionType)
		{
			if (!IsInputUnlocked())
			{
				return;
			}
			GameInput.Action actionInfo = InputManager.instance.GetActionInfo(actionType);
			for (int i = 0; i < actionInfo.activeBinds.Count; i++)
			{
				BindElement bindElement = actionInfo.activeBinds[i];
				float rAWValueAsFloat = bindElement.GetRAWValueAsFloat();
				if (Mathf.Abs(rAWValueAsFloat) > 0.01f && bindElement is BindAxisElement)
				{
					this.onScrollEvent.SafeCall(rAWValueAsFloat);
				}
			}
		}

		private void MoveToUp()
		{
			if (IsInputUnlocked())
			{
				this.onMoveEvent.SafeCall(1f);
			}
		}

		private void MoveToDown()
		{
			if (IsInputUnlocked())
			{
				this.onMoveEvent.SafeCall(-1f);
			}
		}

		public bool IsInputUnlocked()
		{
			if (m_inputRegistered && !m_inputBlocked)
			{
				return !HotkeyManager.IsSleeping();
			}
			return false;
		}
	}
}
